using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Processing.Economy
{
    public sealed class SalaryResult
    {
        private readonly List<string> characterIds;

        public SalaryResult(
            SalaryOutcome outcome,
            int totalSalary,
            IEnumerable<string> characterIds)
        {
            if (totalSalary < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalSalary));
            }

            if (characterIds == null)
            {
                throw new ArgumentNullException(nameof(characterIds));
            }

            Outcome = outcome;
            TotalSalary = totalSalary;
            this.characterIds = new List<string>(characterIds);
        }

        public SalaryOutcome Outcome { get; }
        public int TotalSalary { get; }
        public IReadOnlyList<string> CharacterIds => characterIds;
        public bool WasPaid => Outcome == SalaryOutcome.Paid;
    }
}
