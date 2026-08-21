using System;
using GuildFrontierSim.Application.Factories;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Presets;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;
using UnityEditor;

namespace GuildFrontierSim.Tests.Application.Factories
{
    public sealed class RuntimeDataFactoryTests
    {
        [Test]
        public void CharacterFactory_CopiesDefinitionIntoIndependentRuntimeData()
        {
            CharacterDefinition definition = TestAssetFactory.CreateCharacter(
                "alice",
                level: 3,
                maxHp: 120,
                attack: 14,
                defense: 12,
                speed: 11,
                salary: 25,
                loyalty: 40);

            try
            {
                CharacterRuntimeData runtime = new CharacterRuntimeDataFactory().Create(definition);
                runtime.ApplyDamage(20);
                runtime.ChangeLoyalty(-10);

                Assert.That(runtime.CharacterId, Is.EqualTo("alice"));
                Assert.That(runtime.CurrentHp, Is.EqualTo(100));
                Assert.That(runtime.Loyalty, Is.EqualTo(30));
                Assert.That(definition.MaxHp, Is.EqualTo(120));
                Assert.That(definition.StartingLoyalty, Is.EqualTo(40));
            }
            finally
            {
                TestAssetFactory.Destroy(definition);
            }
        }

        [Test]
        public void GuildFactory_CreatesPatternStateFromPreset()
        {
            CharacterDefinition alice = TestAssetFactory.CreateCharacter("alice");
            CharacterDefinition bob = TestAssetFactory.CreateCharacter("bob");
            GuildStartingPreset preset =
                TestAssetFactory.CreateGuildPreset("Small Guild", 500, "alice", alice, bob);

            try
            {
                GuildRuntimeData guild = new GuildRuntimeDataFactory().Create(preset);

                Assert.That(guild.GuildName, Is.EqualTo("Small Guild"));
                Assert.That(guild.Funds, Is.EqualTo(500));
                Assert.That(guild.Characters, Has.Count.EqualTo(2));
                Assert.That(guild.LeaderCharacterId, Is.EqualTo("alice"));
                Assert.That(guild.Leader.CharacterId, Is.EqualTo("alice"));
            }
            finally
            {
                TestAssetFactory.Destroy(preset, alice, bob);
            }
        }

        [Test]
        public void GuildFactory_WhenLeaderIdIsMissing_Throws()
        {
            CharacterDefinition alice = TestAssetFactory.CreateCharacter("alice");
            GuildStartingPreset preset =
                TestAssetFactory.CreateGuildPreset("Small Guild", 500, "missing", alice);

            try
            {
                Assert.Throws<ArgumentException>(() => new GuildRuntimeDataFactory().Create(preset));
            }
            finally
            {
                TestAssetFactory.Destroy(preset, alice);
            }
        }

        [Test]
        public void GuildFactory_WhenCharacterIdsAreDuplicated_Throws()
        {
            CharacterDefinition first = TestAssetFactory.CreateCharacter("same-id");
            CharacterDefinition second = TestAssetFactory.CreateCharacter("same-id");
            GuildStartingPreset preset =
                TestAssetFactory.CreateGuildPreset("Small Guild", 500, "same-id", first, second);

            try
            {
                Assert.Throws<ArgumentException>(() => new GuildRuntimeDataFactory().Create(preset));
            }
            finally
            {
                TestAssetFactory.Destroy(preset, first, second);
            }
        }

        [Test]
        public void GuildFactory_WhenPresetContainsNullMember_Throws()
        {
            CharacterDefinition alice = TestAssetFactory.CreateCharacter("alice");
            GuildStartingPreset preset =
                TestAssetFactory.CreateGuildPreset("Small Guild", 500, "alice", alice, null);

            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => new GuildRuntimeDataFactory().Create(preset));
            }
            finally
            {
                TestAssetFactory.Destroy(preset, alice);
            }
        }

        [Test]
        public void GuildFactory_DoesNotMutatePresetWhenRuntimeStateChanges()
        {
            CharacterDefinition alice = TestAssetFactory.CreateCharacter("alice");
            GuildStartingPreset preset =
                TestAssetFactory.CreateGuildPreset("Small Guild", 500, "alice", alice);

            try
            {
                GuildRuntimeData guild = new GuildRuntimeDataFactory().Create(preset);
                guild.TrySpendFunds(100);
                guild.Leader.ApplyDamage(10);

                var serializedPreset = new SerializedObject(preset);
                Assert.That(serializedPreset.FindProperty("startingFunds").intValue, Is.EqualTo(500));
                Assert.That(alice.MaxHp, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(preset, alice);
            }
        }
    }
}
