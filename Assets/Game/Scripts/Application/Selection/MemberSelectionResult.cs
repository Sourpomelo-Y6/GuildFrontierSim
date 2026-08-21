using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Characters;

namespace GuildFrontierSim.Application.Selection
{
    public sealed class MemberSelectionResult
    {
        private readonly List<CharacterRuntimeData> selectedMembers;

        public MemberSelectionResult(
            MemberSelectionPurpose purpose,
            int requestedCount,
            IEnumerable<CharacterRuntimeData> selectedMembers)
        {
            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            if (selectedMembers == null)
            {
                throw new ArgumentNullException(nameof(selectedMembers));
            }

            Purpose = purpose;
            RequestedCount = requestedCount;
            this.selectedMembers = new List<CharacterRuntimeData>(selectedMembers);
            if (this.selectedMembers.Count > requestedCount)
            {
                throw new ArgumentException(
                    "Selected member count cannot exceed the requested count.",
                    nameof(selectedMembers));
            }
        }

        public MemberSelectionPurpose Purpose { get; }
        public int RequestedCount { get; }
        public IReadOnlyList<CharacterRuntimeData> SelectedMembers => selectedMembers;
        public int UnfilledCount => RequestedCount - selectedMembers.Count;
        public bool IsComplete => UnfilledCount == 0;
    }
}
