using UnityEngine;

namespace GuildFrontierSim.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "CharacterDefinition",
        menuName = "Guild Frontier Sim/Characters/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, Min(1)] private int startingLevel = 1;
        [SerializeField, Min(1)] private int maxHp = 100;
        [SerializeField, Min(0)] private int attack = 10;
        [SerializeField, Min(0)] private int defense = 10;
        [SerializeField, Min(0)] private int speed = 10;
        [SerializeField, Min(0)] private int salary = 10;
        [SerializeField, Range(-100, 100)] private int startingLoyalty = 30;
        [SerializeField] private string visualId = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public int StartingLevel => startingLevel;
        public int MaxHp => maxHp;
        public int Attack => attack;
        public int Defense => defense;
        public int Speed => speed;
        public int Salary => salary;
        public int StartingLoyalty => startingLoyalty;
        public string VisualId => visualId;
    }
}
