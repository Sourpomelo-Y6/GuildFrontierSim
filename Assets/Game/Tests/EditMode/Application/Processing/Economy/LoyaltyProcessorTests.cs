using System;
using System.Linq;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Economy
{
    public sealed class LoyaltyProcessorTests
    {
        [Test]
        public void ApplySalaryResult_WhenPaid_DoesNotChangeLoyalty()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", 30);
            GuildRuntimeData guild = CreateGuild(leader);
            var salaryResult = new SalaryResult(
                SalaryOutcome.Paid,
                leader.Salary,
                new[] { leader.CharacterId });
            BattleBalanceSettings settings = CreateSettings(unpaidPenalty: 10);

            try
            {
                LoyaltyResult result = new LoyaltyProcessor().ApplySalaryResult(
                    guild,
                    salaryResult,
                    settings);

                Assert.That(result.LoyaltyChange, Is.Zero);
                Assert.That(result.AffectedCharacterIds, Is.EqualTo(new[] { "leader" }));
                Assert.That(result.DepartedCharacterIds, Is.Empty);
                Assert.That(leader.Loyalty, Is.EqualTo(30));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void ApplySalaryResult_WhenUnpaid_PenalizesEveryTarget()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", 30);
            CharacterRuntimeData member = CreateCharacter("member", 15);
            GuildRuntimeData guild = CreateGuild(leader, member);
            var salaryResult = new SalaryResult(
                SalaryOutcome.Unpaid,
                20,
                new[] { leader.CharacterId, member.CharacterId });
            BattleBalanceSettings settings = CreateSettings(unpaidPenalty: 10);

            try
            {
                LoyaltyResult result = new LoyaltyProcessor().ApplySalaryResult(
                    guild,
                    salaryResult,
                    settings);

                Assert.That(result.LoyaltyChange, Is.EqualTo(-10));
                Assert.That(leader.Loyalty, Is.EqualTo(20));
                Assert.That(member.Loyalty, Is.EqualTo(5));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [TestCase(BattleOutcome.Victory, 4)]
        [TestCase(BattleOutcome.Defeat, -7)]
        [TestCase(BattleOutcome.Draw, 0)]
        public void ApplyBattleResult_AppliesConfiguredChange(
            BattleOutcome outcome,
            int expectedChange)
        {
            CharacterRuntimeData leader = CreateCharacter("leader", 20);
            GuildRuntimeData guild = CreateGuild(leader);
            BattleBalanceSettings settings = CreateSettings(
                victoryChange: 4,
                defeatChange: -7);

            try
            {
                LoyaltyResult result = new LoyaltyProcessor().ApplyBattleResult(
                    guild,
                    new[] { leader.CharacterId },
                    outcome,
                    settings);

                Assert.That(result.LoyaltyChange, Is.EqualTo(expectedChange));
                Assert.That(leader.Loyalty, Is.EqualTo(20 + expectedChange));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void ApplySalaryResult_AtMinimumLoyalty_RemovesAvailableMember()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", 30);
            CharacterRuntimeData member = CreateCharacter("member", -95);
            GuildRuntimeData guild = CreateGuild(leader, member);
            var salaryResult = new SalaryResult(
                SalaryOutcome.Unpaid,
                member.Salary,
                new[] { member.CharacterId });
            BattleBalanceSettings settings = CreateSettings(unpaidPenalty: 10);

            try
            {
                LoyaltyResult result = new LoyaltyProcessor().ApplySalaryResult(
                    guild,
                    salaryResult,
                    settings);

                Assert.That(member.Loyalty, Is.EqualTo(-100));
                Assert.That(result.DepartedCharacterIds, Is.EqualTo(new[] { "member" }));
                Assert.That(result.PendingDepartureCharacterIds, Is.Empty);
                Assert.That(guild.TryGetCharacter("member", out _), Is.False);
                Assert.That(guild.Characters.Single(), Is.SameAs(leader));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void ApplySalaryResult_WhenCharacterIsCaptured_QueuesDeparture()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", 30);
            CharacterRuntimeData captured = CreateCharacter("captured", -95);
            captured.SetStatus(CharacterStatus.Captured);
            GuildRuntimeData guild = CreateGuild(leader, captured);
            var salaryResult = new SalaryResult(
                SalaryOutcome.Unpaid,
                captured.Salary,
                new[] { captured.CharacterId });
            BattleBalanceSettings settings = CreateSettings(unpaidPenalty: 10);

            try
            {
                LoyaltyResult result = new LoyaltyProcessor().ApplySalaryResult(
                    guild,
                    salaryResult,
                    settings);

                Assert.That(result.DepartedCharacterIds, Is.Empty);
                Assert.That(result.PendingDepartureCharacterIds, Is.EqualTo(new[] { "captured" }));
                Assert.That(captured.IsDeparturePending, Is.True);
                Assert.That(guild.TryGetCharacter("captured", out _), Is.True);
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void ResolvePendingDepartures_WhenCharacterBecomesAvailable_RemovesCharacter()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", 30);
            CharacterRuntimeData member = CreateCharacter("member", -100);
            member.SetStatus(CharacterStatus.Captured);
            member.MarkDeparturePending();
            GuildRuntimeData guild = CreateGuild(leader, member);
            member.SetStatus(CharacterStatus.Available);

            LoyaltyResult result = new LoyaltyProcessor().ResolvePendingDepartures(guild);

            Assert.That(result.DepartedCharacterIds, Is.EqualTo(new[] { "member" }));
            Assert.That(guild.TryGetCharacter("member", out _), Is.False);
        }

        [Test]
        public void ApplySalaryResult_WhenLeaderWouldLeave_QueuesDepartureForLeadershipStep()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", -95);
            CharacterRuntimeData member = CreateCharacter("member", 30);
            GuildRuntimeData guild = CreateGuild(leader, member);
            var salaryResult = new SalaryResult(
                SalaryOutcome.Unpaid,
                leader.Salary,
                new[] { leader.CharacterId });
            BattleBalanceSettings settings = CreateSettings(unpaidPenalty: 10);

            try
            {
                LoyaltyResult result = new LoyaltyProcessor().ApplySalaryResult(
                    guild,
                    salaryResult,
                    settings);

                Assert.That(result.DepartedCharacterIds, Is.Empty);
                Assert.That(result.PendingDepartureCharacterIds, Is.EqualTo(new[] { "leader" }));
                Assert.That(leader.IsDeparturePending, Is.True);
                Assert.That(guild.Leader, Is.SameAs(leader));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        [Test]
        public void ApplyBattleResult_WithDuplicateId_RejectsBeforeChangingLoyalty()
        {
            CharacterRuntimeData leader = CreateCharacter("leader", 20);
            GuildRuntimeData guild = CreateGuild(leader);
            BattleBalanceSettings settings = CreateSettings(victoryChange: 4);

            try
            {
                Assert.Throws<ArgumentException>(
                    () => new LoyaltyProcessor().ApplyBattleResult(
                        guild,
                        new[] { leader.CharacterId, leader.CharacterId },
                        BattleOutcome.Victory,
                        settings));
                Assert.That(leader.Loyalty, Is.EqualTo(20));
            }
            finally
            {
                TestAssetFactory.Destroy(settings);
            }
        }

        private static BattleBalanceSettings CreateSettings(
            int unpaidPenalty = 10,
            int victoryChange = 2,
            int defeatChange = -3)
        {
            return TestAssetFactory.CreateBattleSettings(
                unpaidSalaryLoyaltyPenalty: unpaidPenalty,
                victoryLoyaltyChange: victoryChange,
                defeatLoyaltyChange: defeatChange);
        }

        private static GuildRuntimeData CreateGuild(
            params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData(
                "Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(string id, int loyalty)
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, 10, loyalty);
        }
    }
}
