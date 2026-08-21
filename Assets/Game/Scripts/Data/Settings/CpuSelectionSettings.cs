using UnityEngine;

namespace GuildFrontierSim.Data.Settings
{
    [CreateAssetMenu(
        fileName = "CpuSelectionSettings",
        menuName = "Guild Frontier Sim/Balance/CPU Selection Settings")]
    public sealed class CpuSelectionSettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int desiredDefenseMembers = 2;
        [SerializeField, Min(1)] private int desiredExpeditionMembers = 3;
        [SerializeField, Min(0)] private int minimumGuildMembersRemaining = 1;
        [SerializeField, Range(0f, 1f)] private float minimumHpRatio = 0.5f;

        public int DesiredDefenseMembers => desiredDefenseMembers;
        public int DesiredExpeditionMembers => desiredExpeditionMembers;
        public int MinimumGuildMembersRemaining => minimumGuildMembersRemaining;
        public float MinimumHpRatio => minimumHpRatio;
    }
}
