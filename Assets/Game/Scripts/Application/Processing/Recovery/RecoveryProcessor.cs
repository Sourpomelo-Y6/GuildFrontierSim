using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Processing.Recovery
{
    public sealed class RecoveryProcessor
    {
        private readonly LoyaltyProcessor loyaltyProcessor;

        public RecoveryProcessor(LoyaltyProcessor loyaltyProcessor = null)
        {
            this.loyaltyProcessor = loyaltyProcessor ?? new LoyaltyProcessor();
        }

        public RecoveryResult Process(GuildRuntimeData guild)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            var advancedIds = new List<string>();
            var recoveredIds = new List<string>();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                if (!IsRecoveryStatus(character.Status))
                {
                    continue;
                }

                advancedIds.Add(character.CharacterId);
                character.AdvanceUnavailableTurn();
                if (character.Status == CharacterStatus.Available)
                {
                    recoveredIds.Add(character.CharacterId);
                }
            }

            LoyaltyResult departureResult =
                loyaltyProcessor.ResolvePendingDepartures(guild);
            return new RecoveryResult(
                advancedIds,
                recoveredIds,
                departureResult.DepartedCharacterIds,
                departureResult.PendingDepartureCharacterIds);
        }

        private static bool IsRecoveryStatus(CharacterStatus status)
        {
            return status == CharacterStatus.Injured ||
                   status == CharacterStatus.Hospitalized ||
                   status == CharacterStatus.Resting;
        }
    }
}
