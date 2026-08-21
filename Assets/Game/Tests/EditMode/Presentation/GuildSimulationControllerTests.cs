using System.Text.RegularExpressions;
using GuildFrontierSim.Application.Simulation;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Presets;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuildFrontierSim.Tests.Presentation
{
    public sealed class GuildSimulationControllerTests
    {
        [Test]
        public void TryInitialize_WithCompleteConfiguration_CreatesSimulation()
        {
            TestContext context = CreateContext();

            try
            {
                Assert.That(context.Controller.TryInitialize(), Is.True);
                Assert.That(context.Controller.IsInitialized, Is.True);
                Assert.That(context.Controller.Simulation, Is.Not.Null);
                Assert.That(context.Controller.Guild.GuildName, Is.EqualTo("Test Guild"));
                Assert.That(context.Controller.Guild.Characters, Has.Count.EqualTo(2));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void AdvanceTurnAndGetResult_AdvancesAndPublishesResult()
        {
            TestContext context = CreateContext();
            SimulationAdvanceResult publishedResult = null;

            try
            {
                context.Controller.SimulationAdvanced += result => publishedResult = result;
                LogAssert.Expect(
                    LogType.Log,
                    new Regex(@"\[Turn 1\]\[Turn\]"));

                SimulationAdvanceResult result =
                    context.Controller.AdvanceTurnAndGetResult();

                Assert.That(result, Is.Not.Null);
                Assert.That(result.TurnResult.TurnNumber, Is.EqualTo(1));
                Assert.That(context.Controller.LastAdvanceResult, Is.SameAs(result));
                Assert.That(publishedResult, Is.SameAs(result));
                Assert.That(context.Controller.Guild.CurrentTurn, Is.EqualTo(1));
            }
            finally
            {
                context.Destroy();
            }
        }

        [Test]
        public void TryInitialize_WhenPresetIsMissing_LogsClearError()
        {
            var gameObject = new GameObject("Controller Test");
            GuildSimulationController controller =
                gameObject.AddComponent<GuildSimulationController>();

            try
            {
                LogAssert.Expect(
                    LogType.Error,
                    "GuildSimulationController requires a GuildStartingPreset.");

                Assert.That(controller.TryInitialize(), Is.False);
                Assert.That(controller.IsInitialized, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static TestContext CreateContext()
        {
            CharacterDefinition leader = TestAssetFactory.CreateCharacter("leader");
            CharacterDefinition member = TestAssetFactory.CreateCharacter("member");
            GuildStartingPreset preset = TestAssetFactory.CreateGuildPreset(
                "Test Guild",
                100,
                "leader",
                leader,
                member);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            BattleBalanceSettings battle = TestAssetFactory.CreateBattleSettings(
                salaryIntervalTurns: 100);
            CpuSelectionSettings cpu = TestAssetFactory.CreateCpuSelectionSettings();
            ExpeditionBalanceSettings expedition =
                TestAssetFactory.CreateExpeditionBalanceSettings();
            GuildSimulationSettings simulation =
                TestAssetFactory.CreateGuildSimulationSettings(
                    defenseIntervalTurns: 100,
                    automaticallyStartExpeditions: false);
            var gameObject = new GameObject("Controller Test");
            GuildSimulationController controller =
                gameObject.AddComponent<GuildSimulationController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("guildStartingPreset").objectReferenceValue = preset;
            serialized.FindProperty("expeditionArea").objectReferenceValue = area;
            serialized.FindProperty("battleSettings").objectReferenceValue = battle;
            serialized.FindProperty("cpuSelectionSettings").objectReferenceValue = cpu;
            serialized.FindProperty("expeditionSettings").objectReferenceValue = expedition;
            serialized.FindProperty("simulationSettings").objectReferenceValue = simulation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return new TestContext(
                controller,
                gameObject,
                leader,
                member,
                preset,
                area,
                battle,
                cpu,
                expedition,
                simulation);
        }

        private sealed class TestContext
        {
            private readonly Object[] assets;

            public TestContext(
                GuildSimulationController controller,
                GameObject gameObject,
                params Object[] assets)
            {
                Controller = controller;
                GameObject = gameObject;
                this.assets = assets;
            }

            public GuildSimulationController Controller { get; }
            public GameObject GameObject { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(GameObject);
                TestAssetFactory.Destroy(assets);
            }
        }
    }
}
