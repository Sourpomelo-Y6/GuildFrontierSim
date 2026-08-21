using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;

namespace GuildFrontierSim.Domain.Battles
{
    public sealed class BattlePowerCalculator
    {
        public float Calculate(
            IReadOnlyList<CharacterRuntimeData> characters,
            BattleBalanceSettings settings)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            if (characters.Count == 0)
            {
                throw new ArgumentException("At least one character is required.", nameof(characters));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            float totalPower = 0f;
            for (int index = 0; index < characters.Count; index++)
            {
                CharacterRuntimeData character = characters[index];
                if (character == null)
                {
                    throw new ArgumentException("Characters cannot contain null.", nameof(characters));
                }

                totalPower += CalculateCharacterPower(character, settings);
            }

            return totalPower;
        }

        private static float CalculateCharacterPower(
            CharacterRuntimeData character,
            BattleBalanceSettings settings)
        {
            return character.Attack * settings.AttackWeight +
                   character.Defense * settings.DefenseWeight +
                   character.CurrentHp * settings.HitPointWeight +
                   character.Speed * settings.SpeedWeight;
        }
    }
}
