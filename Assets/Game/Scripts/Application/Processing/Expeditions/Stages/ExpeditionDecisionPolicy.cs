using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;

namespace GuildFrontierSim.Application.Processing.Expeditions.Stages
{
    public sealed class ExpeditionDecisionPolicy
    {
        public ExpeditionDecision Decide(
            ExpeditionRuntimeData expedition,
            IReadOnlyList<CharacterRuntimeData> participants,
            ExpeditionBalanceSettings settings)
        {
            if (expedition == null)
            {
                throw new ArgumentNullException(nameof(expedition));
            }

            if (participants == null || participants.Count == 0)
            {
                throw new ArgumentException("Expedition participants are required.", nameof(participants));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (expedition.CurrentStage >= expedition.MaximumStages ||
                expedition.TemporaryFunds >= settings.ReturnFundsThreshold ||
                CalculatePartyHpRatio(participants) < settings.MinimumPartyHpRatioToContinue)
            {
                return ExpeditionDecision.Return;
            }

            return ExpeditionDecision.Continue;
        }

        private static float CalculatePartyHpRatio(
            IReadOnlyList<CharacterRuntimeData> participants)
        {
            long currentHp = 0;
            long maximumHp = 0;
            for (int index = 0; index < participants.Count; index++)
            {
                CharacterRuntimeData participant = participants[index] ??
                    throw new ArgumentException("Participants cannot contain null.", nameof(participants));
                currentHp += participant.CurrentHp;
                maximumHp += participant.MaxHp;
            }

            return maximumHp == 0 ? 0f : (float)currentHp / maximumHp;
        }
    }
}
