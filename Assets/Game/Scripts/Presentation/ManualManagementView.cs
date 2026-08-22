using System.Collections.Generic;
using GuildFrontierSim.Application.Simulation;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using UnityEngine;
using UnityEngine.UI;

namespace GuildFrontierSim.Presentation
{
    public sealed class ManualManagementView : MonoBehaviour
    {
        [SerializeField] private GuildSimulationController controller;
        [SerializeField] private Button modeButton;
        [SerializeField] private Text modeButtonText;
        [SerializeField] private GameObject planningPanel;
        [SerializeField] private Text statusText;
        [SerializeField] private Dropdown defenseDropdown;
        [SerializeField] private Toggle delegateDefenseToggle;
        [SerializeField] private Dropdown expeditionDropdown;
        [SerializeField] private Toggle delegateExpeditionToggle;
        [SerializeField] private Dropdown actingLeaderDropdown;
        [SerializeField] private Toggle delegateActingLeaderToggle;
        [SerializeField] private Button applyButton;

        private void Start()
        {
            modeButton.onClick.AddListener(OnModeClicked);
            applyButton.onClick.AddListener(OnApplyClicked);
            controller.ManualPlanningChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (modeButton != null) modeButton.onClick.RemoveListener(OnModeClicked);
            if (applyButton != null) applyButton.onClick.RemoveListener(OnApplyClicked);
            if (controller != null) controller.ManualPlanningChanged -= Refresh;
        }

        public void Refresh()
        {
            if (controller == null || controller.Guild == null) return;
            modeButtonText.text = controller.IsManualMode ? "手動経営" : "CPU経営";
            modeButton.interactable = controller.FlowController == null ||
                (controller.FlowController.State != SimulationFlowState.ApplyingTurn &&
                 controller.FlowController.State !=
                    SimulationFlowState.WaitingForExpeditionDecision);
            bool isPlanning = controller.IsManualMode &&
                controller.FlowController != null &&
                controller.FlowController.State == SimulationFlowState.PlanningTurn;
            planningPanel.SetActive(controller.IsManualMode);
            PopulateMembers(controller.Guild);
            SetControls(isPlanning);

            if (!string.IsNullOrEmpty(controller.LastError))
                statusText.text = $"エラー: {controller.LastError}";
            else if (!isPlanning)
                statusText.text = controller.FlowController.State ==
                    SimulationFlowState.WaitingForExpeditionDecision
                    ? "遠征結果の判断待ちです。"
                    : "「次のターン」で計画を開始します。";
            else
                statusText.text = BuildRequirementText(controller.ManualRequirements);
        }

        private void OnModeClicked()
        {
            controller.SetManualMode(!controller.IsManualMode);
            Refresh();
        }

        private void OnApplyClicked()
        {
            controller.ApplyManualSelections(
                SelectedId(defenseDropdown),
                delegateDefenseToggle.isOn,
                SelectedId(expeditionDropdown),
                delegateExpeditionToggle.isOn,
                SelectedId(actingLeaderDropdown),
                delegateActingLeaderToggle.isOn);
            Refresh();
        }

        private void PopulateMembers(GuildRuntimeData guild)
        {
            var assignable = new List<string>();
            var acting = new List<string>();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                if (!CharacterAvailability.CanBeAssigned(character)) continue;
                assignable.Add(character.CharacterId);
                if (character.CharacterId != guild.LeaderCharacterId)
                    acting.Add(character.CharacterId);
            }
            ReplaceOptions(defenseDropdown, assignable);
            ReplaceOptions(expeditionDropdown, assignable);
            ReplaceOptions(actingLeaderDropdown, acting);
        }

        private void SetControls(bool isPlanning)
        {
            TurnPlanningRequirements requirements = controller.ManualRequirements;
            if (requirements == null)
            {
                defenseDropdown.interactable = false;
                delegateDefenseToggle.interactable = false;
                expeditionDropdown.interactable = false;
                delegateExpeditionToggle.interactable = false;
                actingLeaderDropdown.interactable = false;
                delegateActingLeaderToggle.interactable = false;
                applyButton.interactable = false;
                return;
            }
            defenseDropdown.interactable = isPlanning && requirements.RequiresDefense;
            delegateDefenseToggle.interactable = defenseDropdown.interactable;
            expeditionDropdown.interactable = isPlanning && requirements.RequiresExpedition;
            delegateExpeditionToggle.interactable = expeditionDropdown.interactable;
            actingLeaderDropdown.interactable = isPlanning && requirements.RequiresActingLeader;
            delegateActingLeaderToggle.interactable = actingLeaderDropdown.interactable;
            applyButton.interactable = isPlanning;
        }

        private static string BuildRequirementText(TurnPlanningRequirements requirements)
        {
            var items = new List<string>();
            if (requirements.RequiresDefense) items.Add("防衛");
            if (requirements.RequiresExpedition) items.Add("遠征");
            if (requirements.RequiresActingLeader) items.Add("代理リーダー");
            return items.Count == 0
                ? "判断項目はありません。計画を確定できます。"
                : $"入力待ち: {string.Join(" / ", items)}";
        }

        private static void ReplaceOptions(Dropdown dropdown, List<string> ids)
        {
            string selected = SelectedId(dropdown);
            dropdown.ClearOptions();
            dropdown.AddOptions(ids);
            int index = ids.IndexOf(selected);
            dropdown.value = index < 0 ? 0 : index;
            dropdown.RefreshShownValue();
        }

        private static string SelectedId(Dropdown dropdown)
        {
            return dropdown.options.Count == 0
                ? string.Empty
                : dropdown.options[dropdown.value].text;
        }
    }
}
