using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Processing.Defense;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Defense
{
    public sealed class DefenseBattleRequestTests
    {
        [Test]
        public void Constructor_CopiesExcludedIds()
        {
            var ids = new List<string> { "alice" };
            var request = new DefenseBattleRequest(100f, ids);
            ids.Clear();

            Assert.That(request.ExcludedCharacterIds, Is.EqualTo(new[] { "alice" }));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_WhenEnemyPowerIsInvalid_Throws(float enemyPower)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DefenseBattleRequest(enemyPower));
        }
    }
}
