using System;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Application.Processing.Leadership;
using GuildFrontierSim.Application.Processing.Recovery;

namespace GuildFrontierSim.Application.Processing.Turns
{
    public sealed class TurnResult
    {
        public TurnResult(
            int turnNumber,
            RecoveryResult recoveryResult,
            SalaryResult salaryResult,
            LoyaltyResult salaryLoyaltyResult,
            LeadershipResult leadershipResult)
        {
            if (turnNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turnNumber));
            }

            TurnNumber = turnNumber;
            RecoveryResult = recoveryResult ??
                throw new ArgumentNullException(nameof(recoveryResult));
            SalaryResult = salaryResult ??
                throw new ArgumentNullException(nameof(salaryResult));
            SalaryLoyaltyResult = salaryLoyaltyResult ??
                throw new ArgumentNullException(nameof(salaryLoyaltyResult));
            LeadershipResult = leadershipResult ??
                throw new ArgumentNullException(nameof(leadershipResult));
        }

        public int TurnNumber { get; }
        public RecoveryResult RecoveryResult { get; }
        public SalaryResult SalaryResult { get; }
        public LoyaltyResult SalaryLoyaltyResult { get; }
        public LeadershipResult LeadershipResult { get; }
    }
}
