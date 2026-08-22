using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Planning;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Application.Processing.Leadership;
using GuildFrontierSim.Application.Processing.Turns;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Infrastructure.Random;

namespace GuildFrontierSim.Application.Simulation
{
    public sealed class GuildSimulation
    {
        private readonly BattleBalanceSettings battleSettings;
        private readonly CpuSelectionSettings selectionSettings;
        private readonly ExpeditionBalanceSettings expeditionSettings;
        private readonly GuildSimulationSettings simulationSettings;
        private readonly ExpeditionAreaDefinition expeditionArea;
        private readonly TurnProcessor turnProcessor;
        private readonly List<SimulationLogEntry> logHistory =
            new List<SimulationLogEntry>();

        public GuildSimulation(
            GuildRuntimeData guild,
            BattleBalanceSettings battleSettings,
            CpuSelectionSettings selectionSettings,
            ExpeditionBalanceSettings expeditionSettings,
            GuildSimulationSettings simulationSettings,
            ExpeditionAreaDefinition expeditionArea,
            IRandomSource randomSource,
            TurnProcessor turnProcessor = null)
        {
            Guild = guild ?? throw new ArgumentNullException(nameof(guild));
            this.battleSettings = battleSettings ??
                throw new ArgumentNullException(nameof(battleSettings));
            this.selectionSettings = selectionSettings ??
                throw new ArgumentNullException(nameof(selectionSettings));
            this.expeditionSettings = expeditionSettings ??
                throw new ArgumentNullException(nameof(expeditionSettings));
            this.simulationSettings = simulationSettings ??
                throw new ArgumentNullException(nameof(simulationSettings));
            this.expeditionArea = expeditionArea ??
                throw new ArgumentNullException(nameof(expeditionArea));
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            ValidateSettings(simulationSettings, expeditionArea);
            this.turnProcessor = turnProcessor ??
                new TurnProcessor(randomSource: randomSource);
        }

        public GuildRuntimeData Guild { get; }
        public IReadOnlyList<SimulationLogEntry> LogHistory => logHistory;

        public SimulationAdvanceResult AdvanceTurn()
        {
            return AdvanceTurnInternal(null, false, null);
        }

        public SimulationAdvanceResult AdvanceTurn(TurnPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.TargetTurn != checked(Guild.CurrentTurn + 1) ||
                plan.GuildRevision != Guild.Revision)
            {
                throw new InvalidOperationException("The turn plan is stale.");
            }
            return AdvanceTurnInternal(plan, false, null);
        }

        public TurnPlanningRequirements GetNextTurnRequirements()
        {
            int nextTurn = checked(Guild.CurrentTurn + 1);
            bool defense = nextTurn % simulationSettings.DefenseIntervalTurns == 0;
            bool expedition = simulationSettings.AutomaticallyStartExpeditions &&
                nextTurn % simulationSettings.ExpeditionIntervalTurns == 0 &&
                !HasOngoingExpedition();
            bool actingLeader = !Guild.Leader.IsDeparturePending &&
                !CharacterAvailability.CanBeAssigned(Guild.Leader);
            return new TurnPlanningRequirements(defense, expedition, actingLeader);
        }

        public ExpeditionStageResolution ResolveManualExpeditionStage()
        {
            for (int index = 0; index < Guild.Expeditions.Count; index++)
            {
                ExpeditionRuntimeData expedition = Guild.Expeditions[index];
                if (expedition.Status == ExpeditionStatus.Active)
                {
                    return turnProcessor.ResolveExpeditionStage(
                        Guild,
                        expedition.ExpeditionId,
                        battleSettings,
                        expeditionSettings);
                }
            }
            return null;
        }

        public ExpeditionStageResult ApplyManualExpeditionDecision(
            PendingExpeditionDecision pending,
            ExpeditionDecision decision)
        {
            return turnProcessor.ApplyExpeditionDecision(
                Guild, pending, decision, expeditionSettings);
        }

        public SimulationAdvanceResult ResumeTurnAfterManualExpedition(
            TurnPlan plan,
            ExpeditionStageResult stageResult)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (stageResult == null) throw new ArgumentNullException(nameof(stageResult));
            if (plan.TargetTurn != checked(Guild.CurrentTurn + 1))
                throw new InvalidOperationException("The target turn is stale.");
            return AdvanceTurnInternal(plan, true, stageResult);
        }

        private SimulationAdvanceResult AdvanceTurnInternal(
            TurnPlan plan,
            bool skipExistingExpeditions,
            ExpeditionStageResult preprocessedStageResult)
        {
            int nextTurn = checked(Guild.CurrentTurn + 1);
            TurnRequest request = CreateTurnRequest(
                nextTurn,
                plan,
                skipExistingExpeditions,
                preprocessedStageResult);
            TurnResult turnResult = turnProcessor.Process(
                Guild,
                request,
                selectionSettings,
                battleSettings,
                expeditionSettings);
            List<SimulationLogEntry> logs = CreateLogs(turnResult);
            logHistory.AddRange(logs);
            return new SimulationAdvanceResult(turnResult, logs);
        }

        private TurnRequest CreateTurnRequest(
            int nextTurn,
            TurnPlan plan = null,
            bool skipExistingExpeditions = false,
            ExpeditionStageResult preprocessedStageResult = null)
        {
            DefenseBattleRequest defenseRequest =
                nextTurn % simulationSettings.DefenseIntervalTurns == 0
                    ? new DefenseBattleRequest(simulationSettings.DefenseEnemyBasePower)
                    : null;
            ExpeditionStartRequest expeditionRequest = null;
            if (simulationSettings.AutomaticallyStartExpeditions &&
                nextTurn % simulationSettings.ExpeditionIntervalTurns == 0 &&
                !HasOngoingExpedition())
            {
                expeditionRequest = new ExpeditionStartRequest(
                    $"expedition-{nextTurn}",
                    expeditionArea);
            }

            bool manualDefense = defenseRequest != null && plan != null &&
                !plan.IsDelegatedToCpu(TurnDecisionType.DefenseMembers);
            bool manualExpedition = expeditionRequest != null && plan != null &&
                !plan.IsDelegatedToCpu(TurnDecisionType.ExpeditionMembers) &&
                !plan.IsDelegatedToCpu(TurnDecisionType.ExpeditionArea);
            return new TurnRequest(
                manualDefense ? null : defenseRequest,
                manualExpedition ? null : expeditionRequest,
                manualDefense ? plan.DefenseAssignment : null,
                manualExpedition ? plan.ExpeditionAssignment : null,
                manualExpedition
                    ? new ExpeditionAreaRegistry(new[] { expeditionArea })
                    : null,
                plan != null && !plan.IsDelegatedToCpu(TurnDecisionType.ActingLeader)
                    ? plan.ActingLeaderAssignment
                    : null,
                skipExistingExpeditions,
                preprocessedStageResult == null
                    ? null
                    : new[] { preprocessedStageResult });
        }

        private bool HasOngoingExpedition()
        {
            for (int index = 0; index < Guild.Expeditions.Count; index++)
            {
                ExpeditionStatus status = Guild.Expeditions[index].Status;
                if (status == ExpeditionStatus.Active ||
                    status == ExpeditionStatus.AwaitingDecision ||
                    status == ExpeditionStatus.Returning)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<SimulationLogEntry> CreateLogs(TurnResult result)
        {
            var logs = new List<SimulationLogEntry>
            {
                new SimulationLogEntry(
                    result.TurnNumber,
                    SimulationLogCategory.Turn,
                    $"ターン {result.TurnNumber} を開始しました。")
            };

            AddRecoveryLogs(result, logs);
            AddExpeditionLogs(result, logs);
            AddDefenseLogs(result, logs);
            AddEconomyLogs(result, logs);
            AddLoyaltyLogs(result, logs);
            AddLeadershipLogs(result, logs);
            return logs;
        }

        private static void AddRecoveryLogs(
            TurnResult result,
            ICollection<SimulationLogEntry> logs)
        {
            if (result.RecoveryResult.RecoveredCharacterIds.Count > 0)
            {
                logs.Add(new SimulationLogEntry(
                    result.TurnNumber,
                    SimulationLogCategory.Recovery,
                    "メンバーが活動可能な状態へ回復しました。",
                    result.RecoveryResult.RecoveredCharacterIds));
            }
        }

        private static void AddExpeditionLogs(
            TurnResult result,
            ICollection<SimulationLogEntry> logs)
        {
            for (int index = 0; index < result.ExpeditionStageResults.Count; index++)
            {
                ExpeditionStageResult stage = result.ExpeditionStageResults[index];
                logs.Add(new SimulationLogEntry(
                    result.TurnNumber,
                    SimulationLogCategory.Expedition,
                    $"遠征 {stage.ExpeditionId} のステージ結果: {stage.Outcome}"));
            }

            for (int index = 0; index < result.ExpeditionReturnResults.Count; index++)
            {
                ExpeditionReturnResult returned = result.ExpeditionReturnResults[index];
                logs.Add(new SimulationLogEntry(
                    result.TurnNumber,
                    SimulationLogCategory.Expedition,
                    $"遠征 {returned.ExpeditionId} が帰還し、資金 {returned.TransferredFunds} を獲得しました。",
                    returned.RescuedCharacterIds));
            }

            if (result.ExpeditionStartResult != null)
            {
                logs.Add(new SimulationLogEntry(
                    result.TurnNumber,
                    SimulationLogCategory.Expedition,
                    result.ExpeditionStartResult.Outcome == ExpeditionStartOutcome.Started
                        ? $"遠征 {result.ExpeditionStartResult.Expedition.ExpeditionId} を開始しました。"
                        : "遠征を開始できるメンバーがいませんでした。",
                    result.ExpeditionStartResult.ParticipantIds));
            }
        }

        private static void AddDefenseLogs(
            TurnResult result,
            ICollection<SimulationLogEntry> logs)
        {
            if (result.DefenseResult == null)
            {
                return;
            }

            logs.Add(new SimulationLogEntry(
                result.TurnNumber,
                SimulationLogCategory.Defense,
                $"防衛結果: {result.DefenseResult.Outcome}、報酬 {result.DefenseResult.Reward}",
                result.DefenseResult.DefenderIds));
        }

        private static void AddEconomyLogs(
            TurnResult result,
            ICollection<SimulationLogEntry> logs)
        {
            if (result.SalaryResult.Outcome == SalaryOutcome.NotDue)
            {
                return;
            }

            logs.Add(new SimulationLogEntry(
                result.TurnNumber,
                SimulationLogCategory.Economy,
                result.SalaryResult.Outcome == SalaryOutcome.Paid
                    ? $"給料 {result.SalaryResult.TotalSalary} を支払いました。"
                    : $"資金不足により給料 {result.SalaryResult.TotalSalary} は未払いです。",
                result.SalaryResult.CharacterIds));
        }

        private static void AddLeadershipLogs(
            TurnResult result,
            ICollection<SimulationLogEntry> logs)
        {
            if (result.LeadershipResult.Outcome == LeadershipOutcome.LeaderContinues)
            {
                return;
            }

            var relatedIds = new List<string>();
            if (!string.IsNullOrEmpty(result.LeadershipResult.LeaderId))
            {
                relatedIds.Add(result.LeadershipResult.LeaderId);
            }

            if (!string.IsNullOrEmpty(result.LeadershipResult.ActingLeaderId))
            {
                relatedIds.Add(result.LeadershipResult.ActingLeaderId);
            }

            logs.Add(new SimulationLogEntry(
                result.TurnNumber,
                SimulationLogCategory.Leadership,
                $"リーダー判定: {result.LeadershipResult.Outcome}",
                relatedIds));
        }

        private static void AddLoyaltyLogs(
            TurnResult result,
            ICollection<SimulationLogEntry> logs)
        {
            AddLoyaltyLog(result.TurnNumber, result.DefenseLoyaltyResult, logs);
            for (int index = 0; index < result.ExpeditionLoyaltyResults.Count; index++)
            {
                AddLoyaltyLog(
                    result.TurnNumber,
                    result.ExpeditionLoyaltyResults[index],
                    logs);
            }

            AddLoyaltyLog(result.TurnNumber, result.SalaryLoyaltyResult, logs);
        }

        private static void AddLoyaltyLog(
            int turnNumber,
            LoyaltyResult loyaltyResult,
            ICollection<SimulationLogEntry> logs)
        {
            if (loyaltyResult == null ||
                (loyaltyResult.LoyaltyChange == 0 &&
                 loyaltyResult.DepartedCharacterIds.Count == 0 &&
                 loyaltyResult.PendingDepartureCharacterIds.Count == 0))
            {
                return;
            }

            logs.Add(new SimulationLogEntry(
                turnNumber,
                SimulationLogCategory.Loyalty,
                $"忠誠度変化: {loyaltyResult.LoyaltyChange}",
                loyaltyResult.AffectedCharacterIds));
            if (loyaltyResult.DepartedCharacterIds.Count > 0)
            {
                logs.Add(new SimulationLogEntry(
                    turnNumber,
                    SimulationLogCategory.Loyalty,
                    "メンバーがギルドを離脱しました。",
                    loyaltyResult.DepartedCharacterIds));
            }

            if (loyaltyResult.PendingDepartureCharacterIds.Count > 0)
            {
                logs.Add(new SimulationLogEntry(
                    turnNumber,
                    SimulationLogCategory.Loyalty,
                    "メンバーの離脱が予約されました。",
                    loyaltyResult.PendingDepartureCharacterIds));
            }
        }

        private static void ValidateSettings(
            GuildSimulationSettings settings,
            ExpeditionAreaDefinition area)
        {
            if (settings.DefenseIntervalTurns < 1 ||
                settings.ExpeditionIntervalTurns < 1 ||
                settings.DefenseEnemyBasePower < 0f ||
                float.IsNaN(settings.DefenseEnemyBasePower) ||
                float.IsInfinity(settings.DefenseEnemyBasePower))
            {
                throw new ArgumentException("Simulation settings are invalid.", nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(area.Id))
            {
                throw new ArgumentException("Expedition area is invalid.", nameof(area));
            }
        }
    }
}
