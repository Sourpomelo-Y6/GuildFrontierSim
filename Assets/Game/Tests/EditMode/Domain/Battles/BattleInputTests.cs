using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Battles
{
    public sealed class BattleInputTests
    {
        [Test]
        public void Constructor_CopiesParticipantCollection()
        {
            CharacterRuntimeData character = CreateCharacter();
            var source = new List<CharacterRuntimeData> { character };

            var input = new BattleInput(source, 10f);
            source.Clear();

            Assert.That(input.Participants, Has.Count.EqualTo(1));
            Assert.That(input.Participants[0], Is.SameAs(character));
        }

        [Test]
        public void Constructor_WhenPartyIsEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new BattleInput(Array.Empty<CharacterRuntimeData>(), 10f));
        }

        [Test]
        public void Constructor_WhenParticipantHasNoHp_Throws()
        {
            CharacterRuntimeData character = CreateCharacter();
            character.ApplyDamage(character.MaxHp);

            Assert.Throws<ArgumentException>(
                () => new BattleInput(new[] { character }, 10f));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_WhenEnemyPowerIsInvalid_Throws(float enemyPower)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BattleInput(new[] { CreateCharacter() }, enemyPower));
        }

        private static CharacterRuntimeData CreateCharacter()
        {
            return new CharacterRuntimeData("alice", 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
