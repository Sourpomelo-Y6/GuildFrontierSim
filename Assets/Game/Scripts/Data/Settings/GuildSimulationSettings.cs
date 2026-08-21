using UnityEngine;

namespace GuildFrontierSim.Data.Settings
{
    [CreateAssetMenu(
        fileName = "GuildSimulationSettings",
        menuName = "Guild Frontier Sim/Simulation/Settings")]
    public sealed class GuildSimulationSettings : ScriptableObject
    {
        [Header("Defense")]
        [SerializeField, Min(1)] private int defenseIntervalTurns = 1;
        [SerializeField, Min(0f)] private float defenseEnemyBasePower = 100f;

        [Header("Expedition")]
        [SerializeField] private bool automaticallyStartExpeditions = true;
        [SerializeField, Min(1)] private int expeditionIntervalTurns = 1;

        public int DefenseIntervalTurns => defenseIntervalTurns;
        public float DefenseEnemyBasePower => defenseEnemyBasePower;
        public bool AutomaticallyStartExpeditions => automaticallyStartExpeditions;
        public int ExpeditionIntervalTurns => expeditionIntervalTurns;
    }
}
