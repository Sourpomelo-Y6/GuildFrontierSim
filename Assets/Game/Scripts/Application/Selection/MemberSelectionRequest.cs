using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Selection
{
    public sealed class MemberSelectionRequest
    {
        private readonly HashSet<string> excludedCharacterIds;

        public MemberSelectionRequest(
            MemberSelectionPurpose purpose,
            IEnumerable<string> excludedCharacterIds = null)
        {
            Purpose = purpose;
            this.excludedCharacterIds = new HashSet<string>(StringComparer.Ordinal);

            if (excludedCharacterIds == null)
            {
                return;
            }

            foreach (string characterId in excludedCharacterIds)
            {
                if (string.IsNullOrWhiteSpace(characterId))
                {
                    throw new ArgumentException(
                        "Excluded character IDs cannot contain empty values.",
                        nameof(excludedCharacterIds));
                }

                this.excludedCharacterIds.Add(characterId);
            }
        }

        public MemberSelectionPurpose Purpose { get; }
        public IReadOnlyCollection<string> ExcludedCharacterIds => excludedCharacterIds;

        public bool IsExcluded(string characterId)
        {
            return excludedCharacterIds.Contains(characterId);
        }
    }
}
