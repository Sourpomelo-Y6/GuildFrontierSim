using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Processing.Leadership
{
    public sealed class LeadershipResult
    {
        public LeadershipResult(
            LeadershipOutcome outcome,
            string previousLeaderId,
            string leaderId,
            string actingLeaderId,
            IEnumerable<string> departedCharacterIds,
            IEnumerable<string> pendingDepartureCharacterIds)
        {
            Outcome = outcome;
            PreviousLeaderId = previousLeaderId ?? string.Empty;
            LeaderId = leaderId ?? string.Empty;
            ActingLeaderId = actingLeaderId ?? string.Empty;
            DepartedCharacterIds = Copy(
                departedCharacterIds,
                nameof(departedCharacterIds));
            PendingDepartureCharacterIds = Copy(
                pendingDepartureCharacterIds,
                nameof(pendingDepartureCharacterIds));
        }

        public LeadershipOutcome Outcome { get; }
        public string PreviousLeaderId { get; }
        public string LeaderId { get; }
        public string ActingLeaderId { get; }
        public IReadOnlyList<string> DepartedCharacterIds { get; }
        public IReadOnlyList<string> PendingDepartureCharacterIds { get; }

        private static IReadOnlyList<string> Copy(
            IEnumerable<string> values,
            string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return new List<string>(values);
        }
    }
}
