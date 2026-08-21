using System;
using System.Collections.Generic;
using System.Linq;
using GuildFrontierSim.Application.Simulation;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Simulation
{
    public sealed class GuildSimulationLongRunTests
    {
        [Test]
        public void AdvanceTurn_ForFiftyTurns_PreservesRuntimeInvariants()
        {
            GuildRuntimeData guild = CreateGuild();
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea(
                "forest",
                enemyPower: 80,
                maximumStages: 3);
            CpuSelectionSettings cpu = TestAssetFactory.CreateCpuSelectionSettings(
                desiredDefenseMembers: 2,
                desiredExpeditionMembers: 3,
                minimumGuildMembersRemaining: 2);
            BattleBalanceSettings battle = TestAssetFactory.CreateBattleSettings(
                salaryIntervalTurns: 5);
            ExpeditionBalanceSettings expedition =
                TestAssetFactory.CreateExpeditionBalanceSettings();
            GuildSimulationSettings settings =
                TestAssetFactory.CreateGuildSimulationSettings(
                    defenseIntervalTurns: 2,
                    defenseEnemyBasePower: 90f,
                    automaticallyStartExpeditions: true,
                    expeditionIntervalTurns: 1);
            var simulation = new GuildSimulation(
                guild,
                battle,
                cpu,
                expedition,
                settings,
                area,
                new SystemRandomSource(20260822));

            try
            {
                for (int turn = 1; turn <= 50; turn++)
                {
                    SimulationAdvanceResult result = simulation.AdvanceTurn();
                    Assert.That(result.TurnResult.TurnNumber, Is.EqualTo(turn));
                    AssertInvariants(guild);
                }

                Assert.That(guild.CurrentTurn, Is.EqualTo(50));
                Assert.That(simulation.LogHistory, Is.Not.Empty);
            }
            finally
            {
                TestAssetFactory.Destroy(area, cpu, battle, expedition, settings);
            }
        }

        private static void AssertInvariants(GuildRuntimeData guild)
        {
            Assert.That(guild.Funds, Is.GreaterThanOrEqualTo(0));
            Assert.That(guild.Characters, Is.Not.Empty);
            Assert.That(guild.TryGetCharacter(guild.LeaderCharacterId, out _), Is.True);
            Assert.That(
                guild.Characters.Select(character => character.CharacterId).Distinct().Count(),
                Is.EqualTo(guild.Characters.Count));

            var expeditionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < guild.Expeditions.Count; index++)
            {
                ExpeditionRuntimeData expedition = guild.Expeditions[index];
                Assert.That(expeditionIds.Add(expedition.ExpeditionId), Is.True);
                Assert.That(expedition.TemporaryFunds, Is.GreaterThanOrEqualTo(0));
                for (int participantIndex = 0;
                     participantIndex < expedition.ParticipantIds.Count;
                     participantIndex++)
                {
                    string participantId = expedition.ParticipantIds[participantIndex];
                    Assert.That(guild.TryGetCharacter(participantId, out _), Is.True);
                }
            }

            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                Assert.That(character.CurrentHp, Is.InRange(0, character.MaxHp));
                Assert.That(character.Loyalty, Is.InRange(-100, 100));
                Assert.That(character.UnavailableTurnsRemaining, Is.GreaterThanOrEqualTo(0));
            }
        }

        private static GuildRuntimeData CreateGuild()
        {
            var characters = new List<CharacterRuntimeData>();
            for (int index = 0; index < 8; index++)
            {
                characters.Add(new CharacterRuntimeData(
                    $"member-{index}",
                    1 + index % 3,
                    100 + index * 5,
                    12 + index,
                    10 + index,
                    8 + index,
                    10 + index,
                    30 + index));
            }

            return new GuildRuntimeData(
                "Long Run Guild",
                2000,
                characters,
                characters[0].CharacterId);
        }
    }
}
