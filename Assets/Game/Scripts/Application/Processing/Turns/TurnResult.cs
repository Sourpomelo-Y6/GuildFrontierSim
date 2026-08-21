using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
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
            LeadershipResult leadershipResult,
            IEnumerable<ExpeditionReturnResult> expeditionReturnResults = null,
            IEnumerable<ExpeditionStageResult> expeditionStageResults = null,
            IEnumerable<LoyaltyResult> expeditionLoyaltyResults = null,
            DefenseBattleResult defenseResult = null,
            LoyaltyResult defenseLoyaltyResult = null,
            ExpeditionStartResult expeditionStartResult = null)
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
            ExpeditionReturnResults = new List<ExpeditionReturnResult>(
                expeditionReturnResults ?? Array.Empty<ExpeditionReturnResult>());
            ExpeditionStageResults = new List<ExpeditionStageResult>(
                expeditionStageResults ?? Array.Empty<ExpeditionStageResult>());
            ExpeditionLoyaltyResults = new List<LoyaltyResult>(
                expeditionLoyaltyResults ?? Array.Empty<LoyaltyResult>());
            DefenseResult = defenseResult;
            DefenseLoyaltyResult = defenseLoyaltyResult;
            ExpeditionStartResult = expeditionStartResult;
        }

        public int TurnNumber { get; }
        public RecoveryResult RecoveryResult { get; }
        public SalaryResult SalaryResult { get; }
        public LoyaltyResult SalaryLoyaltyResult { get; }
        public LeadershipResult LeadershipResult { get; }
        public IReadOnlyList<ExpeditionReturnResult> ExpeditionReturnResults { get; }
        public IReadOnlyList<ExpeditionStageResult> ExpeditionStageResults { get; }
        public IReadOnlyList<LoyaltyResult> ExpeditionLoyaltyResults { get; }
        public DefenseBattleResult DefenseResult { get; }
        public LoyaltyResult DefenseLoyaltyResult { get; }
        public ExpeditionStartResult ExpeditionStartResult { get; }
    }
}
