using System;
using GuildFrontierSim.Application.Factories;
using GuildFrontierSim.Application.Simulation;
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

        public GuildSimulation Simulation { get; private set; }
        public SimulationAdvanceResult LastAdvanceResult { get; private set; }
        public bool IsInitialized => Simulation != null;
        public GuildRuntimeData Guild => Simulation?.Guild;

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

        public SimulationAdvanceResult AdvanceTurnAndGetResult()
        {
            if (!IsInitialized && !TryInitialize())
            {
                return null;
            }

            try
            {
                LastAdvanceResult = Simulation.AdvanceTurn();
                for (int index = 0; index < LastAdvanceResult.Logs.Count; index++)
                {
                    SimulationLogEntry entry = LastAdvanceResult.Logs[index];
                    Debug.Log(
                        $"[Turn {entry.TurnNumber}][{entry.Category}] {entry.Message}",
                        this);
                }

                SimulationAdvanced?.Invoke(LastAdvanceResult);
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
