using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Processing.Economy
{
    public sealed class LoyaltyProcessor
    {
        public LoyaltyResult ApplySalaryResult(
            GuildRuntimeData guild,
            SalaryResult salaryResult,
            BattleBalanceSettings settings)
        {
            if (salaryResult == null)
            {
                throw new ArgumentNullException(nameof(salaryResult));
            }

            ValidateArguments(guild, settings);
            int loyaltyChange = salaryResult.Outcome == SalaryOutcome.Unpaid
                ? -settings.UnpaidSalaryLoyaltyPenalty
                : 0;
            return Apply(guild, salaryResult.CharacterIds, loyaltyChange);
        }

        public LoyaltyResult ApplyBattleResult(
            GuildRuntimeData guild,
            IEnumerable<string> participantIds,
            BattleOutcome outcome,
            BattleBalanceSettings settings)
        {
            ValidateArguments(guild, settings);
            int loyaltyChange;
            switch (outcome)
            {
                case BattleOutcome.Victory:
                    loyaltyChange = settings.VictoryLoyaltyChange;
                    break;
                case BattleOutcome.Defeat:
                    loyaltyChange = settings.DefeatLoyaltyChange;
                    break;
                case BattleOutcome.Draw:
                    loyaltyChange = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome));
            }

            return Apply(guild, participantIds, loyaltyChange);
        }

        public LoyaltyResult ResolvePendingDepartures(GuildRuntimeData guild)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            var pendingIds = new List<string>();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                if (guild.Characters[index].IsDeparturePending)
                {
                    pendingIds.Add(guild.Characters[index].CharacterId);
                }
            }

            return ResolveDepartures(guild, pendingIds, 0, pendingIds);
        }

        private static LoyaltyResult Apply(
            GuildRuntimeData guild,
            IEnumerable<string> characterIds,
            int loyaltyChange)
        {
            List<CharacterRuntimeData> characters = ResolveCharacters(guild, characterIds);
            var affectedIds = new List<string>(characters.Count);
            var departureCandidates = new List<string>();
            for (int index = 0; index < characters.Count; index++)
            {
                CharacterRuntimeData character = characters[index];
                affectedIds.Add(character.CharacterId);
                character.ChangeLoyalty(loyaltyChange);
                if (character.Loyalty <= -100)
                {
                    departureCandidates.Add(character.CharacterId);
                }
            }

            return ResolveDepartures(
                guild,
                departureCandidates,
                loyaltyChange,
                affectedIds);
        }

        private static LoyaltyResult ResolveDepartures(
            GuildRuntimeData guild,
            IReadOnlyList<string> departureCandidates,
            int loyaltyChange,
            IReadOnlyList<string> affectedIds)
        {
            var departedIds = new List<string>();
            var pendingIds = new List<string>();
            for (int index = 0; index < departureCandidates.Count; index++)
            {
                string characterId = departureCandidates[index];
                if (!guild.TryGetCharacter(characterId, out CharacterRuntimeData character))
                {
                    continue;
                }

                if (character.Status == CharacterStatus.Available &&
                    guild.TryRemoveCharacter(characterId))
                {
                    departedIds.Add(characterId);
                }
                else
                {
                    character.MarkDeparturePending();
                    pendingIds.Add(characterId);
                }
            }

            return new LoyaltyResult(
                loyaltyChange,
                affectedIds,
                departedIds,
                pendingIds);
        }

        private static List<CharacterRuntimeData> ResolveCharacters(
            GuildRuntimeData guild,
            IEnumerable<string> characterIds)
        {
            if (characterIds == null)
            {
                throw new ArgumentNullException(nameof(characterIds));
            }

            var characters = new List<CharacterRuntimeData>();
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string characterId in characterIds)
            {
                if (!uniqueIds.Add(characterId) ||
                    !guild.TryGetCharacter(characterId, out CharacterRuntimeData character))
                {
                    throw new ArgumentException(
                        $"Character ID is invalid or duplicated: {characterId}",
                        nameof(characterIds));
                }

                characters.Add(character);
            }

            return characters;
        }

        private static void ValidateArguments(
            GuildRuntimeData guild,
            BattleBalanceSettings settings)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.UnpaidSalaryLoyaltyPenalty < 0)
            {
                throw new ArgumentException(
                    "Unpaid salary loyalty penalty cannot be negative.",
                    nameof(settings));
            }
        }
    }
}
