using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Planning
{
    public sealed class TurnPlanBuilder
    {
        public TurnPlanningSession Begin(
            GuildRuntimeData guild,
            GuildControlPolicy policy,
            bool requiresDefense,
            bool requiresExpedition,
            bool requiresActingLeader = false)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var required = new List<TurnDecisionType>();
            if (requiresDefense) required.Add(TurnDecisionType.DefenseMembers);
            if (requiresExpedition)
            {
                required.Add(TurnDecisionType.ExpeditionMembers);
                required.Add(TurnDecisionType.ExpeditionArea);
            }
            if (requiresActingLeader) required.Add(TurnDecisionType.ActingLeader);

            var cpuDecisions = new List<TurnDecisionType>();
            for (int index = 0; index < required.Count; index++)
            {
                if (policy.ShouldUseCpu(guild, required[index]))
                    cpuDecisions.Add(required[index]);
            }

            if (requiresExpedition &&
                (cpuDecisions.Contains(TurnDecisionType.ExpeditionMembers) ||
                 cpuDecisions.Contains(TurnDecisionType.ExpeditionArea)))
            {
                if (!cpuDecisions.Contains(TurnDecisionType.ExpeditionMembers))
                    cpuDecisions.Add(TurnDecisionType.ExpeditionMembers);
                if (!cpuDecisions.Contains(TurnDecisionType.ExpeditionArea))
                    cpuDecisions.Add(TurnDecisionType.ExpeditionArea);
            }

            return new TurnPlanningSession(
                checked(guild.CurrentTurn + 1),
                guild.Revision,
                required,
                cpuDecisions,
                policy.CanDelegateToCpu);
        }

        public TurnPlan Build(GuildRuntimeData guild, TurnPlanningSession session)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (session.TargetTurn != checked(guild.CurrentTurn + 1))
                throw new InvalidOperationException("The target turn is stale.");

            EnsureNoParticipantOverlap(session);
            session.FinalizePlan(guild.Revision);
            return new TurnPlan(
                session.TargetTurn,
                session.GuildRevision,
                session.DefenseAssignment,
                session.ExpeditionAssignment,
                session.ActingLeaderCharacterId,
                session.CpuDecisions);
        }

        private static void EnsureNoParticipantOverlap(TurnPlanningSession session)
        {
            if (session.DefenseAssignment == null || session.ExpeditionAssignment == null)
                return;

            var defenseIds = new HashSet<string>(
                session.DefenseAssignment.DefenderIds,
                StringComparer.Ordinal);
            for (int index = 0; index < session.ExpeditionAssignment.ParticipantIds.Count; index++)
            {
                if (defenseIds.Contains(session.ExpeditionAssignment.ParticipantIds[index]))
                    throw new InvalidOperationException(
                        "Defense and expedition participants cannot overlap.");
            }
        }
    }
}
