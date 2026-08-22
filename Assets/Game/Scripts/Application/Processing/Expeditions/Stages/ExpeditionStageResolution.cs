using System;

namespace GuildFrontierSim.Application.Processing.Expeditions.Stages
{
    public sealed class ExpeditionStageResolution
    {
        public ExpeditionStageResolution(
            ExpeditionStageResult result,
            PendingExpeditionDecision pendingDecision)
        {
            if ((result == null) == (pendingDecision == null))
                throw new ArgumentException("Exactly one stage outcome must be provided.");
            Result = result;
            PendingDecision = pendingDecision;
        }

        public ExpeditionStageResult Result { get; }
        public PendingExpeditionDecision PendingDecision { get; }
        public bool IsWaitingForDecision => PendingDecision != null;
    }
}
