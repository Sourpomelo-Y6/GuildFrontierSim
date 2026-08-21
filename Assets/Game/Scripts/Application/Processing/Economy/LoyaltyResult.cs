using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Processing.Economy
{
    public sealed class LoyaltyResult
    {
        public LoyaltyResult(
            int loyaltyChange,
            IEnumerable<string> affectedCharacterIds,
            IEnumerable<string> departedCharacterIds,
            IEnumerable<string> pendingDepartureCharacterIds)
        {
            LoyaltyChange = loyaltyChange;
            AffectedCharacterIds = Copy(affectedCharacterIds, nameof(affectedCharacterIds));
            DepartedCharacterIds = Copy(departedCharacterIds, nameof(departedCharacterIds));
            PendingDepartureCharacterIds = Copy(
                pendingDepartureCharacterIds,
                nameof(pendingDepartureCharacterIds));
        }

        public int LoyaltyChange { get; }
        public IReadOnlyList<string> AffectedCharacterIds { get; }
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
