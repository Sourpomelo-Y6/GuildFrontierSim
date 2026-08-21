using System;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Tests.Infrastructure.Random;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Battles
{
    public sealed class BattleResolverTests
    {
        [TestCase(30f, BattleOutcome.Victory)]
        [TestCase(40f, BattleOutcome.Defeat)]
        [TestCase(32.5f, BattleOutcome.Draw)]
        public void Resolve_DeterminesOutcomeFromFinalPower(
            float enemyPower,
            BattleOutcome expectedOutcome)
        {
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 1f,
                maximumPowerMultiplier: 1f);
            CharacterRuntimeData character = CreateCharacter();
            var resolver = new BattleResolver(
                new SequenceRandomSource(floatValues: new[] { 0.5f, 0.5f }));

            try
            {
                BattleResult result = resolver.Resolve(
                    new BattleInput(new[] { character }, enemyPower),
                    settings);

                Assert.That(result.Outcome, Is.EqualTo(expectedOutcome));
                Assert.That(result.PartyBasePower, Is.EqualTo(32.5f).Within(0.0001f));
                Assert.That(result.ParticipantIds, Is.EqualTo(new[] { "alice" }));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Resolve_AppliesIndependentRandomModifiers()
        {
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings(
                minimumPowerMultiplier: 0.8f,
                maximumPowerMultiplier: 1.2f);
            var resolver = new BattleResolver(
                new SequenceRandomSource(floatValues: new[] { 1f, 0f }));

            try
            {
                BattleResult result = resolver.Resolve(
                    new BattleInput(new[] { CreateCharacter() }, 32.5f),
                    settings);

                Assert.That(result.PartyPowerMultiplier, Is.EqualTo(1.2f).Within(0.0001f));
                Assert.That(result.EnemyPowerMultiplier, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(result.FinalPartyPower, Is.EqualTo(39f).Within(0.0001f));
                Assert.That(result.FinalEnemyPower, Is.EqualTo(26f).Within(0.0001f));
                Assert.That(result.PowerDifference, Is.EqualTo(13f).Within(0.0001f));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Resolve_DoesNotModifyCharacterState()
        {
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();
            CharacterRuntimeData character = CreateCharacter();
            character.SetStatus(CharacterStatus.Defending);
            int hpBefore = character.CurrentHp;
            int loyaltyBefore = character.Loyalty;
            CharacterStatus statusBefore = character.Status;
            var resolver = new BattleResolver(
                new SequenceRandomSource(floatValues: new[] { 0.5f, 0.5f }));

            try
            {
                resolver.Resolve(new BattleInput(new[] { character }, 30f), settings);

                Assert.That(character.CurrentHp, Is.EqualTo(hpBefore));
                Assert.That(character.Loyalty, Is.EqualTo(loyaltyBefore));
                Assert.That(character.Status, Is.EqualTo(statusBefore));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Resolve_WhenRandomValueIsOutsideUnitRange_Throws()
        {
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();
            var resolver = new BattleResolver(
                new SequenceRandomSource(floatValues: new[] { 1.1f, 0.5f }));

            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => resolver.Resolve(
                        new BattleInput(new[] { CreateCharacter() }, 30f),
                        settings));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        private static CharacterRuntimeData CreateCharacter()
        {
            return new CharacterRuntimeData("alice", 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
