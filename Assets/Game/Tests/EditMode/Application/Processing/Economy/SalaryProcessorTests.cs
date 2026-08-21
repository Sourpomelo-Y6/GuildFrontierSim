using System;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Economy
{
    public sealed class SalaryProcessorTests
    {
        [Test]
        public void Process_OnPaymentTurn_PaysEveryMemberAtOnce()
        {
            GuildRuntimeData guild = CreateGuild(
                100,
                CreateCharacter("leader", 30),
                CreateCharacter("member", 20));
            AdvanceTurns(guild, 5);
            BattleBalanceSettings settings = CreateSettings(5);

            try
            {
                SalaryResult result = new SalaryProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(SalaryOutcome.Paid));
                Assert.That(result.WasPaid, Is.True);
                Assert.That(result.TotalSalary, Is.EqualTo(50));
                Assert.That(result.CharacterIds, Is.EqualTo(new[] { "leader", "member" }));
                Assert.That(guild.Funds, Is.EqualTo(50));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenFundsExactlyMatch_PaysSuccessfully()
        {
            GuildRuntimeData guild = CreateGuild(30, CreateCharacter("leader", 30));
            AdvanceTurns(guild, 1);
            BattleBalanceSettings settings = CreateSettings(1);

            try
            {
                SalaryResult result = new SalaryProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(SalaryOutcome.Paid));
                Assert.That(guild.Funds, Is.Zero);
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenFundsAreInsufficient_LeavesAllFundsUntouched()
        {
            GuildRuntimeData guild = CreateGuild(
                49,
                CreateCharacter("leader", 30),
                CreateCharacter("member", 20));
            AdvanceTurns(guild, 1);
            BattleBalanceSettings settings = CreateSettings(1);

            try
            {
                SalaryResult result = new SalaryProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(SalaryOutcome.Unpaid));
                Assert.That(result.WasPaid, Is.False);
                Assert.That(result.TotalSalary, Is.EqualTo(50));
                Assert.That(result.CharacterIds, Is.EqualTo(new[] { "leader", "member" }));
                Assert.That(guild.Funds, Is.EqualTo(49));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_BeforePaymentTurn_DoesNothing()
        {
            GuildRuntimeData guild = CreateGuild(100, CreateCharacter("leader", 30));
            AdvanceTurns(guild, 4);
            BattleBalanceSettings settings = CreateSettings(5);

            try
            {
                SalaryResult result = new SalaryProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(SalaryOutcome.NotDue));
                Assert.That(result.TotalSalary, Is.Zero);
                Assert.That(result.CharacterIds, Is.Empty);
                Assert.That(guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_AtTurnZero_DoesNotChargeSalary()
        {
            GuildRuntimeData guild = CreateGuild(100, CreateCharacter("leader", 30));
            BattleBalanceSettings settings = CreateSettings(5);

            try
            {
                SalaryResult result = new SalaryProcessor().Process(guild, settings);

                Assert.That(result.Outcome, Is.EqualTo(SalaryOutcome.NotDue));
                Assert.That(guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenTotalSalaryOverflows_IsAtomic()
        {
            GuildRuntimeData guild = CreateGuild(
                int.MaxValue,
                CreateCharacter("leader", int.MaxValue),
                CreateCharacter("member", 1));
            AdvanceTurns(guild, 1);
            BattleBalanceSettings settings = CreateSettings(1);

            try
            {
                Assert.Throws<OverflowException>(
                    () => new SalaryProcessor().Process(guild, settings));
                Assert.That(guild.Funds, Is.EqualTo(int.MaxValue));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        private static BattleBalanceSettings CreateSettings(int interval)
        {
            return TestAssetFactory.CreateBattleSettings(salaryIntervalTurns: interval);
        }

        private static GuildRuntimeData CreateGuild(
            int funds,
            params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData(
                "Guild", funds, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(string id, int salary)
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, salary, 30);
        }

        private static void AdvanceTurns(GuildRuntimeData guild, int count)
        {
            for (int index = 0; index < count; index++)
            {
                guild.AdvanceTurnNumber();
            }
        }
    }
}
