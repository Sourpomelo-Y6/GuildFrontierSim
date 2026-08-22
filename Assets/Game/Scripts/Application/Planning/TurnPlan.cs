using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;

namespace GuildFrontierSim.Application.Planning
{
    public sealed class TurnPlan
    {
        private readonly HashSet<TurnDecisionType> cpuDecisions;

        internal TurnPlan(
            int targetTurn,
            int guildRevision,
            DefenseAssignment defenseAssignment,
            ExpeditionAssignment expeditionAssignment,
            string actingLeaderCharacterId,
            IEnumerable<TurnDecisionType> cpuDecisions)
        {
            TargetTurn = targetTurn;
            GuildRevision = guildRevision;
            DefenseAssignment = defenseAssignment;
            ExpeditionAssignment = expeditionAssignment;
            ActingLeaderCharacterId = actingLeaderCharacterId ?? string.Empty;
            this.cpuDecisions = new HashSet<TurnDecisionType>(
                cpuDecisions ?? throw new ArgumentNullException(nameof(cpuDecisions)));
        }

        public int TargetTurn { get; }
        public int GuildRevision { get; }
        public DefenseAssignment DefenseAssignment { get; }
        public ExpeditionAssignment ExpeditionAssignment { get; }
        public string ActingLeaderCharacterId { get; }

        public bool IsDelegatedToCpu(TurnDecisionType decision)
        {
            return cpuDecisions.Contains(decision);
        }
    }
}
