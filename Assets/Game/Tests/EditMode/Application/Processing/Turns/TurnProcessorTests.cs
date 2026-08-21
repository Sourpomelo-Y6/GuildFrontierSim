using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Application.Processing.Leadership;
using GuildFrontierSim.Application.Processing.Turns;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Turns
{
    public sealed class TurnProcessorTests
    {
        [Test]
        public void Process_AdvancesTurnAndReturnsEveryProcessingResult()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", salary: 10);
            GuildRuntimeData guild = CreateGuild(100, leader);
            BattleBalanceSettings settings = CreateSettings(salaryInterval: 2);

            try
            {
                TurnResult result = new TurnProcessor().Process(guild, settings);

                Assert.That(result.TurnNumber, Is.EqualTo(1));
                Assert.That(guild.CurrentTurn, Is.EqualTo(1));
                Assert.That(result.RecoveryResult, Is.Not.Null);
                Assert.That(result.SalaryResult.Outcome, Is.EqualTo(SalaryOutcome.NotDue));
                Assert.That(result.SalaryLoyaltyResult.LoyaltyChange, Is.Zero);
                Assert.That(
                    result.LeadershipResult.Outcome,
                    Is.EqualTo(LeadershipOutcome.LeaderContinues));
                Assert.That(guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_OnSalaryTurn_PaysSalaryOnce()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", salary: 10);
            GuildRuntimeData guild = CreateGuild(100, leader);
            BattleBalanceSettings settings = CreateSettings(salaryInterval: 2);
            var processor = new TurnProcessor();

            try
            {
                TurnResult first = processor.Process(guild, settings);
                TurnResult second = processor.Process(guild, settings);

                Assert.That(first.SalaryResult.Outcome, Is.EqualTo(SalaryOutcome.NotDue));
                Assert.That(second.TurnNumber, Is.EqualTo(2));
                Assert.That(second.SalaryResult.Outcome, Is.EqualTo(SalaryOutcome.Paid));
                Assert.That(second.SalaryResult.TotalSalary, Is.EqualTo(10));
                Assert.That(guild.Funds, Is.EqualTo(90));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenSalaryIsUnpaid_AppliesLoyaltyPenalty()
        {
            CharacterRuntimeData leader = CreateCharacter(
                "leader",
                salary: 20,
                loyalty: 30);
            GuildRuntimeData guild = CreateGuild(10, leader);
            BattleBalanceSettings settings = CreateSettings(
                salaryInterval: 1,
                unpaidPenalty: 7);

            try
            {
                TurnResult result = new TurnProcessor().Process(guild, settings);

                Assert.That(result.SalaryResult.Outcome, Is.EqualTo(SalaryOutcome.Unpaid));
                Assert.That(result.SalaryLoyaltyResult.LoyaltyChange, Is.EqualTo(-7));
                Assert.That(leader.Loyalty, Is.EqualTo(23));
                Assert.That(guild.Funds, Is.EqualTo(10));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_RecoversAndRemovesPendingMemberBeforeSalaryCalculation()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", salary: 10);
            CharacterRuntimeData departing = CreateCharacter("departing", salary: 50);
            departing.ChangeLoyalty(-130);
            departing.MarkDeparturePending();
            departing.SetStatus(CharacterStatus.Injured, 1);
            GuildRuntimeData guild = CreateGuild(100, leader, departing);
            BattleBalanceSettings settings = CreateSettings(salaryInterval: 1);

            try
            {
                TurnResult result = new TurnProcessor().Process(guild, settings);

                Assert.That(
                    result.RecoveryResult.DepartedCharacterIds,
                    Is.EqualTo(new[] { "departing" }));
                Assert.That(result.SalaryResult.TotalSalary, Is.EqualTo(10));
                Assert.That(result.SalaryResult.CharacterIds, Is.EqualTo(new[] { "leader" }));
                Assert.That(guild.Funds, Is.EqualTo(90));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WhenUnpaidLeaderReachesMinimum_ReplacesLeaderInSameTurn()
        {
            CharacterRuntimeData leader = CreateCharacter(
                "leader",
                salary: 20,
                loyalty: -95);
            CharacterRuntimeData replacement = CreateCharacter(
                "replacement",
                salary: 10,
                level: 2);
            GuildRuntimeData guild = CreateGuild(0, leader, replacement);
            BattleBalanceSettings settings = CreateSettings(
                salaryInterval: 1,
                unpaidPenalty: 10);

            try
            {
                TurnResult result = new TurnProcessor().Process(guild, settings);

                Assert.That(result.SalaryResult.Outcome, Is.EqualTo(SalaryOutcome.Unpaid));
                Assert.That(leader.IsDeparturePending, Is.True);
                Assert.That(
                    result.LeadershipResult.Outcome,
                    Is.EqualTo(LeadershipOutcome.LeaderReplaced));
                Assert.That(guild.LeaderCharacterId, Is.EqualTo("replacement"));
                Assert.That(guild.TryGetCharacter("leader", out _), Is.False);
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void Process_WithInvalidSettings_DoesNotAdvanceTurn()
        {
            CharacterRuntimeData leader = CreateCharacter("leader");
            GuildRuntimeData guild = CreateGuild(100, leader);
            BattleBalanceSettings settings = CreateSettings(salaryInterval: 0);

            try
            {
                Assert.Throws<System.ArgumentException>(
                    () => new TurnProcessor().Process(guild, settings));
                Assert.That(guild.CurrentTurn, Is.Zero);
                Assert.That(guild.Funds, Is.EqualTo(100));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        private static BattleBalanceSettings CreateSettings(
            int salaryInterval,
            int unpaidPenalty = 10)
        {
            return TestAssetFactory.CreateBattleSettings(
                salaryIntervalTurns: salaryInterval,
                unpaidSalaryLoyaltyPenalty: unpaidPenalty);
        }

        private static GuildRuntimeData CreateGuild(
            int funds,
            params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData(
                "Guild", funds, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(
            string id,
            int salary = 10,
            int loyalty = 30,
            int level = 1)
        {
            return new CharacterRuntimeData(
                id,
                level,
                100,
                10,
                10,
                10,
                salary,
                loyalty);
        }
    }
}
