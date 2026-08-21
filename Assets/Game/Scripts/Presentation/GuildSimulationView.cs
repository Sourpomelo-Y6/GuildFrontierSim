using System;
using System.Text;
using GuildFrontierSim.Application.Simulation;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using UnityEngine;
using UnityEngine.UI;

namespace GuildFrontierSim.Presentation
{
    public sealed class GuildSimulationView : MonoBehaviour
    {
        [SerializeField] private GuildSimulationController controller;
        [SerializeField] private Button advanceTurnButton;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text membersText;
        [SerializeField] private Text expeditionsText;
        [SerializeField] private Text logText;
        [SerializeField, Min(1)] private int maximumLogLines = 12;

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            controller.SimulationAdvanced += OnSimulationAdvanced;
            advanceTurnButton.onClick.AddListener(OnAdvanceTurnClicked);
            if (controller.TryInitialize())
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.SimulationAdvanced -= OnSimulationAdvanced;
            }

            if (advanceTurnButton != null)
            {
                advanceTurnButton.onClick.RemoveListener(OnAdvanceTurnClicked);
            }
        }

        public void Refresh()
        {
            GuildRuntimeData guild = controller?.Guild;
            if (guild == null)
            {
                return;
            }

            summaryText.text = BuildSummary(guild);
            membersText.text = BuildMembers(guild);
            expeditionsText.text = BuildExpeditions(guild);
            logText.text = BuildLogs(controller.Simulation);
        }

        private void OnAdvanceTurnClicked()
        {
            advanceTurnButton.interactable = false;
            controller.AdvanceTurn();
            advanceTurnButton.interactable = true;
        }

        private void OnSimulationAdvanced(SimulationAdvanceResult result)
        {
            Refresh();
        }

        private bool ValidateReferences()
        {
            if (controller != null &&
                advanceTurnButton != null &&
                summaryText != null &&
                membersText != null &&
                expeditionsText != null &&
                logText != null)
            {
                return true;
            }

            Debug.LogError("GuildSimulationView requires all UI references.", this);
            return false;
        }

        private static string BuildSummary(GuildRuntimeData guild)
        {
            string actingLeader = string.IsNullOrEmpty(guild.ActingLeaderCharacterId)
                ? "なし"
                : guild.ActingLeaderCharacterId;
            return $"ターン: {guild.CurrentTurn}    資金: {guild.Funds}\n" +
                   $"ギルド長: {guild.LeaderCharacterId}    代理: {actingLeader}";
        }

        private static string BuildMembers(GuildRuntimeData guild)
        {
            var builder = new StringBuilder("メンバー\n");
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                builder.Append(character.CharacterId)
                    .Append("  Lv.").Append(character.Level)
                    .Append("  HP ").Append(character.CurrentHp)
                    .Append('/').Append(character.MaxHp)
                    .Append("  忠誠 ").Append(character.Loyalty)
                    .Append("  ").Append(character.Status);
                if (character.IsDeparturePending)
                {
                    builder.Append("  [離脱予定]");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildExpeditions(GuildRuntimeData guild)
        {
            var builder = new StringBuilder("遠征\n");
            if (guild.Expeditions.Count == 0)
            {
                builder.Append("進行中の遠征はありません。");
                return builder.ToString();
            }

            for (int index = 0; index < guild.Expeditions.Count; index++)
            {
                ExpeditionRuntimeData expedition = guild.Expeditions[index];
                builder.Append(expedition.ExpeditionId)
                    .Append("  ").Append(expedition.AreaId)
                    .Append("  Stage ").Append(expedition.CurrentStage)
                    .Append('/').Append(expedition.MaximumStages)
                    .Append("  ").Append(expedition.Status)
                    .Append("  戦利品 ").Append(expedition.TemporaryFunds)
                    .AppendLine();
            }

            return builder.ToString();
        }

        private string BuildLogs(GuildSimulation simulation)
        {
            var builder = new StringBuilder("ログ\n");
            int firstIndex = Math.Max(0, simulation.LogHistory.Count - maximumLogLines);
            for (int index = firstIndex; index < simulation.LogHistory.Count; index++)
            {
                SimulationLogEntry entry = simulation.LogHistory[index];
                builder.Append("T").Append(entry.TurnNumber)
                    .Append(" [").Append(entry.Category).Append("] ")
                    .AppendLine(entry.Message);
            }

            if (simulation.LogHistory.Count == 0)
            {
                builder.Append("「次のターン」を押して開始してください。");
            }

            return builder.ToString();
        }
    }
}
