using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Definitions;

namespace GuildFrontierSim.Application.Processing.Expeditions
{
    public sealed class ExpeditionStartRequest
    {
        private readonly List<string> excludedCharacterIds;

        public ExpeditionStartRequest(
            string expeditionId,
            ExpeditionAreaDefinition area,
            IEnumerable<string> excludedCharacterIds = null)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                throw new ArgumentException("Expedition ID cannot be empty.", nameof(expeditionId));
            }

            Area = area ?? throw new ArgumentNullException(nameof(area));
            ExpeditionId = expeditionId.Trim();
            this.excludedCharacterIds = excludedCharacterIds == null
                ? new List<string>()
                : new List<string>(excludedCharacterIds);

            for (int index = 0; index < this.excludedCharacterIds.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(this.excludedCharacterIds[index]))
                {
                    throw new ArgumentException(
                        "Excluded character IDs cannot contain empty values.",
                        nameof(excludedCharacterIds));
                }
            }
        }

        public string ExpeditionId { get; }
        public ExpeditionAreaDefinition Area { get; }
        public IReadOnlyList<string> ExcludedCharacterIds => excludedCharacterIds;
    }
}
