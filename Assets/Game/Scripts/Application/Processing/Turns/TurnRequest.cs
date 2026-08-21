using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Application.Processing.Expeditions;

namespace GuildFrontierSim.Application.Processing.Turns
{
    public sealed class TurnRequest
    {
        public TurnRequest(
            DefenseBattleRequest defenseRequest = null,
            ExpeditionStartRequest expeditionStartRequest = null)
        {
            DefenseRequest = defenseRequest;
            ExpeditionStartRequest = expeditionStartRequest;
        }

        public DefenseBattleRequest DefenseRequest { get; }
        public ExpeditionStartRequest ExpeditionStartRequest { get; }
    }
}
