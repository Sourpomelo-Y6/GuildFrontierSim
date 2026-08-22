using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Assignments.Leadership;

namespace GuildFrontierSim.Application.Planning
{
    public sealed class TurnPlanningSession
    {
        private readonly HashSet<TurnDecisionType> requiredDecisions;
        private readonly HashSet<TurnDecisionType> resolvedDecisions =
            new HashSet<TurnDecisionType>();
        private readonly HashSet<TurnDecisionType> cpuDecisions =
            new HashSet<TurnDecisionType>();
        private readonly bool canDelegateToCpu;

        internal TurnPlanningSession(
            int targetTurn,
            int guildRevision,
            IEnumerable<TurnDecisionType> requiredDecisions,
            IEnumerable<TurnDecisionType> initialCpuDecisions,
            bool canDelegateToCpu)
        {
            TargetTurn = targetTurn;
            GuildRevision = guildRevision;
            this.canDelegateToCpu = canDelegateToCpu;
            this.requiredDecisions = new HashSet<TurnDecisionType>(requiredDecisions);
            foreach (TurnDecisionType decision in initialCpuDecisions)
            {
                if (this.requiredDecisions.Contains(decision))
                {
                    cpuDecisions.Add(decision);
                    resolvedDecisions.Add(decision);
                }
            }

            RefreshStatus();
        }

        public int TargetTurn { get; }
        public int GuildRevision { get; }
        public TurnPlanningStatus Status { get; private set; }
        public DefenseAssignment DefenseAssignment { get; private set; }
        public ExpeditionAssignment ExpeditionAssignment { get; private set; }
        public string ActingLeaderCharacterId { get; private set; } = string.Empty;
        public ActingLeaderAssignment ActingLeaderAssignment { get; private set; }
        internal IEnumerable<TurnDecisionType> CpuDecisions => cpuDecisions;

        public bool IsRequired(TurnDecisionType decision) => requiredDecisions.Contains(decision);
        public bool IsResolved(TurnDecisionType decision) => resolvedDecisions.Contains(decision);

        public void SubmitDefense(DefenseAssignment assignment, int guildRevision)
        {
            EnsureSubmissionAllowed(TurnDecisionType.DefenseMembers, guildRevision);
            DefenseAssignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            Resolve(TurnDecisionType.DefenseMembers);
        }

        public void SubmitExpedition(ExpeditionAssignment assignment, int guildRevision)
        {
            EnsureRevision(guildRevision);
            if (!requiredDecisions.Contains(TurnDecisionType.ExpeditionMembers) ||
                !requiredDecisions.Contains(TurnDecisionType.ExpeditionArea))
            {
                throw new InvalidOperationException("An expedition decision is not required.");
            }
            if (resolvedDecisions.Contains(TurnDecisionType.ExpeditionMembers) ||
                resolvedDecisions.Contains(TurnDecisionType.ExpeditionArea))
            {
                throw new InvalidOperationException("The expedition decision is already resolved.");
            }

            ExpeditionAssignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            Resolve(TurnDecisionType.ExpeditionMembers, refresh: false);
            Resolve(TurnDecisionType.ExpeditionArea);
        }

        public void SubmitActingLeader(string characterId, int guildRevision)
        {
            EnsureSubmissionAllowed(TurnDecisionType.ActingLeader, guildRevision);
            ActingLeaderAssignment = new ActingLeaderAssignment(characterId);
            ActingLeaderCharacterId = ActingLeaderAssignment.CharacterId;
            Resolve(TurnDecisionType.ActingLeader);
        }

        public void DelegateToCpu(TurnDecisionType decision, int guildRevision)
        {
            EnsureRevision(guildRevision);
            if (!canDelegateToCpu)
                throw new InvalidOperationException("This policy does not allow CPU delegation.");
            EnsureSubmissionAllowed(decision, guildRevision);
            if (decision == TurnDecisionType.ExpeditionMembers ||
                decision == TurnDecisionType.ExpeditionArea)
            {
                cpuDecisions.Add(TurnDecisionType.ExpeditionMembers);
                cpuDecisions.Add(TurnDecisionType.ExpeditionArea);
                Resolve(TurnDecisionType.ExpeditionMembers, refresh: false);
                Resolve(TurnDecisionType.ExpeditionArea);
                return;
            }

            cpuDecisions.Add(decision);
            Resolve(decision);
        }

        internal void FinalizePlan(int guildRevision)
        {
            EnsureRevision(guildRevision);
            if (Status != TurnPlanningStatus.Ready)
                throw new InvalidOperationException("The turn plan is incomplete.");
            Status = TurnPlanningStatus.Finalized;
        }

        public void MarkApplied(int guildRevision)
        {
            EnsureRevision(guildRevision);
            if (Status != TurnPlanningStatus.Finalized)
                throw new InvalidOperationException("Only a finalized plan can be applied.");
            Status = TurnPlanningStatus.Applied;
        }

        private void EnsureSubmissionAllowed(TurnDecisionType decision, int revision)
        {
            EnsureRevision(revision);
            if (Status == TurnPlanningStatus.Finalized || Status == TurnPlanningStatus.Applied)
                throw new InvalidOperationException("The turn plan can no longer be changed.");
            if (!requiredDecisions.Contains(decision))
                throw new InvalidOperationException("This decision is not required.");
            if (resolvedDecisions.Contains(decision))
                throw new InvalidOperationException("This decision is already resolved.");
        }

        private void EnsureRevision(int revision)
        {
            if (revision != GuildRevision)
                throw new InvalidOperationException("The turn plan revision is stale.");
        }

        private void Resolve(TurnDecisionType decision, bool refresh = true)
        {
            resolvedDecisions.Add(decision);
            if (refresh) RefreshStatus();
        }

        private void RefreshStatus()
        {
            Status = resolvedDecisions.IsSupersetOf(requiredDecisions)
                ? TurnPlanningStatus.Ready
                : TurnPlanningStatus.WaitingForInput;
        }
    }
}
