using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Application.Processing.Leadership;
using GuildFrontierSim.Application.Processing.Recovery;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Infrastructure.Random;

namespace GuildFrontierSim.Application.Processing.Turns
{
    public sealed class TurnProcessor
    {
        private readonly RecoveryProcessor recoveryProcessor;
        private readonly SalaryProcessor salaryProcessor;
        private readonly LoyaltyProcessor loyaltyProcessor;
        private readonly LeadershipProcessor leadershipProcessor;
        private readonly DefenseBattleProcessor defenseProcessor;
        private readonly ExpeditionProcessor expeditionProcessor;
        private readonly ExpeditionStageProcessor expeditionStageProcessor;

        public TurnProcessor(
            RecoveryProcessor recoveryProcessor = null,
            SalaryProcessor salaryProcessor = null,
            LoyaltyProcessor loyaltyProcessor = null,
            LeadershipProcessor leadershipProcessor = null,
            IRandomSource randomSource = null,
            DefenseBattleProcessor defenseProcessor = null,
            ExpeditionProcessor expeditionProcessor = null,
            ExpeditionStageProcessor expeditionStageProcessor = null)
        {
            this.loyaltyProcessor = loyaltyProcessor ?? new LoyaltyProcessor();
            this.recoveryProcessor = recoveryProcessor ??
                new RecoveryProcessor(this.loyaltyProcessor);
            this.salaryProcessor = salaryProcessor ?? new SalaryProcessor();
            this.leadershipProcessor = leadershipProcessor ??
                new LeadershipProcessor(loyaltyProcessor: this.loyaltyProcessor);
            this.defenseProcessor = defenseProcessor ??
                (randomSource == null ? null : new DefenseBattleProcessor(randomSource));
            this.expeditionProcessor = expeditionProcessor ?? new ExpeditionProcessor();
            this.expeditionStageProcessor = expeditionStageProcessor ??
                (randomSource == null ? null : new ExpeditionStageProcessor(randomSource));
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

        public TurnResult Process(
            GuildRuntimeData guild,
            TurnRequest request,
            CpuSelectionSettings selectionSettings,
            BattleBalanceSettings battleSettings,
            ExpeditionBalanceSettings expeditionSettings)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Validate(guild, battleSettings);
            if (selectionSettings == null)
            {
                throw new ArgumentNullException(nameof(selectionSettings));
            }

            if (expeditionSettings == null)
            {
                throw new ArgumentNullException(nameof(expeditionSettings));
            }

            EnsureConfigured(request, guild);

            guild.AdvanceTurnNumber();
            RecoveryResult recoveryResult = recoveryProcessor.Process(guild);
            var returnResults = new List<ExpeditionReturnResult>();
            var stageResults = new List<ExpeditionStageResult>();
            var expeditionLoyaltyResults = new List<LoyaltyResult>();
            ProcessExistingExpeditions(
                guild,
                battleSettings,
                expeditionSettings,
                returnResults,
                stageResults,
                expeditionLoyaltyResults);

            DefenseBattleResult defenseResult = null;
            LoyaltyResult defenseLoyaltyResult = null;
            if (request.DefenseRequest != null || request.DefenseAssignment != null)
            {
                defenseResult = request.DefenseAssignment == null
                    ? defenseProcessor.Process(
                        guild, request.DefenseRequest, selectionSettings, battleSettings)
                    : defenseProcessor.Process(
                        guild, request.DefenseAssignment, selectionSettings, battleSettings);
                if (defenseResult.BattleResult != null)
                {
                    defenseLoyaltyResult = loyaltyProcessor.ApplyBattleResult(
                        guild,
                        defenseResult.DefenderIds,
                        defenseResult.BattleResult.Outcome,
                        battleSettings);
                }
            }

            ExpeditionStartResult startResult = StartRequestedExpedition(
                guild,
                request,
                defenseResult,
                selectionSettings,
                battleSettings);
            SalaryResult salaryResult = salaryProcessor.Process(guild, battleSettings);
            LoyaltyResult salaryLoyaltyResult = loyaltyProcessor.ApplySalaryResult(
                guild,
                salaryResult,
                battleSettings);
            LeadershipResult leadershipResult = request.ActingLeaderAssignment == null
                ? leadershipProcessor.Process(guild, battleSettings)
                : leadershipProcessor.Process(
                    guild,
                    battleSettings,
                    request.ActingLeaderAssignment,
                    guild.Revision);

            return new TurnResult(
                guild.CurrentTurn,
                recoveryResult,
                salaryResult,
                salaryLoyaltyResult,
                leadershipResult,
                returnResults,
                stageResults,
                expeditionLoyaltyResults,
                defenseResult,
                defenseLoyaltyResult,
                startResult);
        }

        private void ProcessExistingExpeditions(
            GuildRuntimeData guild,
            BattleBalanceSettings battleSettings,
            ExpeditionBalanceSettings expeditionSettings,
            ICollection<ExpeditionReturnResult> returnResults,
            ICollection<ExpeditionStageResult> stageResults,
            ICollection<LoyaltyResult> loyaltyResults)
        {
            var expeditions = new List<ExpeditionRuntimeData>(guild.Expeditions);
            for (int index = 0; index < expeditions.Count; index++)
            {
                ExpeditionRuntimeData expedition = expeditions[index];
                if (expedition.Status == ExpeditionStatus.Returning)
                {
                    returnResults.Add(expeditionProcessor.CompleteReturn(
                        guild,
                        expedition.ExpeditionId));
                }
                else if (expedition.Status == ExpeditionStatus.Active)
                {
                    ExpeditionStageResult stageResult =
                        expeditionStageProcessor.ProcessStage(
                            guild,
                            expedition.ExpeditionId,
                            battleSettings,
                            expeditionSettings);
                    stageResults.Add(stageResult);
                    loyaltyResults.Add(loyaltyProcessor.ApplyBattleResult(
                        guild,
                        expedition.ParticipantIds,
                        stageResult.BattleResult.Outcome,
                        battleSettings));
                }
            }
        }

        private ExpeditionStartResult StartRequestedExpedition(
            GuildRuntimeData guild,
            TurnRequest turnRequest,
            DefenseBattleResult defenseResult,
            CpuSelectionSettings selectionSettings,
            BattleBalanceSettings battleSettings)
        {
            ExpeditionStartRequest request = turnRequest.ExpeditionStartRequest;
            if (request == null && turnRequest.ExpeditionAssignment == null)
            {
                return null;
            }

            var excludedIds = request == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(request.ExcludedCharacterIds, StringComparer.Ordinal);
            if (defenseResult != null)
            {
                for (int index = 0; index < defenseResult.DefenderIds.Count; index++)
                {
                    excludedIds.Add(defenseResult.DefenderIds[index]);
                }
            }

            return turnRequest.ExpeditionAssignment == null
                ? expeditionProcessor.Start(
                    guild,
                    new ExpeditionStartRequest(
                        request.ExpeditionId,
                        request.Area,
                        excludedIds),
                    selectionSettings,
                    battleSettings)
                : expeditionProcessor.Start(
                    guild,
                    turnRequest.ExpeditionAssignment,
                    turnRequest.ExpeditionAreaRegistry,
                    selectionSettings,
                    excludedIds);
        }

        private void EnsureConfigured(TurnRequest request, GuildRuntimeData guild)
        {
            if ((request.DefenseRequest != null || request.DefenseAssignment != null) &&
                defenseProcessor == null)
            {
                throw new InvalidOperationException(
                    "A random source is required for defense processing.");
            }

            bool hasActiveExpedition = false;
            for (int index = 0; index < guild.Expeditions.Count; index++)
            {
                if (guild.Expeditions[index].Status == ExpeditionStatus.Active)
                {
                    hasActiveExpedition = true;
                    break;
                }
            }

            if (hasActiveExpedition && expeditionStageProcessor == null)
            {
                throw new InvalidOperationException(
                    "A random source is required for expedition stage processing.");
            }
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
