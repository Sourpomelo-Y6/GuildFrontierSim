using System;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Tests.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Expeditions.Stages
{
    public sealed class PendingExpeditionDecisionTests
    {
        [Test]
        public void ResolveStageBattle_OnNonFinalVictory_CreatesPendingSnapshot()
        {
            using (var context = new Context())
            {
                ExpeditionStageResolution resolution = context.Resolve();

                Assert.That(resolution.IsWaitingForDecision, Is.True);
                Assert.That(resolution.Result, Is.Null);
                Assert.That(resolution.PendingDecision.StageNumber, Is.EqualTo(1));
                Assert.That(resolution.PendingDecision.StageReward, Is.EqualTo(10));
                Assert.That(resolution.PendingDecision.TemporaryFunds, Is.EqualTo(10));
                Assert.That(resolution.PendingDecision.ParticipantHitPoints, Has.Count.EqualTo(1));
                Assert.That(resolution.PendingDecision.GuildRevision, Is.EqualTo(context.Guild.Revision));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.AwaitingDecision));
                Assert.That(context.Expedition.CurrentStage, Is.EqualTo(1));
            }
        }

        [Test]
        public void ApplyDecision_Continue_AdvancesExactlyOnce()
        {
            using (var context = new Context())
            {
                PendingExpeditionDecision pending = context.Resolve().PendingDecision;

                ExpeditionStageResult result = context.Processor.ApplyDecision(
                    context.Guild,
                    pending,
                    ExpeditionDecision.Continue,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.VictoryContinued));
                Assert.That(context.Expedition.CurrentStage, Is.EqualTo(2));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Active));
                Assert.That(pending.IsApplied, Is.True);
                Assert.Throws<InvalidOperationException>(() => context.Processor.ApplyDecision(
                    context.Guild,
                    pending,
                    ExpeditionDecision.Return,
                    context.ExpeditionSettings));
            }
        }

        [Test]
        public void ApplyDecision_Return_BeginsReturnWithoutAdvancingStage()
        {
            using (var context = new Context())
            {
                PendingExpeditionDecision pending = context.Resolve().PendingDecision;

                ExpeditionStageResult result = context.Processor.ApplyDecision(
                    context.Guild,
                    pending,
                    ExpeditionDecision.Return,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.VictoryReturning));
                Assert.That(context.Expedition.CurrentStage, Is.EqualTo(1));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
            }
        }

        [Test]
        public void ApplyDecision_DelegateToCpu_UsesExistingPolicy()
        {
            using (var context = new Context(returnFundsThreshold: 10))
            {
                PendingExpeditionDecision pending = context.Resolve().PendingDecision;

                ExpeditionStageResult result = context.Processor.ApplyDecision(
                    context.Guild,
                    pending,
                    ExpeditionDecision.DelegateToCpu,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.VictoryReturning));
            }
        }

        [Test]
        public void ResolveStageBattle_WhileDecisionPending_IsRejectedWithoutAnotherReward()
        {
            using (var context = new Context())
            {
                context.Resolve();

                Assert.Throws<InvalidOperationException>(() => context.Resolve());
                Assert.That(context.Expedition.TemporaryFunds, Is.EqualTo(10));
            }
        }

        [Test]
        public void ApplyDecision_AfterRevisionChanges_IsRejectedWithoutStateChange()
        {
            using (var context = new Context())
            {
                PendingExpeditionDecision pending = context.Resolve().PendingDecision;
                context.Guild.MarkStateChanged();

                Assert.Throws<InvalidOperationException>(() => context.Processor.ApplyDecision(
                    context.Guild,
                    pending,
                    ExpeditionDecision.Continue,
                    context.ExpeditionSettings));
                Assert.That(context.Expedition.Status,
                    Is.EqualTo(ExpeditionStatus.AwaitingDecision));
                Assert.That(pending.IsApplied, Is.False);
            }
        }

        private sealed class Context : IDisposable
        {
            public Context(int returnFundsThreshold = 999)
            {
                Participant = new CharacterRuntimeData(
                    "participant", 1, 100, 100, 10, 10, 10, 30);
                Participant.SetStatus(CharacterStatus.Expedition);
                Expedition = new ExpeditionRuntimeData(
                    "expedition-1", "forest", new[] { "participant" }, 1f, 3, 1f, false);
                Guild = new GuildRuntimeData(
                    "Guild", 100, new[] { Participant }, Participant.CharacterId);
                Guild.AddExpedition(Expedition);
                BattleSettings = TestAssetFactory.CreateBattleSettings(
                    minimumPowerMultiplier: 1f,
                    maximumPowerMultiplier: 1f);
                ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                    minimumStageFunds: 10,
                    maximumStageFunds: 10,
                    returnFundsThreshold: returnFundsThreshold,
                    minimumPartyHpRatioToContinue: 0f,
                    captiveRescueChance: 0f);
                Processor = new ExpeditionStageProcessor(new SequenceRandomSource(
                    integerValues: new[] { 10 },
                    floatValues: new[] { 0.5f, 0.5f }));
            }

            public CharacterRuntimeData Participant { get; }
            public ExpeditionRuntimeData Expedition { get; }
            public GuildRuntimeData Guild { get; }
            public BattleBalanceSettings BattleSettings { get; }
            public ExpeditionBalanceSettings ExpeditionSettings { get; }
            public ExpeditionStageProcessor Processor { get; }

            public ExpeditionStageResolution Resolve()
            {
                return Processor.ResolveStageBattle(
                    Guild,
                    Expedition.ExpeditionId,
                    BattleSettings,
                    ExpeditionSettings);
            }

            public void Dispose()
            {
                TestAssetFactory.Destroy(BattleSettings, ExpeditionSettings);
            }
        }
    }
}
