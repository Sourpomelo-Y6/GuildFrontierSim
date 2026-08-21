using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Presets;
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
