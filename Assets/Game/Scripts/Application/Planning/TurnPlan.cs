using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Assignments.Leadership;

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
            ActingLeaderAssignment actingLeaderAssignment,
            IEnumerable<TurnDecisionType> cpuDecisions)
        {
            TargetTurn = targetTurn;
            GuildRevision = guildRevision;
            DefenseAssignment = defenseAssignment;
            ExpeditionAssignment = expeditionAssignment;
            ActingLeaderAssignment = actingLeaderAssignment;
            this.cpuDecisions = new HashSet<TurnDecisionType>(
                cpuDecisions ?? throw new ArgumentNullException(nameof(cpuDecisions)));
        }

        public int TargetTurn { get; }
        public int GuildRevision { get; }
        public DefenseAssignment DefenseAssignment { get; }
        public ExpeditionAssignment ExpeditionAssignment { get; }
        public ActingLeaderAssignment ActingLeaderAssignment { get; }
        public string ActingLeaderCharacterId => ActingLeaderAssignment?.CharacterId ?? string.Empty;

        public bool IsDelegatedToCpu(TurnDecisionType decision)
        {
            return cpuDecisions.Contains(decision);
        }
    }
}
