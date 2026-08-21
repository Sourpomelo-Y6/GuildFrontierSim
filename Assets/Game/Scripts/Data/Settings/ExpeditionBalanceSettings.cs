using UnityEngine;

namespace GuildFrontierSim.Data.Settings
{
    [CreateAssetMenu(
        fileName = "ExpeditionBalanceSettings",
        menuName = "Guild Frontier Sim/Balance/Expedition Settings")]
    public sealed class ExpeditionBalanceSettings : ScriptableObject
    {
        [Header("Stage Battle")]
        [SerializeField, Min(0f)] private float enemyPowerGrowthPerStage = 0.15f;
        [SerializeField, Min(0)] private int minimumStageFunds = 50;
        [SerializeField, Min(0)] private int maximumStageFunds = 100;

        [Header("Retreat")]
        [SerializeField, Range(0f, 1f)] private float escapeChance = 0.65f;
        [SerializeField, Range(0f, 1f)] private float escapedLootRetentionRatio = 0.5f;
        [SerializeField, Min(0)] private int returnFundsThreshold = 300;
        [SerializeField, Range(0f, 1f)] private float minimumPartyHpRatioToContinue = 0.4f;

        [Header("Rescue")]
        [SerializeField, Range(0f, 1f)] private float captiveRescueChance = 0.2f;

        public float EnemyPowerGrowthPerStage => enemyPowerGrowthPerStage;
        public int MinimumStageFunds => minimumStageFunds;
        public int MaximumStageFunds => maximumStageFunds;
        public float EscapeChance => escapeChance;
        public float EscapedLootRetentionRatio => escapedLootRetentionRatio;
        public int ReturnFundsThreshold => returnFundsThreshold;
        public float MinimumPartyHpRatioToContinue => minimumPartyHpRatioToContinue;
        public float CaptiveRescueChance => captiveRescueChance;

        private void OnValidate()
        {
            if (maximumStageFunds < minimumStageFunds)
            {
                maximumStageFunds = minimumStageFunds;
            }
        }
    }
}
