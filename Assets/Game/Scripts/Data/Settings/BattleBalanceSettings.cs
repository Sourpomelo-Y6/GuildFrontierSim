using UnityEngine;

namespace GuildFrontierSim.Data.Settings
{
    [CreateAssetMenu(
        fileName = "BattleBalanceSettings",
        menuName = "Guild Frontier Sim/Balance/Battle Settings")]
    public sealed class BattleBalanceSettings : ScriptableObject
    {
        [Header("Battle Power")]
        [SerializeField, Min(0f)] private float attackWeight = 1f;
        [SerializeField, Min(0f)] private float defenseWeight = 0.75f;
        [SerializeField, Min(0f)] private float hitPointWeight = 0.1f;
        [SerializeField, Min(0f)] private float speedWeight = 0.5f;

        [Header("Random Modifier")]
        [SerializeField, Min(0f)] private float minimumPowerMultiplier = 0.85f;
        [SerializeField, Min(0f)] private float maximumPowerMultiplier = 1.15f;

        [Header("Consequences")]
        [SerializeField, Range(0f, 1f)] private float injuryChance = 0.2f;
        [SerializeField, Range(0f, 1f)] private float hospitalizationChance = 0.05f;
        [SerializeField, Range(0f, 1f)] private float captureChance = 0.15f;
        [SerializeField, Min(1)] private int minimumRecoveryTurns = 1;
        [SerializeField, Min(1)] private int maximumRecoveryTurns = 3;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int minimumDefenseReward = 50;
        [SerializeField, Min(0)] private int maximumDefenseReward = 150;
        [SerializeField, Min(0)] private int minimumExpeditionReward = 75;
        [SerializeField, Min(0)] private int maximumExpeditionReward = 250;

        [Header("Economy and Loyalty")]
        [SerializeField, Min(1)] private int salaryIntervalTurns = 5;
        [SerializeField, Min(0)] private int unpaidSalaryLoyaltyPenalty = 10;
        [SerializeField] private int victoryLoyaltyChange = 2;
        [SerializeField] private int defeatLoyaltyChange = -3;

        public float AttackWeight => attackWeight;
        public float DefenseWeight => defenseWeight;
        public float HitPointWeight => hitPointWeight;
        public float SpeedWeight => speedWeight;
        public float MinimumPowerMultiplier => minimumPowerMultiplier;
        public float MaximumPowerMultiplier => maximumPowerMultiplier;
        public float InjuryChance => injuryChance;
        public float HospitalizationChance => hospitalizationChance;
        public float CaptureChance => captureChance;
        public int MinimumRecoveryTurns => minimumRecoveryTurns;
        public int MaximumRecoveryTurns => maximumRecoveryTurns;
        public int MinimumDefenseReward => minimumDefenseReward;
        public int MaximumDefenseReward => maximumDefenseReward;
        public int MinimumExpeditionReward => minimumExpeditionReward;
        public int MaximumExpeditionReward => maximumExpeditionReward;
        public int SalaryIntervalTurns => salaryIntervalTurns;
        public int UnpaidSalaryLoyaltyPenalty => unpaidSalaryLoyaltyPenalty;
        public int VictoryLoyaltyChange => victoryLoyaltyChange;
        public int DefeatLoyaltyChange => defeatLoyaltyChange;

        private void OnValidate()
        {
            if (maximumPowerMultiplier < minimumPowerMultiplier)
            {
                maximumPowerMultiplier = minimumPowerMultiplier;
            }

            if (maximumRecoveryTurns < minimumRecoveryTurns)
            {
                maximumRecoveryTurns = minimumRecoveryTurns;
            }

            if (maximumDefenseReward < minimumDefenseReward)
            {
                maximumDefenseReward = minimumDefenseReward;
            }

            if (maximumExpeditionReward < minimumExpeditionReward)
            {
                maximumExpeditionReward = minimumExpeditionReward;
            }
        }
    }
}
