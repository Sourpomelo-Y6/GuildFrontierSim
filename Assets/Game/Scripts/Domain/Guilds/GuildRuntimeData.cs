using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;

namespace GuildFrontierSim.Domain.Guilds
{
    public sealed class GuildRuntimeData
    {
        private readonly List<CharacterRuntimeData> characters;
        private readonly Dictionary<string, CharacterRuntimeData> charactersById;
        private readonly List<ExpeditionRuntimeData> expeditions =
            new List<ExpeditionRuntimeData>();
        private readonly Dictionary<string, ExpeditionRuntimeData> expeditionsById =
            new Dictionary<string, ExpeditionRuntimeData>(StringComparer.Ordinal);

        public GuildRuntimeData(
            string guildName,
            int funds,
            IEnumerable<CharacterRuntimeData> characters,
            string leaderCharacterId)
        {
            if (string.IsNullOrWhiteSpace(guildName))
            {
                throw new ArgumentException("Guild name cannot be empty.", nameof(guildName));
            }

            if (funds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(funds));
            }

            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            this.characters = new List<CharacterRuntimeData>();
            charactersById = new Dictionary<string, CharacterRuntimeData>(StringComparer.Ordinal);

            foreach (CharacterRuntimeData character in characters)
            {
                if (character == null)
                {
                    throw new ArgumentException("Character collection cannot contain null.", nameof(characters));
                }

                if (!charactersById.TryAdd(character.CharacterId, character))
                {
                    throw new ArgumentException(
                        $"Duplicate character ID: {character.CharacterId}",
                        nameof(characters));
                }

                this.characters.Add(character);
            }

            if (this.characters.Count == 0)
            {
                throw new ArgumentException("A guild must have at least one character.", nameof(characters));
            }

            if (string.IsNullOrWhiteSpace(leaderCharacterId) ||
                !charactersById.ContainsKey(leaderCharacterId))
            {
                throw new ArgumentException(
                    "Leader ID must reference a guild character.",
                    nameof(leaderCharacterId));
            }

            GuildName = guildName.Trim();
            Funds = funds;
            LeaderCharacterId = leaderCharacterId;
            Inventory = new GuildInventory();
        }

        public string GuildName { get; }
        public int Funds { get; private set; }
        public IReadOnlyList<CharacterRuntimeData> Characters => characters;
        public string LeaderCharacterId { get; private set; }
        public string ActingLeaderCharacterId { get; private set; } = string.Empty;
        public GuildInventory Inventory { get; }
        public IReadOnlyList<ExpeditionRuntimeData> Expeditions => expeditions;
        public int CurrentTurn { get; private set; }

        public CharacterRuntimeData Leader => charactersById[LeaderCharacterId];

        public bool TryGetCharacter(string characterId, out CharacterRuntimeData character)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                character = null;
                return false;
            }

            return charactersById.TryGetValue(characterId, out character);
        }

        public void AddFunds(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Funds = checked(Funds + amount);
        }

        public bool TrySpendFunds(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Funds < amount)
            {
                return false;
            }

            Funds -= amount;
            return true;
        }

        public void SetLeader(string characterId)
        {
            if (!TryGetCharacter(characterId, out _))
            {
                throw new ArgumentException(
                    "Leader ID must reference a guild character.",
                    nameof(characterId));
            }

            LeaderCharacterId = characterId;
        }

        public void AddExpedition(ExpeditionRuntimeData expedition)
        {
            if (expedition == null)
            {
                throw new ArgumentNullException(nameof(expedition));
            }

            if (expeditionsById.ContainsKey(expedition.ExpeditionId))
            {
                throw new ArgumentException(
                    $"Duplicate expedition ID: {expedition.ExpeditionId}",
                    nameof(expedition));
            }

            for (int index = 0; index < expedition.ParticipantIds.Count; index++)
            {
                string participantId = expedition.ParticipantIds[index];
                if (!TryGetCharacter(participantId, out CharacterRuntimeData participant) ||
                    participant.Status != CharacterStatus.Expedition)
                {
                    throw new ArgumentException(
                        $"Expedition participant is not assigned to this guild expedition: {participantId}",
                        nameof(expedition));
                }
            }

            expeditions.Add(expedition);
            expeditionsById.Add(expedition.ExpeditionId, expedition);
        }

        public bool TryGetExpedition(
            string expeditionId,
            out ExpeditionRuntimeData expedition)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                expedition = null;
                return false;
            }

            return expeditionsById.TryGetValue(expeditionId, out expedition);
        }

        public void SetActingLeader(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                ActingLeaderCharacterId = string.Empty;
                return;
            }

            if (!TryGetCharacter(characterId, out _))
            {
                throw new ArgumentException(
                    "Acting leader ID must reference a guild character.",
                    nameof(characterId));
            }

            ActingLeaderCharacterId = characterId;
        }

        public void AdvanceTurnNumber()
        {
            CurrentTurn = checked(CurrentTurn + 1);
        }
    }
}
