using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Expeditions
{
    public sealed class ExpeditionAssignmentValidatorTests
    {
        [Test]
        public void Validate_WithRegisteredAreaAndValidMembers_SucceedsInGivenOrder()
        {
            CharacterRuntimeData first = Character("first");
            CharacterRuntimeData second = Character("second");
            GuildRuntimeData guild = Guild(first, second);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings(
                desiredExpeditionMembers: 2,
                minimumGuildMembersRemaining: 0);

            try
            {
                ExpeditionAssignmentValidationResult result = Validate(
                    guild, area, settings, new[] { "second", "first" });

                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Area, Is.SameAs(area));
                Assert.That(result.Participants, Is.EqualTo(new[] { second, first }));
            }
            finally { TestAssetFactory.Destroy(area, settings); }
        }

        [Test]
        public void Validate_WithUnknownArea_ReturnsAreaNotFound()
        {
            CharacterRuntimeData member = Character("member");
            GuildRuntimeData guild = Guild(member);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings();

            try
            {
                ExpeditionAssignmentValidationResult result =
                    new ExpeditionAssignmentValidator().Validate(
                        guild,
                        new ExpeditionAssignment("exp-1", "ruins", new string[0]),
                        new ExpeditionAreaRegistry(new[] { area }),
                        settings);
                Assert.That(result.Error, Is.EqualTo(ExpeditionAssignmentValidationError.AreaNotFound));
            }
            finally { TestAssetFactory.Destroy(area, settings); }
        }

        [Test]
        public void Validate_WithDuplicateMember_ReturnsDuplicateError()
        {
            CharacterRuntimeData member = Character("member");
            GuildRuntimeData guild = Guild(member);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings(
                desiredExpeditionMembers: 2,
                minimumGuildMembersRemaining: 0);

            try
            {
                ExpeditionAssignmentValidationResult result = Validate(
                    guild, area, settings, new[] { "member", "member" });
                Assert.That(result.Error,
                    Is.EqualTo(ExpeditionAssignmentValidationError.DuplicateCharacterId));
            }
            finally { TestAssetFactory.Destroy(area, settings); }
        }

        [Test]
        public void Validate_WhenReserveWouldBeViolated_ReturnsReserveError()
        {
            CharacterRuntimeData member = Character("member");
            GuildRuntimeData guild = Guild(member);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings(
                desiredExpeditionMembers: 1,
                minimumGuildMembersRemaining: 1);

            try
            {
                ExpeditionAssignmentValidationResult result = Validate(
                    guild, area, settings, new[] { "member" });
                Assert.That(result.Error, Is.EqualTo(
                    ExpeditionAssignmentValidationError.InsufficientGuildMembersRemaining));
            }
            finally { TestAssetFactory.Destroy(area, settings); }
        }

        [Test]
        public void Validate_WithDefenseMember_ReturnsReservedError()
        {
            CharacterRuntimeData member = Character("member");
            CharacterRuntimeData reserve = Character("reserve");
            GuildRuntimeData guild = Guild(member, reserve);
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings(
                desiredExpeditionMembers: 1,
                minimumGuildMembersRemaining: 1);

            try
            {
                ExpeditionAssignmentValidationResult result =
                    new ExpeditionAssignmentValidator().Validate(
                        guild,
                        new ExpeditionAssignment("exp-1", "forest", new[] { "member" }),
                        new ExpeditionAreaRegistry(new[] { area }),
                        settings,
                        new[] { "member" });
                Assert.That(result.Error,
                    Is.EqualTo(ExpeditionAssignmentValidationError.ReservedForDefense));
            }
            finally { TestAssetFactory.Destroy(area, settings); }
        }

        [Test]
        public void Validate_WithExistingExpeditionId_ReturnsDuplicateExpeditionError()
        {
            CharacterRuntimeData active = Character("active");
            CharacterRuntimeData reserve = Character("reserve");
            active.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = Guild(active, reserve);
            guild.AddExpedition(new ExpeditionRuntimeData(
                "exp-1", "forest", new[] { "active" }, 100, 3, 1f, true));
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            CpuSelectionSettings settings = TestAssetFactory.CreateCpuSelectionSettings();

            try
            {
                ExpeditionAssignmentValidationResult result = Validate(
                    guild, area, settings, new string[0]);
                Assert.That(result.Error,
                    Is.EqualTo(ExpeditionAssignmentValidationError.DuplicateExpeditionId));
            }
            finally { TestAssetFactory.Destroy(area, settings); }
        }

        private static ExpeditionAssignmentValidationResult Validate(
            GuildRuntimeData guild,
            ExpeditionAreaDefinition area,
            CpuSelectionSettings settings,
            string[] ids)
        {
            return new ExpeditionAssignmentValidator().Validate(
                guild,
                new ExpeditionAssignment("exp-1", area.Id, ids),
                new ExpeditionAreaRegistry(new[] { area }),
                settings);
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
