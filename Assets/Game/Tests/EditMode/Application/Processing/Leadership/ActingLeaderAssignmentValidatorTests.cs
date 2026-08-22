using System.Linq;
using GuildFrontierSim.Application.Assignments.Leadership;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Leadership
{
    public sealed class ActingLeaderAssignmentValidatorTests
    {
        [Test]
        public void GetCandidates_ReturnsOnlyAvailableNonLeaderMembers()
        {
            CharacterRuntimeData leader = Character("leader");
            CharacterRuntimeData available = Character("available");
            CharacterRuntimeData injured = Character("injured");
            CharacterRuntimeData hospitalized = Character("hospitalized");
            CharacterRuntimeData expedition = Character("expedition");
            CharacterRuntimeData captured = Character("captured");
            CharacterRuntimeData departing = Character("departing");
            injured.SetStatus(CharacterStatus.Injured, 1);
            hospitalized.SetStatus(CharacterStatus.Hospitalized, 1);
            expedition.SetStatus(CharacterStatus.Expedition);
            captured.SetStatus(CharacterStatus.Captured);
            departing.MarkDeparturePending();
            GuildRuntimeData guild = Guild(
                leader, available, injured, hospitalized, expedition, captured, departing);

            var validator = new ActingLeaderAssignmentValidator();

            Assert.That(
                validator.GetCandidates(guild).Select(character => character.CharacterId),
                Is.EqualTo(new[] { "available" }));
        }

        [Test]
        public void Validate_WithUnavailableLeaderAndValidCandidate_Succeeds()
        {
            CharacterRuntimeData leader = Character("leader");
            CharacterRuntimeData candidate = Character("candidate");
            leader.SetStatus(CharacterStatus.Hospitalized, 2);
            GuildRuntimeData guild = Guild(leader, candidate);

            ActingLeaderValidationResult result =
                new ActingLeaderAssignmentValidator().Validate(
                    guild,
                    new ActingLeaderAssignment("candidate"),
                    guild.Revision);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Candidate, Is.SameAs(candidate));
        }

        [Test]
        public void Validate_AfterRevisionChanges_ReturnsStaleRevision()
        {
            CharacterRuntimeData leader = Character("leader");
            CharacterRuntimeData candidate = Character("candidate");
            leader.SetStatus(CharacterStatus.Hospitalized, 2);
            GuildRuntimeData guild = Guild(leader, candidate);
            int oldRevision = guild.Revision;
            guild.MarkStateChanged();

            ActingLeaderValidationResult result =
                new ActingLeaderAssignmentValidator().Validate(
                    guild,
                    new ActingLeaderAssignment("candidate"),
                    oldRevision);

            Assert.That(result.Error, Is.EqualTo(ActingLeaderValidationError.StaleRevision));
        }

        [Test]
        public void Validate_WhenFormalLeaderIsAvailable_ReturnsLeaderAvailable()
        {
            GuildRuntimeData guild = Guild(Character("leader"), Character("candidate"));

            ActingLeaderValidationResult result =
                new ActingLeaderAssignmentValidator().Validate(
                    guild,
                    new ActingLeaderAssignment("candidate"),
                    guild.Revision);

            Assert.That(result.Error, Is.EqualTo(ActingLeaderValidationError.LeaderAvailable));
        }

        private static GuildRuntimeData Guild(params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData("Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData Character(string id)
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
