using System;

namespace GuildFrontierSim.Domain.Characters
{
    public sealed class CharacterRuntimeData
    {
        public CharacterRuntimeData(
            string characterId,
            int level,
            int maxHp,
            int attack,
            int defense,
            int speed,
            int salary,
            int loyalty,
            bool isPlayerCharacter = false)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException("Character ID cannot be empty.", nameof(characterId));
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be at least 1.");
            }

            if (maxHp < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "Maximum HP must be at least 1.");
            }

            if (attack < 0 || defense < 0 || speed < 0 || salary < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attack),
                    "Combat statistics and salary cannot be negative.");
            }

            if (loyalty < -100 || loyalty > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(loyalty),
                    "Loyalty must be between -100 and 100.");
            }

            CharacterId = characterId.Trim();
            Level = level;
            MaxHp = maxHp;
            CurrentHp = maxHp;
            Attack = attack;
            Defense = defense;
            Speed = speed;
            Salary = salary;
            Loyalty = loyalty;
            Status = CharacterStatus.Available;
            IsPlayerCharacter = isPlayerCharacter;
        }

        public string CharacterId { get; }
        public int Level { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Attack { get; }
        public int Defense { get; }
        public int Speed { get; }
        public int Salary { get; }
        public int Loyalty { get; private set; }
        public CharacterStatus Status { get; private set; }
        public int UnavailableTurnsRemaining { get; private set; }
        public bool IsPlayerCharacter { get; }
        public bool IsDeparturePending { get; private set; }

        public void ApplyDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            CurrentHp = Math.Max(0, CurrentHp - damage);
        }

        public void RestoreHp(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }

        public void ChangeLoyalty(int amount)
        {
            Loyalty = Math.Max(-100, Math.Min(100, Loyalty + amount));
        }

        public void SetStatus(CharacterStatus status, int unavailableTurns = 0)
        {
            if (unavailableTurns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unavailableTurns));
            }

            if (status == CharacterStatus.Available && unavailableTurns != 0)
            {
                throw new ArgumentException(
                    "Available characters cannot have unavailable turns.",
                    nameof(unavailableTurns));
            }

            Status = status;
            UnavailableTurnsRemaining = unavailableTurns;
        }

        public void AdvanceUnavailableTurn()
        {
            if (UnavailableTurnsRemaining <= 0)
            {
                return;
            }

            UnavailableTurnsRemaining--;
            if (UnavailableTurnsRemaining == 0 &&
                Status != CharacterStatus.Captured &&
                Status != CharacterStatus.Expedition &&
                Status != CharacterStatus.Defending)
            {
                Status = CharacterStatus.Available;
            }
        }

        public void MarkDeparturePending()
        {
            IsDeparturePending = true;
        }
    }
}
