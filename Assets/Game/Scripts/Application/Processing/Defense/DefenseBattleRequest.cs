using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Processing.Defense
{
    public sealed class DefenseBattleRequest
    {
        private readonly List<string> excludedCharacterIds;

        public DefenseBattleRequest(
            float enemyBasePower,
            IEnumerable<string> excludedCharacterIds = null)
        {
            if (enemyBasePower < 0f || float.IsNaN(enemyBasePower) || float.IsInfinity(enemyBasePower))
            {
                throw new ArgumentOutOfRangeException(nameof(enemyBasePower));
            }

            EnemyBasePower = enemyBasePower;
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

        public float EnemyBasePower { get; }
        public IReadOnlyList<string> ExcludedCharacterIds => excludedCharacterIds;
    }
}
