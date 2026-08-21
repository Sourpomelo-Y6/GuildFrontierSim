using System;
using GuildFrontierSim.Domain.Characters;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Characters
{
    public sealed class CharacterRuntimeDataTests
    {
        [Test]
        public void DamageHealingAndLoyalty_StayWithinValidRanges()
        {
            CharacterRuntimeData character = CreateCharacter();

            character.ApplyDamage(150);
            character.ChangeLoyalty(-500);
            Assert.That(character.CurrentHp, Is.Zero);
            Assert.That(character.Loyalty, Is.EqualTo(-100));

            character.RestoreHp(500);
            character.ChangeLoyalty(500);
            Assert.That(character.CurrentHp, Is.EqualTo(character.MaxHp));
            Assert.That(character.Loyalty, Is.EqualTo(100));
        }

        [Test]
        public void AdvanceUnavailableTurn_WhenRecoveryCompletes_MakesCharacterAvailable()
        {
            CharacterRuntimeData character = CreateCharacter();
            character.SetStatus(CharacterStatus.Hospitalized, 2);

            character.AdvanceUnavailableTurn();
            Assert.That(character.Status, Is.EqualTo(CharacterStatus.Hospitalized));

            character.AdvanceUnavailableTurn();
            Assert.That(character.Status, Is.EqualTo(CharacterStatus.Available));
            Assert.That(character.UnavailableTurnsRemaining, Is.Zero);
        }

        [Test]
        public void Constructor_WhenCharacterIdIsEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(() => CreateCharacter(" "));
        }

        [TestCase(-101)]
        [TestCase(101)]
        public void Constructor_WhenLoyaltyIsOutsideRange_Throws(int loyalty)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CharacterRuntimeData("alice", 1, 100, 10, 10, 10, 10, loyalty));
        }

        private static CharacterRuntimeData CreateCharacter(string id = "alice")
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
