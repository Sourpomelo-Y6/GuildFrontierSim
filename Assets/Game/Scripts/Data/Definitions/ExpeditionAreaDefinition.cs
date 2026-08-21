using UnityEngine;

namespace GuildFrontierSim.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "ExpeditionAreaDefinition",
        menuName = "Guild Frontier Sim/Expeditions/Area Definition")]
    public sealed class ExpeditionAreaDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, Min(0)] private int enemyPower = 100;
        [SerializeField, Min(1)] private int maximumStages = 3;
        [SerializeField, Min(0f)] private float rewardMultiplier = 1f;
        [SerializeField] private bool canContainCaptives = true;

        public string Id => id;
        public string DisplayName => displayName;
        public int EnemyPower => enemyPower;
        public int MaximumStages => maximumStages;
        public float RewardMultiplier => rewardMultiplier;
        public bool CanContainCaptives => canContainCaptives;
    }
}
