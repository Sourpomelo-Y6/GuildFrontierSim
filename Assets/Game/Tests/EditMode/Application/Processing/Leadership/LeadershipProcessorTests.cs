using GuildFrontierSim.Application.Processing.Leadership;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Leadership
{
    public sealed class LeadershipProcessorTests
    {
        [Test]
        public void Process_WhenLeaderIsAvailable_ContinuesAndClearsActingLeader()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData member = CreateCharacter("member");
            GuildRuntimeData guild = CreateGuild(leader, member);
            guild.SetActingLeader(member.CharacterId);
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                LeadershipResult result = new LeadershipProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(LeadershipOutcome.LeaderContinues));
                Assert.That(result.LeaderId, Is.EqualTo("leader"));
                Assert.That(result.ActingLeaderId, Is.Empty);
                Assert.That(guild.ActingLeaderCharacterId, Is.Empty);
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenLeaderIsUnavailable_AssignsBestAvailableActingLeader()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", level: 10);
            CharacterRuntimeData lowerLevel = CreateCharacter("lower", level: 2, attack: 100);
            CharacterRuntimeData higherLevel = CreateCharacter("higher", level: 3, attack: 1);
            leader.SetStatus(CharacterStatus.Hospitalized, 2);
            GuildRuntimeData guild = CreateGuild(leader, lowerLevel, higherLevel);
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                LeadershipResult result = new LeadershipProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(LeadershipOutcome.ActingLeaderAssigned));
                Assert.That(result.LeaderId, Is.EqualTo("leader"));
                Assert.That(result.ActingLeaderId, Is.EqualTo("higher"));
                Assert.That(guild.ActingLeaderCharacterId, Is.EqualTo("higher"));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_SelectsByPowerThenLoyaltyThenCharacterId()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData lowerPower = CreateCharacter("a-low", attack: 9, loyalty: 100);
            CharacterRuntimeData lowerLoyalty = CreateCharacter("b-loyalty", attack: 10, loyalty: 20);
            CharacterRuntimeData laterId = CreateCharacter("z-id", attack: 10, loyalty: 30);
            CharacterRuntimeData earlierId = CreateCharacter("a-id", attack: 10, loyalty: 30);
            leader.SetStatus(CharacterStatus.Resting, 1);
            GuildRuntimeData guild = CreateGuild(
                leader,
                lowerPower,
                lowerLoyalty,
                laterId,
                earlierId);
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                LeadershipResult result = new LeadershipProcessor().Process(guild, settings);

                Assert.That(result.ActingLeaderId, Is.EqualTo("a-id"));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenLeaderDepartureIsPending_ReplacesAndRemovesOldLeader()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData replacement = CreateCharacter("replacement", level: 2);
            leader.MarkDeparturePending();
            GuildRuntimeData guild = CreateGuild(leader, replacement);
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                LeadershipResult result = new LeadershipProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(LeadershipOutcome.LeaderReplaced));
                Assert.That(result.PreviousLeaderId, Is.EqualTo("leader"));
                Assert.That(result.LeaderId, Is.EqualTo("replacement"));
                Assert.That(result.DepartedCharacterIds, Is.EqualTo(new[] { "leader" }));
                Assert.That(guild.TryGetCharacter("leader", out _), Is.False);
                Assert.That(guild.Leader, Is.SameAs(replacement));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenCapturedLeaderIsReplaced_KeepsDeparturePending()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData replacement = CreateCharacter("replacement");
            leader.SetStatus(CharacterStatus.Captured);
            leader.MarkDeparturePending();
            GuildRuntimeData guild = CreateGuild(leader, replacement);
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                LeadershipResult result = new LeadershipProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(LeadershipOutcome.LeaderReplaced));
                Assert.That(result.PendingDepartureCharacterIds, Is.EqualTo(new[] { "leader" }));
                Assert.That(guild.TryGetCharacter("leader", out _), Is.True);
                Assert.That(guild.Leader, Is.SameAs(replacement));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenNoCandidateExists_LeavesLeadershipValid()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData unavailable = CreateCharacter("unavailable");
            leader.SetStatus(CharacterStatus.Captured);
            unavailable.SetStatus(CharacterStatus.Injured, 2);
            GuildRuntimeData guild = CreateGuild(leader, unavailable);
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                LeadershipResult result = new LeadershipProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(LeadershipOutcome.NoCandidate));
                Assert.That(result.LeaderId, Is.EqualTo("leader"));
                Assert.That(result.ActingLeaderId, Is.Empty);
                Assert.That(guild.Leader, Is.SameAs(leader));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_DoesNotSelectMemberWhoseDepartureIsPending()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData pending = CreateCharacter("pending", level: 10);
            CharacterRuntimeData eligible = CreateCharacter("eligible", level: 1);
            leader.SetStatus(CharacterStatus.Expedition);
            pending.MarkDeparturePending();
            GuildRuntimeData guild = CreateGuild(leader, pending, eligible);
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                LeadershipResult result = new LeadershipProcessor().Process(guild, settings);

                Assert.That(result.ActingLeaderId, Is.EqualTo("eligible"));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
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
            int level = 1,
            int attack = 10,
            int loyalty = 30)
        {
            return new CharacterRuntimeData(
                id,
                level,
                100,
                attack,
                10,
                10,
                10,
                loyalty);
        }
    }
}
