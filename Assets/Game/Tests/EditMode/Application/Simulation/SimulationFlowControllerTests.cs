using System;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Planning;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Application.Simulation;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Tests.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Simulation
{
    public sealed class SimulationFlowControllerTests
    {
        [Test]
        public void PlayerFlow_WaitsForPlanThenAppliesExactlyOnce()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));
            var controller = new SimulationFlowController(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"));
            int executionCount = 0;

            TurnPlanningSession session = controller.BeginTurnPlanning(
                requiresDefense: true,
                requiresExpedition: false);
            Assert.That(controller.State, Is.EqualTo(SimulationFlowState.PlanningTurn));
            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.WaitingForInput));

            controller.SubmitDefense(
                new DefenseAssignment(100f, new string[0]), guild.Revision);
            controller.ApplyTurnPlan(plan =>
            {
                executionCount++;
                Assert.That(plan.DefenseAssignment, Is.Not.Null);
                guild.AdvanceTurnNumber();
                return null;
            });

            Assert.That(controller.State, Is.EqualTo(SimulationFlowState.Ready));
            Assert.That(executionCount, Is.EqualTo(1));
            Assert.That(guild.CurrentTurn, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() =>
                controller.ApplyTurnPlan(_ => null));
        }

        [Test]
        public void ApplyTurnPlan_WhenPlanIsIncomplete_LeavesPlanningState()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));
            var controller = new SimulationFlowController(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"));
            controller.BeginTurnPlanning(requiresDefense: true, requiresExpedition: false);

            Assert.Throws<InvalidOperationException>(() =>
                controller.ApplyTurnPlan(_ => null));
            Assert.That(controller.State, Is.EqualTo(SimulationFlowState.PlanningTurn));
            Assert.That(guild.CurrentTurn, Is.Zero);
        }

        [Test]
        public void SubmitDecision_WithStaleRevision_IsRejectedWithoutMutation()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));
            var controller = new SimulationFlowController(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"));
            controller.BeginTurnPlanning(requiresDefense: true, requiresExpedition: false);
            int oldRevision = guild.Revision;
            guild.MarkStateChanged();

            Assert.Throws<InvalidOperationException>(() => controller.SubmitDefense(
                new DefenseAssignment(100f, new string[0]), oldRevision));
            Assert.That(controller.State, Is.EqualTo(SimulationFlowState.PlanningTurn));
        }

        [Test]
        public void ExpeditionVictory_PausesAndResumesThroughRealStageProcessor()
        {
            using (var context = new ExpeditionContext(GuildControlMode.Player))
            {
                context.Controller.BeginTurnPlanning(false, false);
                context.Controller.ApplyTurnPlan(_ => context.Resolve());

                Assert.That(context.Controller.State,
                    Is.EqualTo(SimulationFlowState.WaitingForExpeditionDecision));
                Assert.That(context.Expedition.Status,
                    Is.EqualTo(ExpeditionStatus.AwaitingDecision));

                context.Controller.SubmitExpeditionDecision(
                    ExpeditionDecision.Continue,
                    (pending, decision) => context.Apply(pending, decision));

                Assert.That(context.Controller.State, Is.EqualTo(SimulationFlowState.Ready));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Active));
                Assert.That(context.Expedition.CurrentStage, Is.EqualTo(2));
            }
        }

        [Test]
        public void ExpeditionDecision_WhenExecutorFails_RemainsWaiting()
        {
            using (var context = new ExpeditionContext(GuildControlMode.Player))
            {
                context.Controller.BeginTurnPlanning(false, false);
                context.Controller.ApplyTurnPlan(_ => context.Resolve());

                Assert.Throws<InvalidOperationException>(() =>
                    context.Controller.SubmitExpeditionDecision(
                        ExpeditionDecision.Return,
                        (_, __) => throw new InvalidOperationException("failure")));

                Assert.That(context.Controller.State,
                    Is.EqualTo(SimulationFlowState.WaitingForExpeditionDecision));
                Assert.That(context.Controller.PendingExpeditionDecision, Is.Not.Null);
            }
        }

        [Test]
        public void AdvanceCpuTurn_CompletesPlanningAndPendingDecisionInOneCall()
        {
            using (var context = new ExpeditionContext(GuildControlMode.Cpu))
            {
                context.Controller.AdvanceCpuTurn(
                    requiresDefense: true,
                    requiresExpedition: true,
                    requiresActingLeader: false,
                    turnExecutor: _ => context.Resolve(),
                    decisionExecutor: (pending, decision) => context.Apply(pending, decision));

                Assert.That(context.Controller.State, Is.EqualTo(SimulationFlowState.Ready));
                Assert.That(context.Expedition.CurrentStage, Is.EqualTo(2));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Active));
            }
        }

        private sealed class ExpeditionContext : IDisposable
        {
            private readonly BattleBalanceSettings battleSettings;
            private readonly ExpeditionBalanceSettings expeditionSettings;
            private readonly ExpeditionStageProcessor processor;

            public ExpeditionContext(GuildControlMode mode)
            {
                CharacterRuntimeData participant = Character("participant", isPlayer: true);
                participant.SetStatus(CharacterStatus.Expedition);
                Expedition = new ExpeditionRuntimeData(
                    "exp-1", "forest", new[] { "participant" }, 1f, 3, 1f, false);
                Guild = Guild(participant);
                Guild.AddExpedition(Expedition);
                Controller = new SimulationFlowController(
                    Guild,
                    mode == GuildControlMode.Cpu
                        ? new GuildControlPolicy(GuildControlMode.Cpu)
                        : new GuildControlPolicy(GuildControlMode.Player, "participant"));
                battleSettings = TestAssetFactory.CreateBattleSettings(
                    minimumPowerMultiplier: 1f,
                    maximumPowerMultiplier: 1f);
                expeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                    minimumStageFunds: 10,
                    maximumStageFunds: 10,
                    returnFundsThreshold: 999,
                    minimumPartyHpRatioToContinue: 0f,
                    captiveRescueChance: 0f);
                processor = new ExpeditionStageProcessor(new SequenceRandomSource(
                    integerValues: new[] { 10 },
                    floatValues: new[] { 0.5f, 0.5f }));
            }

            public GuildRuntimeData Guild { get; }
            public ExpeditionRuntimeData Expedition { get; }
            public SimulationFlowController Controller { get; }

            public PendingExpeditionDecision Resolve()
            {
                return processor.ResolveStageBattle(
                    Guild,
                    Expedition.ExpeditionId,
                    battleSettings,
                    expeditionSettings).PendingDecision;
            }

            public void Apply(PendingExpeditionDecision pending, ExpeditionDecision decision)
            {
                processor.ApplyDecision(Guild, pending, decision, expeditionSettings);
            }

            public void Dispose()
            {
                TestAssetFactory.Destroy(battleSettings, expeditionSettings);
            }
        }

        private static GuildRuntimeData Guild(params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData("Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData Character(string id, bool isPlayer = false)
        {
            return new CharacterRuntimeData(
                id, 1, 100, 100, 10, 10, 10, 30, isPlayerCharacter: isPlayer);
        }
    }
}
