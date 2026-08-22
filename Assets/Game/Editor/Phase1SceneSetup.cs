using System;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Presets;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuildFrontierSim.Editor
{
    public static class Phase1SceneSetup
    {
        private const string DataFolder = "Assets/Game/Data/Phase1";
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Guild Frontier Sim/Build Phase 1 Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/Game", "Data");
            EnsureFolder("Assets/Game/Data", "Phase1");

            CharacterDefinition aria = CreateCharacter(
                "aria", "アリア", 3, 130, 18, 16, 14, 35, 45);
            CharacterDefinition borin = CreateCharacter(
                "borin", "ボリン", 2, 150, 16, 20, 8, 28, 35);
            CharacterDefinition celine = CreateCharacter(
                "celine", "セリーヌ", 2, 100, 20, 10, 18, 30, 40);
            CharacterDefinition daichi = CreateCharacter(
                "daichi", "ダイチ", 1, 120, 14, 14, 12, 22, 30);
            GuildStartingPreset preset = CreatePreset(aria, borin, celine, daichi);
            ExpeditionAreaDefinition area = CreateArea();
            BattleBalanceSettings battle = CreateAsset<BattleBalanceSettings>(
                $"{DataFolder}/BattleBalance.asset");
            CpuSelectionSettings cpu = CreateAsset<CpuSelectionSettings>(
                $"{DataFolder}/CpuSelection.asset");
            ExpeditionBalanceSettings expedition = CreateAsset<ExpeditionBalanceSettings>(
                $"{DataFolder}/ExpeditionBalance.asset");
            GuildSimulationSettings simulation = CreateSimulationSettings();

            BuildScene(preset, area, battle, cpu, expedition, simulation);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Phase 1 scene and data were generated at {ScenePath}.");
        }

        [MenuItem("Guild Frontier Sim/Build Windows Development Player")]
        public static void BuildWindowsDevelopment()
        {
            BuildReport report = BuildPipeline.BuildPlayer(
                new[] { ScenePath },
                "Builds/Windows/GuildFrontierSim.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows development build failed: {report.summary.result}");
            }

            Debug.Log(
                $"Windows development build succeeded: {report.summary.totalSize} bytes");
        }

        private static CharacterDefinition CreateCharacter(
            string id,
            string displayName,
            int level,
            int maxHp,
            int attack,
            int defense,
            int speed,
            int salary,
            int loyalty)
        {
            CharacterDefinition asset = CreateAsset<CharacterDefinition>(
                $"{DataFolder}/Character_{id}.asset");
            SerializedObject serialized = new SerializedObject(asset);
            Set(serialized, "id", id);
            Set(serialized, "displayName", displayName);
            Set(serialized, "startingLevel", level);
            Set(serialized, "maxHp", maxHp);
            Set(serialized, "attack", attack);
            Set(serialized, "defense", defense);
            Set(serialized, "speed", speed);
            Set(serialized, "salary", salary);
            Set(serialized, "startingLoyalty", loyalty);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static GuildStartingPreset CreatePreset(
            params CharacterDefinition[] characters)
        {
            GuildStartingPreset asset = CreateAsset<GuildStartingPreset>(
                $"{DataFolder}/FrontierGuild.asset");
            SerializedObject serialized = new SerializedObject(asset);
            Set(serialized, "guildName", "辺境の灯火ギルド");
            Set(serialized, "startingFunds", 500);
            Set(serialized, "startingLeaderId", "aria");
            SerializedProperty members = serialized.FindProperty("startingMembers");
            members.arraySize = characters.Length;
            for (int index = 0; index < characters.Length; index++)
            {
                members.GetArrayElementAtIndex(index).objectReferenceValue = characters[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ExpeditionAreaDefinition CreateArea()
        {
            ExpeditionAreaDefinition asset = CreateAsset<ExpeditionAreaDefinition>(
                $"{DataFolder}/WhisperingForest.asset");
            SerializedObject serialized = new SerializedObject(asset);
            Set(serialized, "id", "whispering-forest");
            Set(serialized, "displayName", "ささやきの森");
            Set(serialized, "enemyPower", 35);
            Set(serialized, "maximumStages", 3);
            Set(serialized, "rewardMultiplier", 1f);
            Set(serialized, "canContainCaptives", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static GuildSimulationSettings CreateSimulationSettings()
        {
            GuildSimulationSettings asset = CreateAsset<GuildSimulationSettings>(
                $"{DataFolder}/Simulation.asset");
            SerializedObject serialized = new SerializedObject(asset);
            Set(serialized, "defenseIntervalTurns", 2);
            Set(serialized, "defenseEnemyBasePower", 80f);
            Set(serialized, "automaticallyStartExpeditions", true);
            Set(serialized, "expeditionIntervalTurns", 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void BuildScene(
            GuildStartingPreset preset,
            ExpeditionAreaDefinition area,
            BattleBalanceSettings battle,
            CpuSelectionSettings cpu,
            ExpeditionBalanceSettings expedition,
            GuildSimulationSettings simulation)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();

            GameObject application = new GameObject("Guild Simulation");
            GuildSimulationController controller =
                application.AddComponent<GuildSimulationController>();
            GuildSimulationView view = application.AddComponent<GuildSimulationView>();
            ManualManagementView manualView = application.AddComponent<ManualManagementView>();
            ExpeditionDecisionView decisionView = application.AddComponent<ExpeditionDecisionView>();
            AssignController(controller, preset, area, battle, cpu, expedition, simulation);

            Canvas canvas = CreateCanvas();
            Text title = CreateText(canvas.transform, "Title", "Guild Frontier Sim - Phase 2", 30);
            SetRect(title.rectTransform, 24, -20, 1872, 50);
            Text summary = CreateText(canvas.transform, "Summary", string.Empty, 22);
            SetRect(summary.rectTransform, 24, -82, 1872, 70);
            Text members = CreateText(canvas.transform, "Members", string.Empty, 19);
            SetRect(members.rectTransform, 24, -165, 900, 420);
            Text expeditions = CreateText(canvas.transform, "Expeditions", string.Empty, 19);
            SetRect(expeditions.rectTransform, 948, -165, 948, 250);
            Text logs = CreateText(canvas.transform, "Logs", string.Empty, 17);
            SetRect(logs.rectTransform, 948, -430, 948, 500);
            Button button = CreateButton(canvas.transform, "Advance Turn", "次のターン");
            SetRect((RectTransform)button.transform, 24, -900, 680, 120);
            Button resetButton = CreateButton(canvas.transform, "Reset Simulation", "最初から");
            SetRect((RectTransform)resetButton.transform, 724, -900, 200, 120);
            Button modeButton = CreateButton(canvas.transform, "Management Mode", "CPU経営");
            SetRect((RectTransform)modeButton.transform, 948, -82, 220, 64);

            GameObject planningPanel = new GameObject(
                "Manual Planning Panel", typeof(RectTransform), typeof(Image));
            planningPanel.transform.SetParent(canvas.transform, false);
            planningPanel.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.19f, 0.96f);
            SetRect((RectTransform)planningPanel.transform, 24, -150, 1872, 820);
            Text planningStatus = CreateText(
                planningPanel.transform, "Planning Status", string.Empty, 20);
            SetRect(planningStatus.rectTransform, 24, -18, 1824, 40);
            Text defenseCount = CreateText(
                planningPanel.transform, "Defense Count", "防衛編成", 22);
            SetRect(defenseCount.rectTransform, 24, -72, 620, 40);
            Toggle defenseCpu = CreateToggle(
                planningPanel.transform, "Delegate Defense", "防衛をCPUへ");
            SetRect((RectTransform)defenseCpu.transform, 680, -68, 200, 50);
            RectTransform defenseContainer = CreateListContainer(
                planningPanel.transform, "Defense Member List");
            SetRect(defenseContainer, 24, -122, 856, 330);
            Text expeditionCount = CreateText(
                planningPanel.transform, "Expedition Count", "遠征編成", 22);
            SetRect(expeditionCount.rectTransform, 960, -72, 620, 40);
            Toggle expeditionCpu = CreateToggle(
                planningPanel.transform, "Delegate Expedition", "遠征をCPUへ");
            SetRect((RectTransform)expeditionCpu.transform, 1640, -68, 200, 50);
            RectTransform expeditionContainer = CreateListContainer(
                planningPanel.transform, "Expedition Member List");
            SetRect(expeditionContainer, 960, -122, 888, 330);
            Dropdown actingDropdown = CreateDropdown(
                planningPanel.transform, "Acting Leader Selection", "代理リーダー");
            SetRect((RectTransform)actingDropdown.transform, 24, -480, 320, 55);
            Toggle actingCpu = CreateToggle(
                planningPanel.transform, "Delegate Acting Leader", "代理をCPUへ");
            SetRect((RectTransform)actingCpu.transform, 370, -480, 200, 55);
            Text planSummary = CreateText(
                planningPanel.transform, "Plan Summary", string.Empty, 19);
            SetRect(planSummary.rectTransform, 620, -470, 1228, 120);
            Button cancelPlan = CreateButton(planningPanel.transform, "Cancel Turn Plan", "キャンセル");
            SetRect((RectTransform)cancelPlan.transform, 24, -650, 280, 110);
            Button delegateAll = CreateButton(planningPanel.transform, "Delegate All Decisions", "すべてCPUに任せる");
            SetRect((RectTransform)delegateAll.transform, 330, -650, 440, 110);
            Button applyPlan = CreateButton(
                planningPanel.transform, "Apply Turn Plan", "この計画でターンを実行");
            SetRect((RectTransform)applyPlan.transform, 800, -650, 1048, 110);

            SerializedObject viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("controller").objectReferenceValue = controller;
            viewSerialized.FindProperty("advanceTurnButton").objectReferenceValue = button;
            viewSerialized.FindProperty("resetButton").objectReferenceValue = resetButton;
            viewSerialized.FindProperty("summaryText").objectReferenceValue = summary;
            viewSerialized.FindProperty("membersText").objectReferenceValue = members;
            viewSerialized.FindProperty("expeditionsText").objectReferenceValue = expeditions;
            viewSerialized.FindProperty("logText").objectReferenceValue = logs;
            viewSerialized.FindProperty("maximumLogLines").intValue = 12;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject manualSerialized = new SerializedObject(manualView);
            manualSerialized.FindProperty("controller").objectReferenceValue = controller;
            manualSerialized.FindProperty("modeButton").objectReferenceValue = modeButton;
            manualSerialized.FindProperty("modeButtonText").objectReferenceValue =
                modeButton.GetComponentInChildren<Text>();
            manualSerialized.FindProperty("planningPanel").objectReferenceValue = planningPanel;
            manualSerialized.FindProperty("statusText").objectReferenceValue = planningStatus;
            manualSerialized.FindProperty("defenseMemberContainer").objectReferenceValue = defenseContainer;
            manualSerialized.FindProperty("defenseCountText").objectReferenceValue = defenseCount;
            manualSerialized.FindProperty("delegateDefenseToggle").objectReferenceValue = defenseCpu;
            manualSerialized.FindProperty("expeditionMemberContainer").objectReferenceValue = expeditionContainer;
            manualSerialized.FindProperty("expeditionCountText").objectReferenceValue = expeditionCount;
            manualSerialized.FindProperty("delegateExpeditionToggle").objectReferenceValue = expeditionCpu;
            manualSerialized.FindProperty("actingLeaderDropdown").objectReferenceValue = actingDropdown;
            manualSerialized.FindProperty("delegateActingLeaderToggle").objectReferenceValue = actingCpu;
            manualSerialized.FindProperty("planSummaryText").objectReferenceValue = planSummary;
            manualSerialized.FindProperty("applyButton").objectReferenceValue = applyPlan;
            manualSerialized.FindProperty("cancelButton").objectReferenceValue = cancelPlan;
            manualSerialized.FindProperty("delegateAllButton").objectReferenceValue = delegateAll;
            manualSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject decisionPanel = new GameObject(
                "Expedition Decision Panel", typeof(RectTransform), typeof(Image));
            decisionPanel.transform.SetParent(canvas.transform, false);
            decisionPanel.GetComponent<Image>().color = new Color(0.07f, 0.1f, 0.16f, 0.99f);
            SetRect((RectTransform)decisionPanel.transform, 510, -180, 900, 650);
            Text decisionTitle = CreateText(
                decisionPanel.transform, "Decision Title", "遠征結果", 28);
            decisionTitle.alignment = TextAnchor.MiddleCenter;
            SetRect(decisionTitle.rectTransform, 20, -16, 860, 50);
            Text decisionDetails = CreateText(
                decisionPanel.transform, "Decision Details", string.Empty, 20);
            SetRect(decisionDetails.rectTransform, 36, -82, 828, 400);
            Button continueButton = CreateButton(
                decisionPanel.transform, "Continue Expedition", "遠征を続ける");
            SetRect((RectTransform)continueButton.transform, 36, -520, 250, 90);
            Button returnButton = CreateButton(
                decisionPanel.transform, "Return Expedition", "帰還する");
            SetRect((RectTransform)returnButton.transform, 325, -520, 250, 90);
            Button delegateButton = CreateButton(
                decisionPanel.transform, "Delegate Expedition Decision", "CPUに任せる");
            SetRect((RectTransform)delegateButton.transform, 614, -520, 250, 90);

            SerializedObject decisionSerialized = new SerializedObject(decisionView);
            decisionSerialized.FindProperty("controller").objectReferenceValue = controller;
            decisionSerialized.FindProperty("panel").objectReferenceValue = decisionPanel;
            decisionSerialized.FindProperty("detailsText").objectReferenceValue = decisionDetails;
            decisionSerialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            decisionSerialized.FindProperty("returnButton").objectReferenceValue = returnButton;
            decisionSerialized.FindProperty("delegateButton").objectReferenceValue = delegateButton;
            decisionSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static void AssignController(
            GuildSimulationController controller,
            GuildStartingPreset preset,
            ExpeditionAreaDefinition area,
            BattleBalanceSettings battle,
            CpuSelectionSettings cpu,
            ExpeditionBalanceSettings expedition,
            GuildSimulationSettings simulation)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("guildStartingPreset").objectReferenceValue = preset;
            serialized.FindProperty("expeditionArea").objectReferenceValue = area;
            serialized.FindProperty("battleSettings").objectReferenceValue = battle;
            serialized.FindProperty("cpuSelectionSettings").objectReferenceValue = cpu;
            serialized.FindProperty("expeditionSettings").objectReferenceValue = expedition;
            serialized.FindProperty("simulationSettings").objectReferenceValue = simulation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Canvas CreateCanvas()
        {
            GameObject gameObject = new GameObject(
                "Phase 1 UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(Image));
            Canvas canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.GetComponent<Image>().color = new Color(0.055f, 0.075f, 0.11f, 1f);
            return canvas;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = new Color(0.9f, 0.93f, 1f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.42f, 0.65f, 1f);
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(gameObject.transform, "Label", label, 28);
            text.alignment = TextAnchor.MiddleCenter;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            GameObject box = new GameObject("Background", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(root.transform, false);
            SetRect((RectTransform)box.transform, 0, -8, 32, 32);
            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            RectTransform checkRect = (RectTransform)check.transform;
            checkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
            check.GetComponent<Image>().color = new Color(0.2f, 0.75f, 1f, 1f);
            Text text = CreateText(root.transform, "Label", label, 17);
            SetRect(text.rectTransform, 42, -4, 140, 40);
            Toggle toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            return toggle;
        }

        private static Dropdown CreateDropdown(Transform parent, string name, string placeholder)
        {
            GameObject root = new GameObject(
                name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = new Color(0.14f, 0.2f, 0.29f, 1f);
            Text caption = CreateText(root.transform, "Label", placeholder, 18);
            caption.alignment = TextAnchor.MiddleLeft;
            SetRect(caption.rectTransform, 12, 0, 210, 50);

            GameObject template = new GameObject(
                "Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(root.transform, false);
            RectTransform templateRect = (RectTransform)template.transform;
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0f, 180f);
            template.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.22f, 1f);

            GameObject viewport = new GameObject(
                "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            RectTransform viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 40f);

            GameObject item = new GameObject(
                "Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = (RectTransform)item.transform;
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 40f);
            Text itemText = CreateText(item.transform, "Item Label", placeholder, 17);
            itemText.alignment = TextAnchor.MiddleLeft;
            itemText.rectTransform.anchorMin = Vector2.zero;
            itemText.rectTransform.anchorMax = Vector2.one;
            itemText.rectTransform.offsetMin = new Vector2(12f, 0f);
            itemText.rectTransform.offsetMax = Vector2.zero;
            Toggle itemToggle = item.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemText;

            ScrollRect scroll = template.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            Dropdown dropdown = root.GetComponent<Dropdown>();
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            template.SetActive(false);
            return dropdown;
        }

        private static RectTransform CreateListContainer(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = new Color(0.07f, 0.1f, 0.15f, 1f);
            VerticalLayoutGroup layout = gameObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return (RectTransform)gameObject.transform;
        }

        private static void SetRect(
            RectTransform rect,
            float left,
            float top,
            float width,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void CreateCamera()
        {
            GameObject gameObject = new GameObject("Main Camera", typeof(Camera));
            gameObject.tag = "MainCamera";
            Camera camera = gameObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void Set(SerializedObject serialized, string name, string value)
        {
            serialized.FindProperty(name).stringValue = value;
        }

        private static void Set(SerializedObject serialized, string name, int value)
        {
            serialized.FindProperty(name).intValue = value;
        }

        private static void Set(SerializedObject serialized, string name, float value)
        {
            serialized.FindProperty(name).floatValue = value;
        }

        private static void Set(SerializedObject serialized, string name, bool value)
        {
            serialized.FindProperty(name).boolValue = value;
        }
    }
}
