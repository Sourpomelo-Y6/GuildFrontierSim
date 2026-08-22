using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Assignments.Expeditions
{
    public sealed class ExpeditionAssignment
    {
        private readonly List<string> participantIds;

        public ExpeditionAssignment(
            string expeditionId,
            string areaId,
            IEnumerable<string> participantIds)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
                throw new ArgumentException("Expedition ID cannot be empty.", nameof(expeditionId));
            if (string.IsNullOrWhiteSpace(areaId))
                throw new ArgumentException("Area ID cannot be empty.", nameof(areaId));
            if (participantIds == null)
                throw new ArgumentNullException(nameof(participantIds));

            ExpeditionId = expeditionId.Trim();
            AreaId = areaId.Trim();
            this.participantIds = new List<string>(participantIds);
        }

        public string ExpeditionId { get; }
        public string AreaId { get; }
        public IReadOnlyList<string> ParticipantIds => participantIds;
    }
}
