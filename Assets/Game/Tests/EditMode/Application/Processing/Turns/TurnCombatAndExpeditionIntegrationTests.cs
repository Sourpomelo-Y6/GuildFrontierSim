using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Application.Processing.Turns;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Tests.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Turns
{
    public sealed class TurnCombatAndExpeditionIntegrationTests
    {
        [Test]
        public void Process_DefendsThenStartsExpeditionWithoutReusingDefender()
        {
            CharacterRuntimeData strongest = CreateCharacter("strongest", attack: 50);
            CharacterRuntimeData second = CreateCharacter("second", attack: 40);
            CharacterRuntimeData reserve = CreateCharacter("reserve", attack: 10);
            GuildRuntimeData guild = CreateGuild(strongest, second, reserve);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredDefenseMembers: 1,
                    desiredExpeditionMembers: 1,
                    minimumGuildMembersRemaining: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                victoryInjuryChance: 0f,
                minimumDefenseReward: 100,
                maximumDefenseReward: 100,
                salaryIntervalTurns: 100);
            ExpeditionBalanceSettings expeditionSettings =
                TestAssetFactory.CreateExpeditionBalanceSettings();
            var random = new SequenceRandomSource(
                integerValues: new[] { 100 },
                floatValues: new[] { 0.5f, 0.5f, 0.9f });
            var processor = new TurnProcessor(randomSource: random);

            try
            {
                TurnResult result = processor.Process(
                    guild,
                    new TurnRequest(
                        new DefenseBattleRequest(1f),
                        new ExpeditionStartRequest("expedition-1", area)),
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);

                Assert.That(result.DefenseResult.Outcome, Is.EqualTo(DefenseOutcome.Victory));
                Assert.That(result.DefenseResult.DefenderIds, Is.EqualTo(new[] { "strongest" }));
                Assert.That(result.DefenseLoyaltyResult.LoyaltyChange, Is.EqualTo(2));
                Assert.That(
                    result.ExpeditionStartResult.Outcome,
                    Is.EqualTo(ExpeditionStartOutcome.Started));
                Assert.That(
                    result.ExpeditionStartResult.ParticipantIds,
                    Is.EqualTo(new[] { "second" }));
                Assert.That(strongest.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(second.Status, Is.EqualTo(CharacterStatus.Expedition));
                Assert.That(strongest.Loyalty, Is.EqualTo(32));
            }
            finally
            {
                TestAssetFactory.Destroy(
                    area,
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);
            }
        }

        [Test]
        public void Process_AdvancesActiveExpeditionThenCompletesReturnNextTurn()
        {
            CharacterRuntimeData participant = CreateCharacter("participant", attack: 50);
            participant.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = CreateGuild(participant);
            var expedition = new ExpeditionRuntimeData(
                "expedition-1",
                "forest",
                new[] { participant.CharacterId },
                1f,
                1,
                1f,
                false);
            guild.AddExpedition(expedition);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings();
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                salaryIntervalTurns: 100,
                victoryLoyaltyChange: 3);
            ExpeditionBalanceSettings expeditionSettings =
                TestAssetFactory.CreateExpeditionBalanceSettings(
                    minimumStageFunds: 50,
                    maximumStageFunds: 50);
            var random = new SequenceRandomSource(
                integerValues: new[] { 50 },
                floatValues: new[] { 0.5f, 0.5f });
            var processor = new TurnProcessor(randomSource: random);

            try
            {
                TurnResult first = processor.Process(
                    guild,
                    new TurnRequest(),
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);

                Assert.That(first.ExpeditionStageResults, Has.Count.EqualTo(1));
                Assert.That(
                    first.ExpeditionStageResults[0].Outcome,
                    Is.EqualTo(ExpeditionStageOutcome.VictoryReturning));
                Assert.That(first.ExpeditionLoyaltyResults, Has.Count.EqualTo(1));
                Assert.That(participant.Loyalty, Is.EqualTo(33));
                Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
                Assert.That(guild.Funds, Is.EqualTo(100));

                TurnResult second = processor.Process(
                    guild,
                    new TurnRequest(),
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);

                Assert.That(second.ExpeditionReturnResults, Has.Count.EqualTo(1));
                Assert.That(second.ExpeditionReturnResults[0].TransferredFunds, Is.EqualTo(50));
                Assert.That(second.ExpeditionStageResults, Is.Empty);
                Assert.That(guild.Funds, Is.EqualTo(150));
                Assert.That(guild.Expeditions, Is.Empty);
                Assert.That(participant.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);
            }
        }

        [Test]
        public void Process_WhenNoDefenderExists_DoesNotApplyBattleLoyalty()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            leader.SetStatus(CharacterStatus.Hospitalized, 2);
            GuildRuntimeData guild = CreateGuild(leader);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings =
                TestAssetFactory.CreateBattleSettings(salaryIntervalTurns: 100);
            ExpeditionBalanceSettings expeditionSettings =
                TestAssetFactory.CreateExpeditionBalanceSettings();
            var processor = new TurnProcessor(
                randomSource: new SequenceRandomSource());

            try
            {
                TurnResult result = processor.Process(
                    guild,
                    new TurnRequest(new DefenseBattleRequest(10f)),
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);

                Assert.That(result.DefenseResult.Outcome, Is.EqualTo(DefenseOutcome.NoDefenders));
                Assert.That(result.DefenseLoyaltyResult, Is.Null);
                Assert.That(leader.Loyalty, Is.EqualTo(30));
            }
            finally
            {
                TestAssetFactory.Destroy(
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);
            }
        }

        [Test]
        public void Process_WhenRandomSourceIsMissing_RejectsBeforeAdvancingTurn()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            GuildRuntimeData guild = CreateGuild(leader);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings();
            BattleBalanceSettings battleSettings =
                TestAssetFactory.CreateBattleSettings();
            ExpeditionBalanceSettings expeditionSettings =
                TestAssetFactory.CreateExpeditionBalanceSettings();

            try
            {
                Assert.Throws<System.InvalidOperationException>(
                    () => new TurnProcessor().Process(
                        guild,
                        new TurnRequest(new DefenseBattleRequest(10f)),
                        selectionSettings,
                        battleSettings,
                        expeditionSettings));
                Assert.That(guild.CurrentTurn, Is.Zero);
            }
            finally
            {
                TestAssetFactory.Destroy(
                    selectionSettings,
                    battleSettings,
                    expeditionSettings);
            }
        }

        private static GuildRuntimeData CreateGuild(
            params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData(
                "Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(
            string id,
            int attack = 10)
        {
            return new CharacterRuntimeData(id, 1, 100, attack, 10, 10, 10, 30);
        }
    }
}
