using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Processing.Recovery
{
    public sealed class RecoveryResult
    {
        public RecoveryResult(
            IEnumerable<string> advancedCharacterIds,
            IEnumerable<string> recoveredCharacterIds,
            IEnumerable<string> departedCharacterIds,
            IEnumerable<string> pendingDepartureCharacterIds)
        {
            AdvancedCharacterIds = Copy(
                advancedCharacterIds,
                nameof(advancedCharacterIds));
            RecoveredCharacterIds = Copy(
                recoveredCharacterIds,
                nameof(recoveredCharacterIds));
            DepartedCharacterIds = Copy(
                departedCharacterIds,
                nameof(departedCharacterIds));
            PendingDepartureCharacterIds = Copy(
                pendingDepartureCharacterIds,
                nameof(pendingDepartureCharacterIds));
        }

        public IReadOnlyList<string> AdvancedCharacterIds { get; }
        public IReadOnlyList<string> RecoveredCharacterIds { get; }
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
