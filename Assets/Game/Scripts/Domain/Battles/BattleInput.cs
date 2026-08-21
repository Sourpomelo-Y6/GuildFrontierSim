using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Characters;

namespace GuildFrontierSim.Domain.Battles
{
    public sealed class BattleInput
    {
        private readonly List<CharacterRuntimeData> participants;

        public BattleInput(
            IEnumerable<CharacterRuntimeData> participants,
            float enemyBasePower)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (enemyBasePower < 0f || float.IsNaN(enemyBasePower) || float.IsInfinity(enemyBasePower))
            {
                throw new ArgumentOutOfRangeException(nameof(enemyBasePower));
            }

            this.participants = new List<CharacterRuntimeData>(participants);
            if (this.participants.Count == 0)
            {
                throw new ArgumentException("A battle requires at least one participant.", nameof(participants));
            }

            for (int index = 0; index < this.participants.Count; index++)
            {
                CharacterRuntimeData participant = this.participants[index];
                if (participant == null)
                {
                    throw new ArgumentException("Participants cannot contain null.", nameof(participants));
                }

                if (participant.CurrentHp <= 0)
                {
                    throw new ArgumentException(
                        $"Participant {participant.CharacterId} has no HP.",
                        nameof(participants));
                }
            }

            EnemyBasePower = enemyBasePower;
        }

        public IReadOnlyList<CharacterRuntimeData> Participants => participants;
        public float EnemyBasePower { get; }
    }
}
