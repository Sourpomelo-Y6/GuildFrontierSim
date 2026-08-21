using System;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Battles
{
    public sealed class BattlePowerCalculatorTests
    {
        [Test]
        public void Calculate_SumsWeightedCharacterStatistics()
        {
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings(
                attackWeight: 2f,
                defenseWeight: 1f,
                hitPointWeight: 0.5f,
                speedWeight: 0.25f);
            var first = new CharacterRuntimeData("first", 1, 100, 10, 8, 4, 0, 30);
            var second = new CharacterRuntimeData("second", 1, 80, 6, 5, 12, 0, 30);
            second.ApplyDamage(20);

            try
            {
                float power = new BattlePowerCalculator().Calculate(
                    new[] { first, second },
                    settings);

                Assert.That(power, Is.EqualTo(129f).Within(0.0001f));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Calculate_WhenPartyIsEmpty_Throws()
        {
            BattleBalanceSettings settings = TestAssetFactory.CreateBattleSettings();

            try
            {
                Assert.Throws<ArgumentException>(
                    () => new BattlePowerCalculator().Calculate(
                        Array.Empty<CharacterRuntimeData>(),
                        settings));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }
    }
}
