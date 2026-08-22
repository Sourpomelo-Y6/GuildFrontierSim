using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Battles;

namespace GuildFrontierSim.Application.Processing.Expeditions.Stages
{
    public sealed class PendingExpeditionDecision
    {
        private readonly List<ExpeditionParticipantHp> participantHitPoints;

        internal PendingExpeditionDecision(
            string expeditionId,
            int stageNumber,
            BattleResult battleResult,
            int stageReward,
            int temporaryFunds,
            string rescuedCharacterId,
            IEnumerable<ExpeditionParticipantHp> participantHitPoints,
            int guildRevision)
        {
            ExpeditionId = expeditionId;
            StageNumber = stageNumber;
            BattleResult = battleResult ?? throw new ArgumentNullException(nameof(battleResult));
            StageReward = stageReward;
            TemporaryFunds = temporaryFunds;
            RescuedCharacterId = rescuedCharacterId ?? string.Empty;
            this.participantHitPoints = new List<ExpeditionParticipantHp>(participantHitPoints);
            GuildRevision = guildRevision;
        }

        public string ExpeditionId { get; }
        public int StageNumber { get; }
        public BattleResult BattleResult { get; }
        public int StageReward { get; }
        public int TemporaryFunds { get; }
        public string RescuedCharacterId { get; }
        public IReadOnlyList<ExpeditionParticipantHp> ParticipantHitPoints => participantHitPoints;
        public int GuildRevision { get; }
        public bool IsApplied { get; private set; }

        internal void MarkApplied()
        {
            if (IsApplied) throw new InvalidOperationException("The decision was already applied.");
            IsApplied = true;
        }
    }
}
