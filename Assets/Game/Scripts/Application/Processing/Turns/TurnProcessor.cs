using System;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Application.Processing.Leadership;
using GuildFrontierSim.Application.Processing.Recovery;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Processing.Turns
{
    public sealed class TurnProcessor
    {
        private readonly RecoveryProcessor recoveryProcessor;
        private readonly SalaryProcessor salaryProcessor;
        private readonly LoyaltyProcessor loyaltyProcessor;
        private readonly LeadershipProcessor leadershipProcessor;

        public TurnProcessor(
            RecoveryProcessor recoveryProcessor = null,
            SalaryProcessor salaryProcessor = null,
            LoyaltyProcessor loyaltyProcessor = null,
            LeadershipProcessor leadershipProcessor = null)
        {
            this.loyaltyProcessor = loyaltyProcessor ?? new LoyaltyProcessor();
            this.recoveryProcessor = recoveryProcessor ??
                new RecoveryProcessor(this.loyaltyProcessor);
            this.salaryProcessor = salaryProcessor ?? new SalaryProcessor();
            this.leadershipProcessor = leadershipProcessor ??
                new LeadershipProcessor(loyaltyProcessor: this.loyaltyProcessor);
        }

        public TurnResult Process(
            GuildRuntimeData guild,
            BattleBalanceSettings settings)
        {
            Validate(guild, settings);

            guild.AdvanceTurnNumber();
            RecoveryResult recoveryResult = recoveryProcessor.Process(guild);
            SalaryResult salaryResult = salaryProcessor.Process(guild, settings);
            LoyaltyResult salaryLoyaltyResult = loyaltyProcessor.ApplySalaryResult(
                guild,
                salaryResult,
                settings);
            LeadershipResult leadershipResult = leadershipProcessor.Process(
                guild,
                settings);

            return new TurnResult(
                guild.CurrentTurn,
                recoveryResult,
                salaryResult,
                salaryLoyaltyResult,
                leadershipResult);
        }

        private static void Validate(
            GuildRuntimeData guild,
            BattleBalanceSettings settings)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.SalaryIntervalTurns < 1)
            {
                throw new ArgumentException(
                    "Salary interval must be at least one turn.",
                    nameof(settings));
            }

            if (settings.UnpaidSalaryLoyaltyPenalty < 0)
            {
                throw new ArgumentException(
                    "Unpaid salary loyalty penalty cannot be negative.",
                    nameof(settings));
            }
        }
    }
}
