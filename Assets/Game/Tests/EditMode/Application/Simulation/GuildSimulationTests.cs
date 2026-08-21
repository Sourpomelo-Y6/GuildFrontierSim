using System.Linq;
using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Application.Simulation;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Tests.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Simulation
{
    public sealed class GuildSimulationTests
    {
        [Test]
        public void AdvanceTurn_AutomaticallyStartsAdvancesAndReturnsExpedition()
        {
            GuildRuntimeData guild = CreateGuild(
                CreateCharacter("leader", attack: 50),
                CreateCharacter("member", attack: 40));
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea(
                "forest",
                enemyPower: 1,
                maximumStages: 1,
                rewardMultiplier: 1f,
                canContainCaptives: false);
            CpuSelectionSettings selection = TestAssetFactory.CreateCpuSelectionSettings(
                desiredExpeditionMembers: 1,
                minimumGuildMembersRemaining: 1);
            BattleBalanceSettings battle = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                salaryIntervalTurns: 100);
            ExpeditionBalanceSettings expedition =
                TestAssetFactory.CreateExpeditionBalanceSettings(
                    minimumStageFunds: 50,
                    maximumStageFunds: 50);
            GuildSimulationSettings simulationSettings =
                TestAssetFactory.CreateGuildSimulationSettings(
                    defenseIntervalTurns: 100,
                    automaticallyStartExpeditions: true,
                    expeditionIntervalTurns: 1);
            var random = new SequenceRandomSource(
                integerValues: new[] { 50 },
                floatValues: new[] { 0.5f, 0.5f });
            var simulation = new GuildSimulation(
                guild,
                battle,
                selection,
                expedition,
                simulationSettings,
                area,
                random);

            try
            {
                SimulationAdvanceResult first = simulation.AdvanceTurn();
                SimulationAdvanceResult second = simulation.AdvanceTurn();
                SimulationAdvanceResult third = simulation.AdvanceTurn();

                Assert.That(
                    first.TurnResult.ExpeditionStartResult.Outcome,
                    Is.EqualTo(ExpeditionStartOutcome.Started));
                Assert.That(
                    second.TurnResult.ExpeditionStageResults.Single().Outcome,
                    Is.EqualTo(ExpeditionStageOutcome.VictoryReturning));
                Assert.That(
                    third.TurnResult.ExpeditionReturnResults.Single().TransferredFunds,
                    Is.EqualTo(50));
                Assert.That(third.TurnResult.ExpeditionStartResult, Is.Null);
                Assert.That(guild.Funds, Is.EqualTo(150));
                Assert.That(guild.Expeditions, Is.Empty);
                Assert.That(simulation.LogHistory, Has.Count.GreaterThanOrEqualTo(6));
                Assert.That(
                    simulation.LogHistory.Any(
                        entry => entry.Category == SimulationLogCategory.Expedition &&
                                 entry.Message.Contains("帰還")),
                    Is.True);
            }
            finally
            {
                TestAssetFactory.Destroy(
                    area,
                    selection,
                    battle,
                    expedition,
                    simulationSettings);
            }
        }

        [Test]
        public void AdvanceTurn_WithSameStateAndRandomSequence_IsReproducible()
        {
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings selection = TestAssetFactory.CreateCpuSelectionSettings(
                desiredDefenseMembers: 1);
            BattleBalanceSettings battle = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                victoryInjuryChance: 0f,
                minimumDefenseReward: 10,
                maximumDefenseReward: 10,
                salaryIntervalTurns: 2,
                victoryLoyaltyChange: 2);
            ExpeditionBalanceSettings expedition =
                TestAssetFactory.CreateExpeditionBalanceSettings();
            GuildSimulationSettings simulationSettings =
                TestAssetFactory.CreateGuildSimulationSettings(
                    defenseIntervalTurns: 1,
                    defenseEnemyBasePower: 1f,
                    automaticallyStartExpeditions: false);
            GuildSimulation first = CreateDefenseSimulation(
                area, selection, battle, expedition, simulationSettings);
            GuildSimulation second = CreateDefenseSimulation(
                area, selection, battle, expedition, simulationSettings);

            try
            {
                SimulationAdvanceResult firstTurnA = first.AdvanceTurn();
                SimulationAdvanceResult firstTurnB = second.AdvanceTurn();
                SimulationAdvanceResult secondTurnA = first.AdvanceTurn();
                SimulationAdvanceResult secondTurnB = second.AdvanceTurn();

                Assert.That(
                    firstTurnA.TurnResult.DefenseResult.Outcome,
                    Is.EqualTo(DefenseOutcome.Victory));
                Assert.That(
                    firstTurnA.Logs.Select(log => log.Message),
                    Is.EqualTo(firstTurnB.Logs.Select(log => log.Message)));
                Assert.That(
                    secondTurnA.Logs.Select(log => log.Message),
                    Is.EqualTo(secondTurnB.Logs.Select(log => log.Message)));
                Assert.That(first.Guild.Funds, Is.EqualTo(second.Guild.Funds));
                Assert.That(
                    first.Guild.Characters[0].Loyalty,
                    Is.EqualTo(second.Guild.Characters[0].Loyalty));
                Assert.That(first.Guild.Funds, Is.EqualTo(110));
                Assert.That(first.Guild.Characters[0].Loyalty, Is.EqualTo(34));
                Assert.That(
                    first.LogHistory.Any(
                        entry => entry.Category == SimulationLogCategory.Loyalty),
                    Is.True);
            }
            finally
            {
                TestAssetFactory.Destroy(
                    area,
                    selection,
                    battle,
                    expedition,
                    simulationSettings);
            }
        }

        private static GuildSimulation CreateDefenseSimulation(
            ExpeditionAreaDefinition area,
            CpuSelectionSettings selection,
            BattleBalanceSettings battle,
            ExpeditionBalanceSettings expedition,
            GuildSimulationSettings simulationSettings)
        {
            GuildRuntimeData guild = CreateGuild(CreateCharacter("leader", attack: 50));
            var random = new SequenceRandomSource(
                integerValues: new[] { 10, 10 },
                floatValues: new[]
                {
                    0.5f, 0.5f, 0.9f,
                    0.5f, 0.5f, 0.9f
                });
            return new GuildSimulation(
                guild,
                battle,
                selection,
                expedition,
                simulationSettings,
                area,
                random);
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
