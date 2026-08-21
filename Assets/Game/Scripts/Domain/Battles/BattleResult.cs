using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Domain.Battles
{
    public sealed class BattleResult
    {
        private readonly List<string> participantIds;

        public BattleResult(
            BattleOutcome outcome,
            float partyBasePower,
            float enemyBasePower,
            float partyPowerMultiplier,
            float enemyPowerMultiplier,
            IEnumerable<string> participantIds)
        {
            if (participantIds == null)
            {
                throw new ArgumentNullException(nameof(participantIds));
            }

            Outcome = outcome;
            PartyBasePower = partyBasePower;
            EnemyBasePower = enemyBasePower;
            PartyPowerMultiplier = partyPowerMultiplier;
            EnemyPowerMultiplier = enemyPowerMultiplier;
            FinalPartyPower = partyBasePower * partyPowerMultiplier;
            FinalEnemyPower = enemyBasePower * enemyPowerMultiplier;
            PowerDifference = FinalPartyPower - FinalEnemyPower;
            this.participantIds = new List<string>(participantIds);
        }

        public BattleOutcome Outcome { get; }
        public float PartyBasePower { get; }
        public float EnemyBasePower { get; }
        public float PartyPowerMultiplier { get; }
        public float EnemyPowerMultiplier { get; }
        public float FinalPartyPower { get; }
        public float FinalEnemyPower { get; }
        public float PowerDifference { get; }
        public IReadOnlyList<string> ParticipantIds => participantIds;
    }
}
