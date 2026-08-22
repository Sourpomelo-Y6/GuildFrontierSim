using System;
using System.Linq;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Tests.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Defense
{
    public sealed class DefenseBattleProcessorTests
    {
        [Test]
        public void Process_WhenVictorious_AddsFullRewardAndRestoresDefenders()
        {
            CharacterRuntimeData first = CreateCharacter("first", attack: 50);
            CharacterRuntimeData second = CreateCharacter("second", attack: 40);
            GuildRuntimeData guild = CreateGuild(first, second);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 2);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                victoryInjuryChance: 0.1f,
                minimumDefenseReward: 100,
                maximumDefenseReward: 100);
            var random = new SequenceRandomSource(
                integerValues: new[] { 100 },
                floatValues: new[] { 0.5f, 0.5f, 0.9f, 0.9f });

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(random).Process(
                    guild,
                    new DefenseBattleRequest(10f),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.Victory));
                Assert.That(result.BattleResult.Outcome, Is.EqualTo(BattleOutcome.Victory));
                Assert.That(result.Reward, Is.EqualTo(100));
                Assert.That(guild.Funds, Is.EqualTo(200));
                Assert.That(result.DefenderIds, Is.EqualTo(new[] { "first", "second" }));
                Assert.That(result.InjuredCharacterIds, Is.Empty);
                Assert.That(result.HospitalizedCharacterIds, Is.Empty);
                Assert.That(first.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(second.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WhenDefeated_AddsReducedRewardAndHospitalizesDefender()
        {
            CharacterRuntimeData defender = CreateCharacter("defender");
            GuildRuntimeData guild = CreateGuild(defender);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                injuryChance: 1f,
                hospitalizationChance: 1f,
                minimumRecoveryTurns: 3,
                maximumRecoveryTurns: 3,
                minimumDefenseReward: 101,
                maximumDefenseReward: 101,
                defeatDefenseRewardMultiplier: 0.5f);
            var random = new SequenceRandomSource(
                integerValues: new[] { 101, 3 },
                floatValues: new[] { 0.5f, 0.5f, 0.1f, 0.1f });

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(random).Process(
                    guild,
                    new DefenseBattleRequest(1000f),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.RepelledWithLoss));
                Assert.That(result.Reward, Is.EqualTo(51));
                Assert.That(guild.Funds, Is.EqualTo(151));
                Assert.That(result.HospitalizedCharacterIds, Is.EqualTo(new[] { "defender" }));
                Assert.That(result.InjuredCharacterIds, Is.Empty);
                Assert.That(defender.Status, Is.EqualTo(CharacterStatus.Hospitalized));
                Assert.That(defender.UnavailableTurnsRemaining, Is.EqualTo(3));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WhenInjuryDoesNotRequireHospital_SetsInjuredState()
        {
            CharacterRuntimeData defender = CreateCharacter("defender");
            GuildRuntimeData guild = CreateGuild(defender);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                injuryChance: 1f,
                hospitalizationChance: 0f,
                minimumRecoveryTurns: 2,
                maximumRecoveryTurns: 2,
                minimumDefenseReward: 10,
                maximumDefenseReward: 10);
            var random = new SequenceRandomSource(
                integerValues: new[] { 10, 2 },
                floatValues: new[] { 0.5f, 0.5f, 0.1f, 0.9f });

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(random).Process(
                    guild,
                    new DefenseBattleRequest(1000f),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.InjuredCharacterIds, Is.EqualTo(new[] { "defender" }));
                Assert.That(result.HospitalizedCharacterIds, Is.Empty);
                Assert.That(defender.Status, Is.EqualTo(CharacterStatus.Injured));
                Assert.That(defender.UnavailableTurnsRemaining, Is.EqualTo(2));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WhenBattleDraws_MapsToRepelledWithLoss()
        {
            CharacterRuntimeData defender = CreateCharacter("defender");
            GuildRuntimeData guild = CreateGuild(defender);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                injuryChance: 0f,
                minimumDefenseReward: 100,
                maximumDefenseReward: 100);
            var random = new SequenceRandomSource(
                integerValues: new[] { 100 },
                floatValues: new[] { 0.5f, 0.5f, 0.9f });

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(random).Process(
                    guild,
                    new DefenseBattleRequest(32.5f),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.BattleResult.Outcome, Is.EqualTo(BattleOutcome.Draw));
                Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.RepelledWithLoss));
                Assert.That(result.Reward, Is.EqualTo(50));
                Assert.That(defender.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WhenNoDefenderIsAvailable_ReturnsWithoutBattle()
        {
            CharacterRuntimeData captured = CreateCharacter("captured");
            captured.SetStatus(CharacterStatus.Captured);
            GuildRuntimeData guild = CreateGuild(captured);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();
            var random = new SequenceRandomSource();

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(random).Process(
                    guild,
                    new DefenseBattleRequest(100f),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.NoDefenders));
                Assert.That(result.BattleResult, Is.Null);
                Assert.That(result.Reward, Is.Zero);
                Assert.That(result.DefenderIds, Is.Empty);
                Assert.That(guild.Funds, Is.EqualTo(100));
                Assert.That(captured.Status, Is.EqualTo(CharacterStatus.Captured));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WhenResolutionFails_RestoresDefendingStatus()
        {
            CharacterRuntimeData defender = CreateCharacter("defender");
            GuildRuntimeData guild = CreateGuild(defender);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();
            var random = new SequenceRandomSource(floatValues: new[] { 1.1f });

            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => new DefenseBattleProcessor(random).Process(
                        guild,
                        new DefenseBattleRequest(100f),
                        selectionSettings,
                        battleSettings));

                Assert.That(defender.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_UsesCpuSelectionAndExcludesRequestedMember()
        {
            CharacterRuntimeData strongest = CreateCharacter("strongest", attack: 100);
            CharacterRuntimeData next = CreateCharacter("next", attack: 50);
            GuildRuntimeData guild = CreateGuild(strongest, next);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                victoryInjuryChance: 0f,
                minimumDefenseReward: 10,
                maximumDefenseReward: 10);
            var random = new SequenceRandomSource(
                integerValues: new[] { 10 },
                floatValues: new[] { 0.5f, 0.5f, 0.9f });

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(random).Process(
                    guild,
                    new DefenseBattleRequest(1f, new[] { strongest.CharacterId }),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.DefenderIds.Single(), Is.EqualTo("next"));
                Assert.That(strongest.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WithExplicitAssignment_UsesOnlyRequestedDefender()
        {
            CharacterRuntimeData strongest = CreateCharacter("strongest", attack: 100);
            CharacterRuntimeData requested = CreateCharacter("requested", attack: 10);
            GuildRuntimeData guild = CreateGuild(strongest, requested);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f,
                victoryInjuryChance: 0f,
                minimumDefenseReward: 10,
                maximumDefenseReward: 10);
            var random = new SequenceRandomSource(
                integerValues: new[] { 10 },
                floatValues: new[] { 0.5f, 0.5f, 0.9f });

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(random).Process(
                    guild,
                    new DefenseAssignment(1f, new[] { requested.CharacterId }),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.DefenderIds, Is.EqualTo(new[] { "requested" }));
                Assert.That(strongest.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WithEmptyExplicitAssignment_ReturnsNoDefenders()
        {
            CharacterRuntimeData character = CreateCharacter("member");
            GuildRuntimeData guild = CreateGuild(character);
            CpuSelectionSettings selectionSettings = TestAssetFactory.CreateCpuSelectionSettings();
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                DefenseBattleResult result = new DefenseBattleProcessor(
                    new SequenceRandomSource()).Process(
                    guild,
                    new DefenseAssignment(100f, Array.Empty<string>()),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.NoDefenders));
                Assert.That(guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Process_WithInvalidExplicitAssignment_DoesNotChangeGuildState()
        {
            CharacterRuntimeData character = CreateCharacter("member");
            GuildRuntimeData guild = CreateGuild(character);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 2);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    new DefenseBattleProcessor(new SequenceRandomSource()).Process(
                        guild,
                        new DefenseAssignment(100f, new[] { "member", "member" }),
                        selectionSettings,
                        battleSettings));

                Assert.That(character.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        private static GuildRuntimeData CreateGuild(params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData("Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(string id, int attack = 10)
        {
            return new CharacterRuntimeData(id, 1, 100, attack, 10, 10, 10, 30);
        }
    }
}
