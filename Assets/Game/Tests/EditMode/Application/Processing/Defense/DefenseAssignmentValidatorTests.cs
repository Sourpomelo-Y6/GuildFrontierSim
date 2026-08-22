using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Defense
{
    public sealed class DefenseAssignmentValidatorTests
    {
        [Test]
        public void Validate_WithValidIds_ReturnsCharactersInAssignmentOrder()
        {
            CharacterRuntimeData first = CreateCharacter("first");
            CharacterRuntimeData second = CreateCharacter("second");
            GuildRuntimeData guild = CreateGuild(first, second);
            CpuSelectionSettings settings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 2);

            try
            {
                DefenseAssignmentValidationResult result =
                    new DefenseAssignmentValidator().Validate(
                        guild,
                        new DefenseAssignment(100f, new[] { "second", "first" }),
                        settings);

                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Defenders, Is.EqualTo(new[] { second, first }));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [TestCase("missing", DefenseAssignmentValidationError.CharacterNotFound)]
        [TestCase("", DefenseAssignmentValidationError.InvalidCharacterId)]
        public void Validate_WithInvalidId_ReturnsExpectedError(
            string characterId,
            DefenseAssignmentValidationError expectedError)
        {
            CharacterRuntimeData member = CreateCharacter("member");
            GuildRuntimeData guild = CreateGuild(member);
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings();

            try
            {
                DefenseAssignmentValidationResult result =
                    new DefenseAssignmentValidator().Validate(
                        guild,
                        new DefenseAssignment(100f, new[] { characterId }),
                        settings);

                Assert.That(result.Error, Is.EqualTo(expectedError));
                Assert.That(result.IsValid, Is.False);
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Validate_WithDuplicateId_ReturnsDuplicateError()
        {
            CharacterRuntimeData member = CreateCharacter("member");
            GuildRuntimeData guild = CreateGuild(member);
            CpuSelectionSettings settings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 2);

            try
            {
                DefenseAssignmentValidationResult result =
                    new DefenseAssignmentValidator().Validate(
                        guild,
                        new DefenseAssignment(100f, new[] { "member", "member" }),
                        settings);

                Assert.That(result.Error,
                    Is.EqualTo(DefenseAssignmentValidationError.DuplicateCharacterId));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Validate_WithTooManyDefenders_ReturnsCountError()
        {
            CharacterRuntimeData first = CreateCharacter("first");
            CharacterRuntimeData second = CreateCharacter("second");
            GuildRuntimeData guild = CreateGuild(first, second);
            CpuSelectionSettings settings =
                TestAssetFactory.CreateCpuSelectionSettings(desiredDefenseMembers: 1);

            try
            {
                DefenseAssignmentValidationResult result =
                    new DefenseAssignmentValidator().Validate(
                        guild,
                        new DefenseAssignment(100f, new[] { "first", "second" }),
                        settings);

                Assert.That(result.Error,
                    Is.EqualTo(DefenseAssignmentValidationError.TooManyDefenders));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Validate_WithLowHitPoints_ReturnsHitPointError()
        {
            CharacterRuntimeData member = CreateCharacter("member");
            member.ApplyDamage(60);
            GuildRuntimeData guild = CreateGuild(member);
            CpuSelectionSettings settings =
                TestAssetFactory.CreateCpuSelectionSettings(minimumHpRatio: 0.5f);

            try
            {
                DefenseAssignmentValidationResult result =
                    new DefenseAssignmentValidator().Validate(
                        guild,
                        new DefenseAssignment(100f, new[] { "member" }),
                        settings);

                Assert.That(result.Error,
                    Is.EqualTo(DefenseAssignmentValidationError.HitPointsBelowMinimum));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Validate_WithDeparturePendingMember_ReturnsUnavailableError()
        {
            CharacterRuntimeData member = CreateCharacter("member");
            member.MarkDeparturePending();
            GuildRuntimeData guild = CreateGuild(member);
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings();

            try
            {
                DefenseAssignmentValidationResult result =
                    new DefenseAssignmentValidator().Validate(
                        guild,
                        new DefenseAssignment(100f, new[] { "member" }),
                        settings);

                Assert.That(result.Error,
                    Is.EqualTo(DefenseAssignmentValidationError.CharacterUnavailable));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        private static GuildRuntimeData CreateGuild(params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData("Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(string id)
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
