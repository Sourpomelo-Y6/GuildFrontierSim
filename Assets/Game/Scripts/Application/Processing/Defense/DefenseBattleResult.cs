using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Battles;

namespace GuildFrontierSim.Application.Processing.Defense
{
    public sealed class DefenseBattleResult
    {
        private readonly List<string> defenderIds;
        private readonly List<string> injuredCharacterIds;
        private readonly List<string> hospitalizedCharacterIds;

        public DefenseBattleResult(
            DefenseOutcome outcome,
            BattleResult battleResult,
            int reward,
            IEnumerable<string> defenderIds,
            IEnumerable<string> injuredCharacterIds,
            IEnumerable<string> hospitalizedCharacterIds)
        {
            if (reward < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reward));
            }

            Outcome = outcome;
            BattleResult = battleResult;
            Reward = reward;
            this.defenderIds = Copy(defenderIds, nameof(defenderIds));
            this.injuredCharacterIds = Copy(
                injuredCharacterIds,
                nameof(injuredCharacterIds));
            this.hospitalizedCharacterIds = Copy(
                hospitalizedCharacterIds,
                nameof(hospitalizedCharacterIds));
        }

        public DefenseOutcome Outcome { get; }
        public BattleResult BattleResult { get; }
        public int Reward { get; }
        public IReadOnlyList<string> DefenderIds => defenderIds;
        public IReadOnlyList<string> InjuredCharacterIds => injuredCharacterIds;
        public IReadOnlyList<string> HospitalizedCharacterIds => hospitalizedCharacterIds;

        private static List<string> Copy(IEnumerable<string> values, string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return new List<string>(values);
        }
    }
}
