using GuildFrontierSim.Data.Definitions;
using UnityEngine;

namespace GuildFrontierSim.Data.Presets
{
    [CreateAssetMenu(
        fileName = "GuildStartingPreset",
        menuName = "Guild Frontier Sim/Guilds/Starting Preset")]
    public sealed class GuildStartingPreset : ScriptableObject
    {
        [SerializeField] private string guildName = "Frontier Guild";
        [SerializeField, Min(0)] private int startingFunds = 1000;
        [SerializeField] private CharacterDefinition[] startingMembers = { };
        [SerializeField] private string startingLeaderId = string.Empty;

        public string GuildName => guildName;
        public int StartingFunds => startingFunds;
        public CharacterDefinition[] StartingMembers => startingMembers;
        public string StartingLeaderId => startingLeaderId;
    }
}
