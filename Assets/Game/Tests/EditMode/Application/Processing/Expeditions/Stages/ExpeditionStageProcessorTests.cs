using System;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Tests.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Expeditions.Stages
{
    public sealed class ExpeditionStageProcessorTests
    {
        [Test]
        public void ProcessStage_OnVictory_AddsTemporaryRewardAndContinues()
        {
            TestContext context = CreateContext(maximumStages: 3, enemyPower: 10f);
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                minimumStageFunds: 100,
                maximumStageFunds: 100,
                returnFundsThreshold: 999,
                minimumPartyHpRatioToContinue: 0f,
                captiveRescueChance: 0f);
            var random = new SequenceRandomSource(
                integerValues: new[] { 100 },
                floatValues: new[] { 0.5f, 0.5f });

            try
            {
                ExpeditionStageResult result = CreateProcessor(random).ProcessStage(
                    context.Guild,
                    context.Expedition.ExpeditionId,
                    context.BattleSettings,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.VictoryContinued));
                Assert.That(result.BattleResult.Outcome, Is.EqualTo(BattleOutcome.Victory));
                Assert.That(result.StageReward, Is.EqualTo(100));
                Assert.That(context.Expedition.TemporaryFunds, Is.EqualTo(100));
                Assert.That(context.Expedition.CurrentStage, Is.EqualTo(2));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Active));
                Assert.That(context.Guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void ProcessStage_OnFinalStageVictory_BeginsReturn()
        {
            TestContext context = CreateContext(maximumStages: 1, enemyPower: 10f);
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                minimumStageFunds: 10,
                maximumStageFunds: 10,
                returnFundsThreshold: 999,
                minimumPartyHpRatioToContinue: 0f,
                captiveRescueChance: 0f);
            var random = new SequenceRandomSource(
                integerValues: new[] { 10 },
                floatValues: new[] { 0.5f, 0.5f });

            try
            {
                ExpeditionStageResult result = CreateProcessor(random).ProcessStage(
                    context.Guild,
                    context.Expedition.ExpeditionId,
                    context.BattleSettings,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.VictoryReturning));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
                Assert.That(context.Participant.Status, Is.EqualTo(CharacterStatus.Expedition));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void ProcessStage_WhenFundsReachThreshold_BeginsReturn()
        {
            TestContext context = CreateContext(maximumStages: 3, enemyPower: 10f);
            context.Expedition.AddTemporaryFunds(40);
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                minimumStageFunds: 10,
                maximumStageFunds: 10,
                returnFundsThreshold: 50,
                minimumPartyHpRatioToContinue: 0f,
                captiveRescueChance: 0f);
            var random = new SequenceRandomSource(
                integerValues: new[] { 10 },
                floatValues: new[] { 0.5f, 0.5f });

            try
            {
                ExpeditionStageResult result = CreateProcessor(random).ProcessStage(
                    context.Guild,
                    context.Expedition.ExpeditionId,
                    context.BattleSettings,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.VictoryReturning));
                Assert.That(context.Expedition.TemporaryFunds, Is.EqualTo(50));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void ProcessStage_WhenPartyHpIsLow_BeginsReturn()
        {
            TestContext context = CreateContext(maximumStages: 3, enemyPower: 1f);
            context.Participant.ApplyDamage(70);
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                minimumStageFunds: 10,
                maximumStageFunds: 10,
                returnFundsThreshold: 999,
                minimumPartyHpRatioToContinue: 0.5f,
                captiveRescueChance: 0f);
            var random = new SequenceRandomSource(
                integerValues: new[] { 10 },
                floatValues: new[] { 0.5f, 0.5f });

            try
            {
                ExpeditionStageResult result = CreateProcessor(random).ProcessStage(
                    context.Guild,
                    context.Expedition.ExpeditionId,
                    context.BattleSettings,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.VictoryReturning));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void ProcessStage_OnEscape_RetainsLootAndBeginsReturn()
        {
            TestContext context = CreateContext(maximumStages: 3, enemyPower: 1000f);
            context.Expedition.AddTemporaryFunds(101);
            context.Expedition.TemporaryInventory.Add("potion", 3);
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                escapeChance: 1f,
                escapedLootRetentionRatio: 0.5f);
            var random = new SequenceRandomSource(
                floatValues: new[] { 0.5f, 0.5f, 0f });

            try
            {
                ExpeditionStageResult result = CreateProcessor(random).ProcessStage(
                    context.Guild,
                    context.Expedition.ExpeditionId,
                    context.BattleSettings,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.EscapedReturning));
                Assert.That(context.Expedition.TemporaryFunds, Is.EqualTo(50));
                Assert.That(context.Expedition.TemporaryInventory.GetQuantity("potion"), Is.EqualTo(1));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
                Assert.That(context.Participant.Status, Is.EqualTo(CharacterStatus.Expedition));
                Assert.That(context.Guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void ProcessStage_WhenEscapeFails_CapturesPartyAndDiscardsLoot()
        {
            TestContext context = CreateContext(maximumStages: 3, enemyPower: 1000f);
            context.Expedition.AddTemporaryFunds(100);
            context.Expedition.TemporaryInventory.Add("potion", 2);
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                escapeChance: 0f);
            var random = new SequenceRandomSource(
                floatValues: new[] { 0.5f, 0.5f, 0.5f });

            try
            {
                ExpeditionStageResult result = CreateProcessor(random).ProcessStage(
                    context.Guild,
                    context.Expedition.ExpeditionId,
                    context.BattleSettings,
                    context.ExpeditionSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStageOutcome.Captured));
                Assert.That(context.Expedition.Status, Is.EqualTo(ExpeditionStatus.Captured));
                Assert.That(context.Expedition.TemporaryFunds, Is.Zero);
                Assert.That(context.Expedition.TemporaryInventory.Quantities, Is.Empty);
                Assert.That(context.Participant.Status, Is.EqualTo(CharacterStatus.Captured));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void ProcessStage_OnVictory_RecordsFirstCapturedCharacterById()
        {
            TestContext context = CreateContext(maximumStages: 1, enemyPower: 1f);
            CharacterRuntimeData captiveB = CreateCharacter("captive-b");
            CharacterRuntimeData captiveA = CreateCharacter("captive-a");
            captiveB.SetStatus(CharacterStatus.Captured);
            captiveA.SetStatus(CharacterStatus.Captured);
            context = context.WithAdditionalMembers(captiveB, captiveA);
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings(
                minimumStageFunds: 10,
                maximumStageFunds: 10,
                captiveRescueChance: 1f);
            var random = new SequenceRandomSource(
                integerValues: new[] { 10 },
                floatValues: new[] { 0.5f, 0.5f, 0f });

            try
            {
                ExpeditionStageResult result = CreateProcessor(random).ProcessStage(
                    context.Guild,
                    context.Expedition.ExpeditionId,
                    context.BattleSettings,
                    context.ExpeditionSettings);

                Assert.That(result.RescuedCharacterId, Is.EqualTo("captive-a"));
                Assert.That(context.Expedition.RescuedCharacterIds, Is.EqualTo(new[] { "captive-a" }));
                Assert.That(captiveA.Status, Is.EqualTo(CharacterStatus.Captured));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void ProcessStage_WhenExpeditionIsNotActive_Throws()
        {
            TestContext context = CreateContext(maximumStages: 1, enemyPower: 1f);
            context.Expedition.BeginReturn();
            context.ExpeditionSettings = TestAssetFactory.CreateExpeditionBalanceSettings();

            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => CreateProcessor(new SequenceRandomSource()).ProcessStage(
                        context.Guild,
                        context.Expedition.ExpeditionId,
                        context.BattleSettings,
                        context.ExpeditionSettings));
            }
            finally
            {
                context.Destroy();
            }
        }

        private static ExpeditionStageProcessor CreateProcessor(SequenceRandomSource random)
        {
            return new ExpeditionStageProcessor(random);
        }

        private static TestContext CreateContext(int maximumStages, float enemyPower)
        {
            CharacterRuntimeData participant = CreateCharacter("participant", attack: 100);
            participant.SetStatus(CharacterStatus.Expedition);
            var expedition = new ExpeditionRuntimeData(
                "expedition-1",
                "forest",
                new[] { participant.CharacterId },
                enemyPower,
                maximumStages,
                1f,
                true);
            var guild = new GuildRuntimeData(
                "Guild",
                100,
                new[] { participant },
                participant.CharacterId);
            guild.AddExpedition(expedition);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f);
            return new TestContext(guild, expedition, participant, battleSettings);
        }

        private static CharacterRuntimeData CreateCharacter(string id, int attack = 10)
        {
            return new CharacterRuntimeData(id, 1, 100, attack, 10, 10, 10, 30);
        }

        private sealed class TestContext
        {
            public TestContext(
                GuildRuntimeData guild,
                ExpeditionRuntimeData expedition,
                CharacterRuntimeData participant,
                BattleBalanceSettings battleSettings)
            {
                Guild = guild;
                Expedition = expedition;
                Participant = participant;
                BattleSettings = battleSettings;
            }

            public GuildRuntimeData Guild { get; private set; }
            public ExpeditionRuntimeData Expedition { get; }
            public CharacterRuntimeData Participant { get; }
            public BattleBalanceSettings BattleSettings { get; }
            public ExpeditionBalanceSettings ExpeditionSettings { get; set; }

            public TestContext WithAdditionalMembers(params CharacterRuntimeData[] members)
            {
                var allMembers = new CharacterRuntimeData[members.Length + 1];
                allMembers[0] = Participant;
                Array.Copy(members, 0, allMembers, 1, members.Length);
                var replacement = new GuildRuntimeData(
                    "Guild",
                    Guild.Funds,
                    allMembers,
                    Participant.CharacterId);
                replacement.AddExpedition(Expedition);
                Guild = replacement;
                return this;
            }

            public void Destroy()
            {
                TestAssetFactory.Destroy(BattleSettings, ExpeditionSettings);
            }
        }
    }
}
