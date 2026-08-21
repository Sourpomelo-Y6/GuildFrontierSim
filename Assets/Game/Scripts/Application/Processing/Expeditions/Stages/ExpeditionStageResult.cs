using System;
using GuildFrontierSim.Domain.Battles;

namespace GuildFrontierSim.Application.Processing.Expeditions.Stages
{
    public sealed class ExpeditionStageResult
    {
        public ExpeditionStageResult(
            string expeditionId,
            ExpeditionStageOutcome outcome,
            BattleResult battleResult,
            int stageReward,
            int temporaryFundsAfterStage,
            string rescuedCharacterId = "")
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                throw new ArgumentException("Expedition ID cannot be empty.", nameof(expeditionId));
            }

            if (battleResult == null)
            {
                throw new ArgumentNullException(nameof(battleResult));
            }

            if (stageReward < 0 || temporaryFundsAfterStage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageReward));
            }

            ExpeditionId = expeditionId;
            Outcome = outcome;
            BattleResult = battleResult;
            StageReward = stageReward;
            TemporaryFundsAfterStage = temporaryFundsAfterStage;
            RescuedCharacterId = rescuedCharacterId ?? string.Empty;
        }

        public string ExpeditionId { get; }
        public ExpeditionStageOutcome Outcome { get; }
        public BattleResult BattleResult { get; }
        public int StageReward { get; }
        public int TemporaryFundsAfterStage { get; }
        public string RescuedCharacterId { get; }
    }
}
