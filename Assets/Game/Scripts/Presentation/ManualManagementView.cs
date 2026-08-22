using System;
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
        [SerializeField] private RectTransform defenseMemberContainer;
        [SerializeField] private Text defenseCountText;
        [SerializeField] private Toggle delegateDefenseToggle;
        [SerializeField] private RectTransform expeditionMemberContainer;
        [SerializeField] private Text expeditionCountText;
        [SerializeField] private Toggle delegateExpeditionToggle;
        [SerializeField] private Dropdown actingLeaderDropdown;
        [SerializeField] private Toggle delegateActingLeaderToggle;
        [SerializeField] private Text planSummaryText;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button delegateAllButton;

        private readonly Dictionary<string, Toggle> defenseToggles = new Dictionary<string, Toggle>(StringComparer.Ordinal);
        private readonly Dictionary<string, Toggle> expeditionToggles = new Dictionary<string, Toggle>(StringComparer.Ordinal);
        private bool changingSelection;

        private void Start()
        {
            modeButton.onClick.AddListener(OnModeClicked);
            applyButton.onClick.AddListener(OnApplyClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
            delegateAllButton.onClick.AddListener(OnDelegateAllClicked);
            delegateDefenseToggle.onValueChanged.AddListener(_ => UpdateDisplay());
            delegateExpeditionToggle.onValueChanged.AddListener(_ => UpdateDisplay());
            controller.ManualPlanningChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (controller != null) controller.ManualPlanningChanged -= Refresh;
        }

        public void Refresh()
        {
            if (controller == null || controller.Guild == null) return;
            modeButtonText.text = controller.IsManualMode ? "手動経営" : "CPU経営";
            SimulationFlowState state = controller.FlowController?.State ?? SimulationFlowState.Ready;
            modeButton.interactable = state == SimulationFlowState.Ready;
            bool isPlanning = controller.IsManualMode && state == SimulationFlowState.PlanningTurn;
            planningPanel.SetActive(isPlanning);
            if (!isPlanning) return;

            BuildMemberToggles(controller.Guild);
            PopulateActingLeaders(controller.Guild);
            delegateDefenseToggle.interactable = controller.ManualRequirements.RequiresDefense;
            delegateExpeditionToggle.interactable = controller.ManualRequirements.RequiresExpedition;
            actingLeaderDropdown.interactable = controller.ManualRequirements.RequiresActingLeader;
            delegateActingLeaderToggle.interactable = actingLeaderDropdown.interactable;
            statusText.text = string.IsNullOrEmpty(controller.LastError)
                ? "メンバーを複数選択し、内容を確認してターンを実行してください。"
                : $"エラー: {controller.LastError}";
            UpdateDisplay();
        }

        private void OnModeClicked()
        {
            controller.SetManualMode(!controller.IsManualMode);
            Refresh();
        }

        private void OnApplyClicked()
        {
            controller.ApplyManualSelections(
                SelectedIds(defenseToggles), delegateDefenseToggle.isOn,
                SelectedIds(expeditionToggles), delegateExpeditionToggle.isOn,
                SelectedId(actingLeaderDropdown), delegateActingLeaderToggle.isOn);
            Refresh();
        }

        private void OnCancelClicked()
        {
            controller.CancelManualPlanning();
            Refresh();
        }

        private void OnDelegateAllClicked()
        {
            controller.ApplyManualSelections(
                Array.Empty<string>(), true,
                Array.Empty<string>(), true,
                string.Empty, true);
            Refresh();
        }

        private void BuildMemberToggles(GuildRuntimeData guild)
        {
            var selectedDefense = new HashSet<string>(SelectedIds(defenseToggles), StringComparer.Ordinal);
            var selectedExpedition = new HashSet<string>(SelectedIds(expeditionToggles), StringComparer.Ordinal);
            ClearContainer(defenseMemberContainer);
            ClearContainer(expeditionMemberContainer);
            defenseToggles.Clear();
            expeditionToggles.Clear();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                defenseToggles.Add(character.CharacterId,
                    CreateMemberToggle(defenseMemberContainer, character, true));
                expeditionToggles.Add(character.CharacterId,
                    CreateMemberToggle(expeditionMemberContainer, character, false));
            }
            changingSelection = true;
            foreach (string id in selectedDefense)
                if (defenseToggles.TryGetValue(id, out Toggle defenseToggle)) defenseToggle.isOn = true;
            foreach (string id in selectedExpedition)
                if (expeditionToggles.TryGetValue(id, out Toggle expeditionToggle)) expeditionToggle.isOn = true;
            changingSelection = false;
        }

        private Toggle CreateMemberToggle(RectTransform parent, CharacterRuntimeData character, bool defense)
        {
            GameObject row = new GameObject(character.CharacterId, typeof(RectTransform), typeof(Image), typeof(Toggle));
            row.transform.SetParent(parent, false);
            ((RectTransform)row.transform).sizeDelta = new Vector2(0f, 54f);
            row.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.26f, 1f);
            Toggle toggle = row.GetComponent<Toggle>();
            toggle.targetGraphic = row.GetComponent<Image>();
            GameObject check = new GameObject("Selected", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(row.transform, false);
            RectTransform checkRect = (RectTransform)check.transform;
            checkRect.anchorMin = new Vector2(0f, 0f);
            checkRect.anchorMax = new Vector2(0f, 1f);
            checkRect.sizeDelta = new Vector2(8f, 0f);
            checkRect.anchoredPosition = new Vector2(4f, 0f);
            check.GetComponent<Image>().color = new Color(0.2f, 0.75f, 1f, 1f);
            toggle.graphic = check.GetComponent<Image>();
            Text label = CreateRuntimeText(row.transform, 16);
            label.text = BuildMemberLabel(character);
            label.rectTransform.offsetMin = new Vector2(18f, 2f);
            label.rectTransform.offsetMax = new Vector2(-6f, -2f);
            toggle.interactable = CharacterAvailability.CanBeAssigned(character);
            toggle.onValueChanged.AddListener(selected =>
                OnMemberSelectionChanged(character.CharacterId, defense, selected));
            return toggle;
        }

        private void OnMemberSelectionChanged(string id, bool defense, bool selected)
        {
            if (changingSelection) return;
            changingSelection = true;
            try
            {
                Dictionary<string, Toggle> own = defense ? defenseToggles : expeditionToggles;
                Dictionary<string, Toggle> other = defense ? expeditionToggles : defenseToggles;
                int limit = defense ? controller.DesiredDefenseMembers : controller.DesiredExpeditionMembers;
                if (selected && CountSelected(own) > limit) own[id].isOn = false;
                else if (selected && other.TryGetValue(id, out Toggle otherToggle)) otherToggle.isOn = false;
            }
            finally { changingSelection = false; }
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (!planningPanel.activeSelf) return;
            bool defenseRequired = controller.ManualRequirements.RequiresDefense;
            bool expeditionRequired = controller.ManualRequirements.RequiresExpedition;
            SetMemberInteractability(
                defenseToggles,
                defenseRequired && !delegateDefenseToggle.isOn);
            SetMemberInteractability(
                expeditionToggles,
                expeditionRequired && !delegateExpeditionToggle.isOn);
            defenseCountText.text = controller.ManualRequirements.RequiresDefense
                ? $"防衛編成  {CountSelected(defenseToggles)}/{controller.DesiredDefenseMembers}人"
                : "防衛イベントなし";
            expeditionCountText.text = controller.ManualRequirements.RequiresExpedition
                ? $"遠征編成  {CountSelected(expeditionToggles)}/{controller.DesiredExpeditionMembers}人"
                : "新規遠征なし";
            planSummaryText.text =
                $"防衛: {Summary(defenseToggles, delegateDefenseToggle.isOn)}\n" +
                $"遠征: {Summary(expeditionToggles, delegateExpeditionToggle.isOn)}\n" +
                "遠征先: Whispering Forest  敵戦闘力 35";
            bool defenseReady = !defenseRequired || delegateDefenseToggle.isOn ||
                CountSelected(defenseToggles) > 0;
            bool expeditionReady = !expeditionRequired || delegateExpeditionToggle.isOn ||
                CountSelected(expeditionToggles) > 0;
            bool actingReady = !controller.ManualRequirements.RequiresActingLeader ||
                delegateActingLeaderToggle.isOn || actingLeaderDropdown.options.Count > 0;
            applyButton.interactable = defenseReady && expeditionReady && actingReady;
        }

        private void PopulateActingLeaders(GuildRuntimeData guild)
        {
            actingLeaderDropdown.ClearOptions();
            var ids = new List<string>();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                if (character.CharacterId != guild.LeaderCharacterId && CharacterAvailability.CanBeAssigned(character))
                    ids.Add(character.CharacterId);
            }
            actingLeaderDropdown.AddOptions(ids);
        }

        private static string BuildMemberLabel(CharacterRuntimeData character)
        {
            string reason = CharacterAvailability.CanBeAssigned(character)
                ? string.Empty : $"  [選択不可: {character.Status}]";
            return $"{character.CharacterId}  HP {character.CurrentHp}/{character.MaxHp}" +
                   $"  攻{character.Attack} 防{character.Defense}  {character.Status}{reason}";
        }

        private void SetMemberInteractability(
            Dictionary<string, Toggle> toggles,
            bool decisionEnabled)
        {
            foreach (KeyValuePair<string, Toggle> pair in toggles)
            {
                pair.Value.interactable = decisionEnabled &&
                    controller.Guild.TryGetCharacter(pair.Key, out CharacterRuntimeData character) &&
                    CharacterAvailability.CanBeAssigned(character);
            }
        }

        private static Text CreateRuntimeText(Transform parent, int size)
        {
            GameObject gameObject = new GameObject("Member", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private static void ClearContainer(RectTransform container)
        {
            for (int index = container.childCount - 1; index >= 0; index--)
                Destroy(container.GetChild(index).gameObject);
        }

        private static int CountSelected(Dictionary<string, Toggle> toggles)
        {
            int count = 0;
            foreach (Toggle toggle in toggles.Values) if (toggle.isOn) count++;
            return count;
        }

        private static List<string> SelectedIds(Dictionary<string, Toggle> toggles)
        {
            var ids = new List<string>();
            foreach (KeyValuePair<string, Toggle> pair in toggles)
                if (pair.Value.isOn) ids.Add(pair.Key);
            return ids;
        }

        private static string Summary(Dictionary<string, Toggle> toggles, bool delegated)
        {
            if (delegated) return "CPUに任せる";
            List<string> ids = SelectedIds(toggles);
            return ids.Count == 0 ? "選択なし" : string.Join(", ", ids);
        }

        private static string SelectedId(Dropdown dropdown)
        {
            return dropdown.options.Count == 0 ? string.Empty : dropdown.options[dropdown.value].text;
        }
    }
}
