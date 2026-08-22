using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Factories;
using GuildFrontierSim.Application.Simulation;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Planning;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Presets;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Infrastructure.Random;
using UnityEngine;

namespace GuildFrontierSim.Presentation
{
    public sealed class GuildSimulationController : MonoBehaviour
    {
        [Header("Initial Data")]
        [SerializeField] private GuildStartingPreset guildStartingPreset;
        [SerializeField] private ExpeditionAreaDefinition expeditionArea;

        [Header("Settings")]
        [SerializeField] private BattleBalanceSettings battleSettings;
        [SerializeField] private CpuSelectionSettings cpuSelectionSettings;
        [SerializeField] private ExpeditionBalanceSettings expeditionSettings;
        [SerializeField] private GuildSimulationSettings simulationSettings;

        public event Action<SimulationAdvanceResult> SimulationAdvanced;
        public event Action ManualPlanningChanged;

        public GuildSimulation Simulation { get; private set; }
        public SimulationAdvanceResult LastAdvanceResult { get; private set; }
        public bool IsInitialized => Simulation != null;
        public GuildRuntimeData Guild => Simulation?.Guild;
        public bool IsManualMode { get; private set; }
        public SimulationFlowController FlowController { get; private set; }
        public TurnPlanningRequirements ManualRequirements { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public int DesiredDefenseMembers => cpuSelectionSettings.DesiredDefenseMembers;
        public int DesiredExpeditionMembers => cpuSelectionSettings.DesiredExpeditionMembers;
        private TurnPlan pendingTurnPlan;

        private void Start()
        {
            TryInitialize();
        }

        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (!TryGetConfigurationError(out string error))
            {
                Debug.LogError(error, this);
                return false;
            }

            try
            {
                GuildRuntimeData guild =
                    new GuildRuntimeDataFactory().Create(guildStartingPreset);
                Simulation = new GuildSimulation(
                    guild,
                    battleSettings,
                    cpuSelectionSettings,
                    expeditionSettings,
                    simulationSettings,
                    expeditionArea,
                    new UnityRandomSource());
                CreateFlowController();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Guild simulation initialization failed: {exception.Message}",
                    this);
                return false;
            }
        }

        public void AdvanceTurn()
        {
            AdvanceTurnAndGetResult();
        }

        public bool ResetSimulation()
        {
            Simulation = null;
            LastAdvanceResult = null;
            FlowController = null;
            ManualRequirements = null;
            LastError = string.Empty;
            pendingTurnPlan = null;
            return TryInitialize();
        }

        public void SetManualMode(bool enabled)
        {
            if (!IsInitialized && !TryInitialize()) return;
            IsManualMode = enabled;
            CreateFlowController();
            ManualRequirements = null;
            LastError = string.Empty;
            ManualPlanningChanged?.Invoke();
        }

        public bool BeginManualPlanning()
        {
            if (!IsManualMode || (!IsInitialized && !TryInitialize())) return false;
            try
            {
                ManualRequirements = Simulation.GetNextTurnRequirements();
                FlowController.BeginTurnPlanning(
                    ManualRequirements.RequiresDefense,
                    ManualRequirements.RequiresExpedition,
                    ManualRequirements.RequiresActingLeader);
                LastError = string.Empty;
                ManualPlanningChanged?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                ManualPlanningChanged?.Invoke();
                return false;
            }
        }

        public bool ApplyManualSelections(
            string defenseCharacterId,
            bool delegateDefense,
            string expeditionCharacterId,
            bool delegateExpedition,
            string actingLeaderCharacterId,
            bool delegateActingLeader)
        {
            return ApplyManualSelections(
                string.IsNullOrWhiteSpace(defenseCharacterId)
                    ? Array.Empty<string>()
                    : new[] { defenseCharacterId },
                delegateDefense,
                string.IsNullOrWhiteSpace(expeditionCharacterId)
                    ? Array.Empty<string>()
                    : new[] { expeditionCharacterId },
                delegateExpedition,
                actingLeaderCharacterId,
                delegateActingLeader);
        }

        public bool ApplyManualSelections(
            IEnumerable<string> defenseCharacterIds,
            bool delegateDefense,
            IEnumerable<string> expeditionCharacterIds,
            bool delegateExpedition,
            string actingLeaderCharacterId,
            bool delegateActingLeader)
        {
            if (FlowController == null ||
                FlowController.State != SimulationFlowState.PlanningTurn)
                return false;
            try
            {
                TurnPlanningSession session = FlowController.PlanningSession;
                int revision = Guild.Revision;
                if (session.IsRequired(TurnDecisionType.DefenseMembers) &&
                    !session.IsResolved(TurnDecisionType.DefenseMembers))
                {
                    if (delegateDefense)
                        FlowController.DelegateToCpu(TurnDecisionType.DefenseMembers, revision);
                    else
                        FlowController.SubmitDefense(
                            new DefenseAssignment(
                                simulationSettings.DefenseEnemyBasePower,
                                defenseCharacterIds),
                            revision);
                }

                if (session.IsRequired(TurnDecisionType.ExpeditionMembers) &&
                    !session.IsResolved(TurnDecisionType.ExpeditionMembers))
                {
                    if (delegateExpedition)
                        FlowController.DelegateToCpu(TurnDecisionType.ExpeditionMembers, revision);
                    else
                        FlowController.SubmitExpedition(
                            new ExpeditionAssignment(
                                $"expedition-{session.TargetTurn}",
                                expeditionArea.Id,
                                expeditionCharacterIds),
                            revision);
                }

                if (session.IsRequired(TurnDecisionType.ActingLeader) &&
                    !session.IsResolved(TurnDecisionType.ActingLeader))
                {
                    if (delegateActingLeader)
                        FlowController.DelegateToCpu(TurnDecisionType.ActingLeader, revision);
                    else
                        FlowController.SubmitActingLeader(actingLeaderCharacterId, revision);
                }

                FlowController.ApplyTurnPlan(plan =>
                {
                    ExpeditionStageResolution resolution =
                        Simulation.ResolveManualExpeditionStage();
                    if (resolution == null)
                    {
                        CompletePlannedTurn(Simulation.AdvanceTurn(plan));
                        return null;
                    }
                    if (!resolution.IsWaitingForDecision)
                    {
                        CompletePlannedTurn(
                            Simulation.ResumeTurnAfterManualExpedition(
                                plan, resolution.Result));
                        return null;
                    }

                    pendingTurnPlan = plan;
                    return resolution.PendingDecision;
                });
                LastError = string.Empty;
                ManualRequirements = null;
                ManualPlanningChanged?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                ManualPlanningChanged?.Invoke();
                return false;
            }
        }

        public bool CancelManualPlanning()
        {
            if (FlowController == null ||
                FlowController.State != SimulationFlowState.PlanningTurn)
                return false;
            FlowController.CancelTurnPlanning();
            ManualRequirements = null;
            LastError = string.Empty;
            ManualPlanningChanged?.Invoke();
            return true;
        }

        public bool ApplyManualExpeditionDecision(ExpeditionDecision decision)
        {
            if (FlowController == null ||
                FlowController.State != SimulationFlowState.WaitingForExpeditionDecision ||
                pendingTurnPlan == null)
            {
                return false;
            }

            try
            {
                FlowController.SubmitExpeditionDecision(
                    decision,
                    (pending, selectedDecision) =>
                    {
                        ExpeditionStageResult stageResult =
                            Simulation.ApplyManualExpeditionDecision(
                                pending, selectedDecision);
                        CompletePlannedTurn(
                            Simulation.ResumeTurnAfterManualExpedition(
                                pendingTurnPlan, stageResult));
                    });
                pendingTurnPlan = null;
                ManualRequirements = null;
                LastError = string.Empty;
                ManualPlanningChanged?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                ManualPlanningChanged?.Invoke();
                return false;
            }
        }

        public SimulationAdvanceResult AdvanceTurnAndGetResult()
        {
            if (!IsInitialized && !TryInitialize())
            {
                return null;
            }

            try
            {
                LastAdvanceResult = Simulation.AdvanceTurn();
                PublishAdvanceResult();
                return LastAdvanceResult;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Guild simulation turn failed: {exception.Message}",
                    this);
                return null;
            }
        }

        private void CreateFlowController()
        {
            GuildControlPolicy policy = IsManualMode
                ? new GuildControlPolicy(GuildControlMode.Player, Guild.LeaderCharacterId)
                : new GuildControlPolicy(GuildControlMode.Cpu);
            FlowController = new SimulationFlowController(Guild, policy);
        }

        private void PublishAdvanceResult()
        {
            for (int index = 0; index < LastAdvanceResult.Logs.Count; index++)
            {
                SimulationLogEntry entry = LastAdvanceResult.Logs[index];
                Debug.Log($"[Turn {entry.TurnNumber}][{entry.Category}] {entry.Message}", this);
            }
            SimulationAdvanced?.Invoke(LastAdvanceResult);
        }

        private void CompletePlannedTurn(SimulationAdvanceResult result)
        {
            LastAdvanceResult = result;
            PublishAdvanceResult();
        }

        private bool TryGetConfigurationError(out string error)
        {
            if (guildStartingPreset == null)
            {
                error = "GuildSimulationController requires a GuildStartingPreset.";
                return false;
            }

            if (expeditionArea == null)
            {
                error = "GuildSimulationController requires an ExpeditionAreaDefinition.";
                return false;
            }

            if (battleSettings == null ||
                cpuSelectionSettings == null ||
                expeditionSettings == null ||
                simulationSettings == null)
            {
                error = "GuildSimulationController requires all simulation settings.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
