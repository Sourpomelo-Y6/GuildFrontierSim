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
            Set(serialized, "enemyPower", 70);
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
            AssignController(controller, preset, area, battle, cpu, expedition, simulation);

            Canvas canvas = CreateCanvas();
            Text title = CreateText(canvas.transform, "Title", "Guild Frontier Sim - Phase 1", 30);
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
