using GuildFrontierSim.Application.Processing.Recovery;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Recovery
{
    public sealed class RecoveryProcessorTests
    {
        [TestCase(CharacterStatus.Injured)]
        [TestCase(CharacterStatus.Hospitalized)]
        [TestCase(CharacterStatus.Resting)]
        public void Process_DecrementsTimedRecoveryStatus(CharacterStatus status)
        {
            CharacterRuntimeData character = CreateCharacter("leader");
            character.SetStatus(status, 2);
            GuildRuntimeData guild = CreateGuild(character);

            RecoveryResult result = new RecoveryProcessor().Process(guild);

            Assert.That(result.AdvancedCharacterIds, Is.EqualTo(new[] { "leader" }));
            Assert.That(result.RecoveredCharacterIds, Is.Empty);
            Assert.That(character.Status, Is.EqualTo(status));
            Assert.That(character.UnavailableTurnsRemaining, Is.EqualTo(1));
        }

        [TestCase(CharacterStatus.Injured)]
        [TestCase(CharacterStatus.Hospitalized)]
        [TestCase(CharacterStatus.Resting)]
        public void Process_WhenFinalRecoveryTurnEnds_MakesCharacterAvailable(
            CharacterStatus status)
        {
            CharacterRuntimeData character = CreateCharacter("leader");
            character.SetStatus(status, 1);
            GuildRuntimeData guild = CreateGuild(character);

            RecoveryResult result = new RecoveryProcessor().Process(guild);

            Assert.That(result.RecoveredCharacterIds, Is.EqualTo(new[] { "leader" }));
            Assert.That(character.Status, Is.EqualTo(CharacterStatus.Available));
            Assert.That(character.UnavailableTurnsRemaining, Is.Zero);
        }

        [TestCase(CharacterStatus.Captured)]
        [TestCase(CharacterStatus.Expedition)]
        [TestCase(CharacterStatus.Defending)]
        [TestCase(CharacterStatus.Available)]
        public void Process_DoesNotAdvanceNonRecoveryStatus(CharacterStatus status)
        {
            CharacterRuntimeData character = CreateCharacter("leader");
            character.SetStatus(status);
            GuildRuntimeData guild = CreateGuild(character);

            RecoveryResult result = new RecoveryProcessor().Process(guild);

            Assert.That(result.AdvancedCharacterIds, Is.Empty);
            Assert.That(result.RecoveredCharacterIds, Is.Empty);
            Assert.That(character.Status, Is.EqualTo(status));
        }

        [Test]
        public void Process_WhenPendingMemberRecovers_RemovesMemberSafely()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData member = CreateCharacter("member");
            member.ChangeLoyalty(-130);
            member.MarkDeparturePending();
            member.SetStatus(CharacterStatus.Injured, 1);
            GuildRuntimeData guild = CreateGuild(leader, member);

            RecoveryResult result = new RecoveryProcessor().Process(guild);

            Assert.That(result.RecoveredCharacterIds, Is.EqualTo(new[] { "member" }));
            Assert.That(result.DepartedCharacterIds, Is.EqualTo(new[] { "member" }));
            Assert.That(result.PendingDepartureCharacterIds, Is.Empty);
            Assert.That(guild.TryGetCharacter("member", out _), Is.False);
        }

        [Test]
        public void Process_WhenPendingLeaderRecovers_KeepsDeparturePending()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            CharacterRuntimeData member = CreateCharacter("member");
            leader.ChangeLoyalty(-130);
            leader.MarkDeparturePending();
            leader.SetStatus(CharacterStatus.Hospitalized, 1);
            GuildRuntimeData guild = CreateGuild(leader, member);

            RecoveryResult result = new RecoveryProcessor().Process(guild);

            Assert.That(result.RecoveredCharacterIds, Is.EqualTo(new[] { "leader" }));
            Assert.That(result.DepartedCharacterIds, Is.Empty);
            Assert.That(result.PendingDepartureCharacterIds, Is.EqualTo(new[] { "leader" }));
            Assert.That(guild.Leader, Is.SameAs(leader));
            Assert.That(leader.Status, Is.EqualTo(CharacterStatus.Available));
        }

        private static GuildRuntimeData CreateGuild(
            params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData(
                "Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(string id)
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
