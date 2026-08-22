using System;
using System.Linq;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Expeditions
{
    public sealed class ExpeditionProcessorStartTests
    {
        [Test]
        public void Start_SelectsMembersRegistersExpeditionAndSnapshotsArea()
        {
            CharacterRuntimeData strongest = CreateCharacter("strongest", 50);
            CharacterRuntimeData second = CreateCharacter("second", 40);
            CharacterRuntimeData third = CreateCharacter("third", 30);
            CharacterRuntimeData reserveOne = CreateCharacter("reserve-1", 20);
            CharacterRuntimeData reserveTwo = CreateCharacter("reserve-2", 10);
            GuildRuntimeData guild = CreateGuild(
                strongest,
                second,
                third,
                reserveOne,
                reserveTwo);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea(
                "forest",
                enemyPower: 120,
                maximumStages: 4,
                rewardMultiplier: 1.5f,
                canContainCaptives: true);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredExpeditionMembers: 3,
                    minimumGuildMembersRemaining: 2);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                ExpeditionStartResult result = new ExpeditionProcessor().Start(
                    guild,
                    new ExpeditionStartRequest("expedition-1", area),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStartOutcome.Started));
                Assert.That(
                    result.ParticipantIds,
                    Is.EqualTo(new[] { "strongest", "second", "third" }));
                Assert.That(result.UnfilledMemberCount, Is.Zero);
                Assert.That(guild.Expeditions.Single(), Is.SameAs(result.Expedition));
                Assert.That(result.Expedition.AreaId, Is.EqualTo("forest"));
                Assert.That(result.Expedition.EnemyBasePower, Is.EqualTo(120f));
                Assert.That(result.Expedition.MaximumStages, Is.EqualTo(4));
                Assert.That(result.Expedition.RewardMultiplier, Is.EqualTo(1.5f));
                Assert.That(result.Expedition.CanContainCaptives, Is.True);
                Assert.That(strongest.Status, Is.EqualTo(CharacterStatus.Expedition));
                Assert.That(second.Status, Is.EqualTo(CharacterStatus.Expedition));
                Assert.That(third.Status, Is.EqualTo(CharacterStatus.Expedition));
                Assert.That(reserveOne.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(reserveTwo.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(area, selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Start_WhenReservePreventsSelection_ReturnsNoMembers()
        {
            CharacterRuntimeData onlyMember = CreateCharacter("only-member");
            GuildRuntimeData guild = CreateGuild(onlyMember);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredExpeditionMembers: 1,
                    minimumGuildMembersRemaining: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                ExpeditionStartResult result = new ExpeditionProcessor().Start(
                    guild,
                    new ExpeditionStartRequest("expedition-1", area),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.Outcome, Is.EqualTo(ExpeditionStartOutcome.NoMembersAvailable));
                Assert.That(result.Expedition, Is.Null);
                Assert.That(result.ParticipantIds, Is.Empty);
                Assert.That(guild.Expeditions, Is.Empty);
                Assert.That(onlyMember.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(area, selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Start_RespectsExcludedMemberIds()
        {
            CharacterRuntimeData strongest = CreateCharacter("strongest", 50);
            CharacterRuntimeData next = CreateCharacter("next", 30);
            GuildRuntimeData guild = CreateGuild(strongest, next);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredExpeditionMembers: 1,
                    minimumGuildMembersRemaining: 0);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                ExpeditionStartResult result = new ExpeditionProcessor().Start(
                    guild,
                    new ExpeditionStartRequest(
                        "expedition-1",
                        area,
                        new[] { strongest.CharacterId }),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.ParticipantIds.Single(), Is.EqualTo("next"));
                Assert.That(strongest.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(area, selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Start_WhenExpeditionIdIsDuplicated_RestoresNewlySelectedMember()
        {
            CharacterRuntimeData first = CreateCharacter("first", 50);
            CharacterRuntimeData second = CreateCharacter("second", 40);
            GuildRuntimeData guild = CreateGuild(first, second);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredExpeditionMembers: 1,
                    minimumGuildMembersRemaining: 0);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();
            var processor = new ExpeditionProcessor();

            try
            {
                processor.Start(
                    guild,
                    new ExpeditionStartRequest("same-id", area),
                    selectionSettings,
                    battleSettings);

                Assert.Throws<ArgumentException>(
                    () => processor.Start(
                        guild,
                        new ExpeditionStartRequest("same-id", area),
                        selectionSettings,
                        battleSettings));

                Assert.That(first.Status, Is.EqualTo(CharacterStatus.Expedition));
                Assert.That(second.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(guild.Expeditions, Has.Count.EqualTo(1));
            }
            finally
            {
                TestAssetFactory.Destroy(area, selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Start_WithExplicitAssignment_UsesOnlyRequestedMembersAndArea()
        {
            CharacterRuntimeData strongest = CreateCharacter("strongest", 100);
            CharacterRuntimeData requested = CreateCharacter("requested", 10);
            CharacterRuntimeData reserve = CreateCharacter("reserve", 5);
            GuildRuntimeData guild = CreateGuild(strongest, requested, reserve);
            ExpeditionAreaDefinition forest = TestAssetFactory.CreateExpeditionArea("forest");
            ExpeditionAreaDefinition ruins = TestAssetFactory.CreateExpeditionArea(
                "ruins", enemyPower: 250, maximumStages: 5);
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings(
                desiredExpeditionMembers: 1,
                minimumGuildMembersRemaining: 1);

            try
            {
                ExpeditionStartResult result = new ExpeditionProcessor().Start(
                    guild,
                    new ExpeditionAssignment("manual-1", "ruins", new[] { "requested" }),
                    new ExpeditionAreaRegistry(new[] { forest, ruins }),
                    settings);

                Assert.That(result.ParticipantIds, Is.EqualTo(new[] { "requested" }));
                Assert.That(result.Expedition.AreaId, Is.EqualTo("ruins"));
                Assert.That(result.Expedition.EnemyBasePower, Is.EqualTo(250f));
                Assert.That(strongest.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(requested.Status, Is.EqualTo(CharacterStatus.Expedition));
            }
            finally
            {
                TestAssetFactory.Destroy(forest, ruins, settings);
            }
        }

        [Test]
        public void Start_WithDefenseOverlap_RejectsBeforeChangingState()
        {
            CharacterRuntimeData member = CreateCharacter("member");
            CharacterRuntimeData reserve = CreateCharacter("reserve");
            GuildRuntimeData guild = CreateGuild(member, reserve);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings(
                desiredExpeditionMembers: 1,
                minimumGuildMembersRemaining: 1);

            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    new ExpeditionProcessor().Start(
                        guild,
                        new ExpeditionAssignment("manual-1", "forest", new[] { "member" }),
                        new ExpeditionAreaRegistry(new[] { area }),
                        settings,
                        new[] { "member" }));

                Assert.That(member.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(guild.Expeditions, Is.Empty);
            }
            finally
            {
                TestAssetFactory.Destroy(area, settings);
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
