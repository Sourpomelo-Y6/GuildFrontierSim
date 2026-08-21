using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Presets;
using GuildFrontierSim.Data.Settings;
using UnityEditor;
using UnityEngine;

namespace GuildFrontierSim.Tests
{
    internal static class TestAssetFactory
    {
        public static CharacterDefinition CreateCharacter(
            string id,
            int level = 1,
            int maxHp = 100,
            int attack = 10,
            int defense = 10,
            int speed = 10,
            int salary = 10,
            int loyalty = 30)
        {
            CharacterDefinition definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            var serializedObject = new SerializedObject(definition);
            serializedObject.FindProperty("id").stringValue = id;
            serializedObject.FindProperty("displayName").stringValue = id;
            serializedObject.FindProperty("startingLevel").intValue = level;
            serializedObject.FindProperty("maxHp").intValue = maxHp;
            serializedObject.FindProperty("attack").intValue = attack;
            serializedObject.FindProperty("defense").intValue = defense;
            serializedObject.FindProperty("speed").intValue = speed;
            serializedObject.FindProperty("salary").intValue = salary;
            serializedObject.FindProperty("startingLoyalty").intValue = loyalty;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        public static GuildStartingPreset CreateGuildPreset(
            string guildName,
            int funds,
            string leaderId,
            params CharacterDefinition[] members)
        {
            GuildStartingPreset preset =
                ScriptableObject.CreateInstance<GuildStartingPreset>();
            var serializedObject = new SerializedObject(preset);
            serializedObject.FindProperty("guildName").stringValue = guildName;
            serializedObject.FindProperty("startingFunds").intValue = funds;
            serializedObject.FindProperty("startingLeaderId").stringValue = leaderId;

            SerializedProperty memberProperty = serializedObject.FindProperty("startingMembers");
            memberProperty.arraySize = members.Length;
            for (int index = 0; index < members.Length; index++)
            {
                memberProperty.GetArrayElementAtIndex(index).objectReferenceValue = members[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return preset;
        }

        public static ExpeditionAreaDefinition CreateExpeditionArea(
            string id,
            int enemyPower = 100,
            int maximumStages = 3,
            float rewardMultiplier = 1f,
            bool canContainCaptives = true)
        {
            ExpeditionAreaDefinition area =
                ScriptableObject.CreateInstance<ExpeditionAreaDefinition>();
            var serializedObject = new SerializedObject(area);
            serializedObject.FindProperty("id").stringValue = id;
            serializedObject.FindProperty("displayName").stringValue = id;
            serializedObject.FindProperty("enemyPower").intValue = enemyPower;
            serializedObject.FindProperty("maximumStages").intValue = maximumStages;
            serializedObject.FindProperty("rewardMultiplier").floatValue = rewardMultiplier;
            serializedObject.FindProperty("canContainCaptives").boolValue = canContainCaptives;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return area;
        }

        public static BattleBalanceSettings CreateBattleSettings(
            float attackWeight = 1f,
            float defenseWeight = 0.75f,
            float hitPointWeight = 0.1f,
            float speedWeight = 0.5f,
            float minimumPowerMultiplier = 0.85f,
            float maximumPowerMultiplier = 1.15f,
            float victoryInjuryChance = 0.05f,
            float injuryChance = 0.2f,
            float hospitalizationChance = 0.05f,
            int minimumRecoveryTurns = 1,
            int maximumRecoveryTurns = 3,
            int minimumDefenseReward = 50,
            int maximumDefenseReward = 150,
            float defeatDefenseRewardMultiplier = 0.5f,
            int salaryIntervalTurns = 5,
            int unpaidSalaryLoyaltyPenalty = 10)
        {
            BattleBalanceSettings settings =
                ScriptableObject.CreateInstance<BattleBalanceSettings>();
            var serializedObject = new SerializedObject(settings);
            serializedObject.FindProperty("attackWeight").floatValue = attackWeight;
            serializedObject.FindProperty("defenseWeight").floatValue = defenseWeight;
            serializedObject.FindProperty("hitPointWeight").floatValue = hitPointWeight;
            serializedObject.FindProperty("speedWeight").floatValue = speedWeight;
            serializedObject.FindProperty("minimumPowerMultiplier").floatValue =
                minimumPowerMultiplier;
            serializedObject.FindProperty("maximumPowerMultiplier").floatValue =
                maximumPowerMultiplier;
            serializedObject.FindProperty("victoryInjuryChance").floatValue =
                victoryInjuryChance;
            serializedObject.FindProperty("injuryChance").floatValue = injuryChance;
            serializedObject.FindProperty("hospitalizationChance").floatValue =
                hospitalizationChance;
            serializedObject.FindProperty("minimumRecoveryTurns").intValue =
                minimumRecoveryTurns;
            serializedObject.FindProperty("maximumRecoveryTurns").intValue =
                maximumRecoveryTurns;
            serializedObject.FindProperty("minimumDefenseReward").intValue =
                minimumDefenseReward;
            serializedObject.FindProperty("maximumDefenseReward").intValue =
                maximumDefenseReward;
            serializedObject.FindProperty("defeatDefenseRewardMultiplier").floatValue =
                defeatDefenseRewardMultiplier;
            serializedObject.FindProperty("salaryIntervalTurns").intValue =
                salaryIntervalTurns;
            serializedObject.FindProperty("unpaidSalaryLoyaltyPenalty").intValue =
                unpaidSalaryLoyaltyPenalty;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        public static CpuSelectionSettings CreateCpuSelectionSettings(
            int desiredDefenseMembers = 2,
            int desiredExpeditionMembers = 3,
            int minimumGuildMembersRemaining = 1,
            float minimumHpRatio = 0.5f)
        {
            CpuSelectionSettings settings =
                ScriptableObject.CreateInstance<CpuSelectionSettings>();
            var serializedObject = new SerializedObject(settings);
            serializedObject.FindProperty("desiredDefenseMembers").intValue =
                desiredDefenseMembers;
            serializedObject.FindProperty("desiredExpeditionMembers").intValue =
                desiredExpeditionMembers;
            serializedObject.FindProperty("minimumGuildMembersRemaining").intValue =
                minimumGuildMembersRemaining;
            serializedObject.FindProperty("minimumHpRatio").floatValue = minimumHpRatio;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        public static ExpeditionBalanceSettings CreateExpeditionBalanceSettings(
            float enemyPowerGrowthPerStage = 0.15f,
            int minimumStageFunds = 50,
            int maximumStageFunds = 100,
            float escapeChance = 0.65f,
            float escapedLootRetentionRatio = 0.5f,
            int returnFundsThreshold = 300,
            float minimumPartyHpRatioToContinue = 0.4f,
            float captiveRescueChance = 0.2f)
        {
            ExpeditionBalanceSettings settings =
                ScriptableObject.CreateInstance<ExpeditionBalanceSettings>();
            var serializedObject = new SerializedObject(settings);
            serializedObject.FindProperty("enemyPowerGrowthPerStage").floatValue =
                enemyPowerGrowthPerStage;
            serializedObject.FindProperty("minimumStageFunds").intValue = minimumStageFunds;
            serializedObject.FindProperty("maximumStageFunds").intValue = maximumStageFunds;
            serializedObject.FindProperty("escapeChance").floatValue = escapeChance;
            serializedObject.FindProperty("escapedLootRetentionRatio").floatValue =
                escapedLootRetentionRatio;
            serializedObject.FindProperty("returnFundsThreshold").intValue =
                returnFundsThreshold;
            serializedObject.FindProperty("minimumPartyHpRatioToContinue").floatValue =
                minimumPartyHpRatioToContinue;
            serializedObject.FindProperty("captiveRescueChance").floatValue =
                captiveRescueChance;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        public static void Destroy(params Object[] assets)
        {
            foreach (Object asset in assets)
            {
                if (asset != null)
                {
                    Object.DestroyImmediate(asset);
                }
            }
        }
    }
}
