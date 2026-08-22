using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Assignments.Defense
{
    public sealed class DefenseAssignment
    {
        private readonly List<string> defenderIds;

        public DefenseAssignment(float enemyBasePower, IEnumerable<string> defenderIds)
        {
            if (float.IsNaN(enemyBasePower) || float.IsInfinity(enemyBasePower) ||
                enemyBasePower < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyBasePower));
            }

            if (defenderIds == null)
            {
                throw new ArgumentNullException(nameof(defenderIds));
            }

            EnemyBasePower = enemyBasePower;
            this.defenderIds = new List<string>(defenderIds);
        }

        public float EnemyBasePower { get; }
        public IReadOnlyList<string> DefenderIds => defenderIds;
    }
}
