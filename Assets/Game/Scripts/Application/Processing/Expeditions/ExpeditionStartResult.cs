using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Expeditions;

namespace GuildFrontierSim.Application.Processing.Expeditions
{
    public sealed class ExpeditionStartResult
    {
        private readonly List<string> participantIds;

        public ExpeditionStartResult(
            ExpeditionStartOutcome outcome,
            ExpeditionRuntimeData expedition,
            IEnumerable<string> participantIds,
            int requestedMemberCount)
        {
            if (participantIds == null)
            {
                throw new ArgumentNullException(nameof(participantIds));
            }

            if (requestedMemberCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedMemberCount));
            }

            Outcome = outcome;
            Expedition = expedition;
            this.participantIds = new List<string>(participantIds);
            RequestedMemberCount = requestedMemberCount;
        }

        public ExpeditionStartOutcome Outcome { get; }
        public ExpeditionRuntimeData Expedition { get; }
        public IReadOnlyList<string> ParticipantIds => participantIds;
        public int RequestedMemberCount { get; }
        public int UnfilledMemberCount => RequestedMemberCount - participantIds.Count;
    }
}
