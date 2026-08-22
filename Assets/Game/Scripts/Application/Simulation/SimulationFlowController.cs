using System;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Planning;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Simulation
{
    public sealed class SimulationFlowController
    {
        private readonly GuildRuntimeData guild;
        private readonly GuildControlPolicy controlPolicy;
        private readonly TurnPlanBuilder planBuilder;

        public SimulationFlowController(
            GuildRuntimeData guild,
            GuildControlPolicy controlPolicy,
            TurnPlanBuilder planBuilder = null)
        {
            this.guild = guild ?? throw new ArgumentNullException(nameof(guild));
            this.controlPolicy = controlPolicy ??
                throw new ArgumentNullException(nameof(controlPolicy));
            this.planBuilder = planBuilder ?? new TurnPlanBuilder();
        }

        public SimulationFlowState State { get; private set; } = SimulationFlowState.Ready;
        public TurnPlanningSession PlanningSession { get; private set; }
        public TurnPlan CurrentPlan { get; private set; }
        public PendingExpeditionDecision PendingExpeditionDecision { get; private set; }

        public TurnPlanningSession BeginTurnPlanning(
            bool requiresDefense,
            bool requiresExpedition,
            bool requiresActingLeader = false)
        {
            EnsureState(SimulationFlowState.Ready);
            PlanningSession = planBuilder.Begin(
                guild,
                controlPolicy,
                requiresDefense,
                requiresExpedition,
                requiresActingLeader);
            CurrentPlan = null;
            PendingExpeditionDecision = null;
            State = SimulationFlowState.PlanningTurn;
            return PlanningSession;
        }

        public void SubmitDefense(DefenseAssignment assignment, int guildRevision)
        {
            EnsureState(SimulationFlowState.PlanningTurn);
            EnsureCurrentRevision(guildRevision);
            PlanningSession.SubmitDefense(assignment, guildRevision);
        }

        public void SubmitExpedition(ExpeditionAssignment assignment, int guildRevision)
        {
            EnsureState(SimulationFlowState.PlanningTurn);
            EnsureCurrentRevision(guildRevision);
            PlanningSession.SubmitExpedition(assignment, guildRevision);
        }

        public void SubmitActingLeader(string characterId, int guildRevision)
        {
            EnsureState(SimulationFlowState.PlanningTurn);
            EnsureCurrentRevision(guildRevision);
            PlanningSession.SubmitActingLeader(characterId, guildRevision);
        }

        public void DelegateToCpu(TurnDecisionType decision, int guildRevision)
        {
            EnsureState(SimulationFlowState.PlanningTurn);
            EnsureCurrentRevision(guildRevision);
            PlanningSession.DelegateToCpu(decision, guildRevision);
        }

        public void ApplyTurnPlan(
            Func<TurnPlan, PendingExpeditionDecision> executor)
        {
            EnsureState(SimulationFlowState.PlanningTurn);
            if (executor == null) throw new ArgumentNullException(nameof(executor));

            CurrentPlan = planBuilder.Build(guild, PlanningSession);
            PlanningSession.MarkApplied(guild.Revision);
            State = SimulationFlowState.ApplyingTurn;
            try
            {
                PendingExpeditionDecision = executor(CurrentPlan);
                State = PendingExpeditionDecision == null
                    ? SimulationFlowState.Ready
                    : SimulationFlowState.WaitingForExpeditionDecision;
                if (State == SimulationFlowState.Ready) ClearCompletedTurn();
            }
            catch
            {
                ResetAfterFailure();
                throw;
            }
        }

        public void SubmitExpeditionDecision(
            ExpeditionDecision decision,
            Action<PendingExpeditionDecision, ExpeditionDecision> executor)
        {
            EnsureState(SimulationFlowState.WaitingForExpeditionDecision);
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            if (decision == ExpeditionDecision.DelegateToCpu &&
                !controlPolicy.CanDelegateToCpu &&
                controlPolicy.Mode == GuildControlMode.Player)
            {
                throw new InvalidOperationException("This policy does not allow CPU delegation.");
            }

            State = SimulationFlowState.ApplyingTurn;
            try
            {
                executor(PendingExpeditionDecision, decision);
                State = SimulationFlowState.Ready;
                ClearCompletedTurn();
            }
            catch
            {
                State = SimulationFlowState.WaitingForExpeditionDecision;
                throw;
            }
        }

        public void AdvanceCpuTurn(
            bool requiresDefense,
            bool requiresExpedition,
            bool requiresActingLeader,
            Func<TurnPlan, PendingExpeditionDecision> turnExecutor,
            Action<PendingExpeditionDecision, ExpeditionDecision> decisionExecutor)
        {
            if (controlPolicy.Mode != GuildControlMode.Cpu)
                throw new InvalidOperationException("This operation is only valid in CPU mode.");
            BeginTurnPlanning(requiresDefense, requiresExpedition, requiresActingLeader);
            ApplyTurnPlan(turnExecutor);
            if (State == SimulationFlowState.WaitingForExpeditionDecision)
            {
                SubmitExpeditionDecision(
                    ExpeditionDecision.DelegateToCpu,
                    decisionExecutor);
            }
        }

        private void EnsureState(SimulationFlowState expected)
        {
            if (State != expected)
                throw new InvalidOperationException(
                    $"Simulation state must be {expected}, but was {State}.");
        }

        private void EnsureCurrentRevision(int revision)
        {
            if (revision != guild.Revision)
                throw new InvalidOperationException("The guild revision is stale.");
        }

        private void ClearCompletedTurn()
        {
            PlanningSession = null;
            CurrentPlan = null;
            PendingExpeditionDecision = null;
        }

        private void ResetAfterFailure()
        {
            State = SimulationFlowState.Ready;
            ClearCompletedTurn();
        }
    }
}
