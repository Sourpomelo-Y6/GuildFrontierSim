using System.Linq;
using GuildFrontierSim.Application.Selection;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Selection
{
    public sealed class CpuMemberSelectorTests
    {
        [Test]
        public void Select_PrioritizesHigherPowerCharacters()
        {
            GuildRuntimeData guild = CreateGuild(
                CreateCharacter("weak", attack: 5),
                CreateCharacter("strong", attack: 30),
                CreateCharacter("medium", attack: 15));
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 2);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                MemberSelectionResult result = SelectDefense(
                    guild,
                    selectionSettings,
                    battleSettings);

                Assert.That(
                    result.SelectedMembers.Select(member => member.CharacterId),
                    Is.EqualTo(new[] { "strong", "medium" }));
                Assert.That(result.IsComplete, Is.True);
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Select_WhenPowerIsEqual_UsesCharacterIdOrder()
        {
            GuildRuntimeData guild = CreateGuild(
                CreateCharacter("charlie"),
                CreateCharacter("alice"),
                CreateCharacter("bob"));
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 3);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                MemberSelectionResult result = SelectDefense(
                    guild,
                    selectionSettings,
                    battleSettings);

                Assert.That(
                    result.SelectedMembers.Select(member => member.CharacterId),
                    Is.EqualTo(new[] { "alice", "bob", "charlie" }));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Select_ExcludesUnavailableLowHpAndRequestedIds()
        {
            CharacterRuntimeData available = CreateCharacter("available", attack: 10);
            CharacterRuntimeData captured = CreateCharacter("captured", attack: 100);
            CharacterRuntimeData lowHp = CreateCharacter("low-hp", attack: 100);
            CharacterRuntimeData alreadySelected = CreateCharacter("defender", attack: 100);
            captured.SetStatus(CharacterStatus.Captured);
            lowHp.ApplyDamage(60);
            GuildRuntimeData guild = CreateGuild(available, captured, lowHp, alreadySelected);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredDefenseMembers: 4,
                    minimumHpRatio: 0.5f);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();
            var request = new MemberSelectionRequest(
                MemberSelectionPurpose.Defense,
                new[] { alreadySelected.CharacterId });

            try
            {
                MemberSelectionResult result = new CpuMemberSelector().Select(
                    guild,
                    request,
                    selectionSettings,
                    battleSettings);

                Assert.That(
                    result.SelectedMembers.Select(member => member.CharacterId),
                    Is.EqualTo(new[] { "available" }));
                Assert.That(result.UnfilledCount, Is.EqualTo(3));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void SelectExpedition_LeavesConfiguredMinimumMembers()
        {
            GuildRuntimeData guild = CreateGuild(
                CreateCharacter("a", attack: 50),
                CreateCharacter("b", attack: 40),
                CreateCharacter("c", attack: 30),
                CreateCharacter("d", attack: 20),
                CreateCharacter("e", attack: 10));
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredExpeditionMembers: 4,
                    minimumGuildMembersRemaining: 2);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                MemberSelectionResult result = new CpuMemberSelector().Select(
                    guild,
                    new MemberSelectionRequest(MemberSelectionPurpose.Expedition),
                    selectionSettings,
                    battleSettings);

                Assert.That(result.SelectedMembers, Has.Count.EqualTo(3));
                Assert.That(result.UnfilledCount, Is.EqualTo(1));
                Assert.That(
                    result.SelectedMembers.Select(member => member.CharacterId),
                    Is.EqualTo(new[] { "a", "b", "c" }));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void SelectDefense_DoesNotApplyExpeditionReserve()
        {
            GuildRuntimeData guild = CreateGuild(
                CreateCharacter("a"),
                CreateCharacter("b"));
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(
                    desiredDefenseMembers: 2,
                    minimumGuildMembersRemaining: 2);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                MemberSelectionResult result = SelectDefense(
                    guild,
                    selectionSettings,
                    battleSettings);

                Assert.That(result.SelectedMembers, Has.Count.EqualTo(2));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        [Test]
        public void Select_DoesNotChangeCharacterStatus()
        {
            CharacterRuntimeData first = CreateCharacter("first");
            CharacterRuntimeData second = CreateCharacter("second");
            GuildRuntimeData guild = CreateGuild(first, second);
            CpuSelectionSettings selectionSettings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);
            BattleBalanceSettings battleSettings = TestAssetFactory.CreateBattleSettings();

            try
            {
                SelectDefense(guild, selectionSettings, battleSettings);

                Assert.That(first.Status, Is.EqualTo(CharacterStatus.Available));
                Assert.That(second.Status, Is.EqualTo(CharacterStatus.Available));
            }
            finally
            {
                TestAssetFactory.Destroy(selectionSettings, battleSettings);
            }
        }

        private static MemberSelectionResult SelectDefense(
            GuildRuntimeData guild,
            CpuSelectionSettings selectionSettings,
            BattleBalanceSettings battleSettings)
        {
            return new CpuMemberSelector().Select(
                guild,
                new MemberSelectionRequest(MemberSelectionPurpose.Defense),
                selectionSettings,
                battleSettings);
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
