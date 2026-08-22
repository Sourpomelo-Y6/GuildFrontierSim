using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Assignments.Leadership;
using GuildFrontierSim.Application.Processing.Defense;
using GuildFrontierSim.Application.Processing.Expeditions;

namespace GuildFrontierSim.Application.Processing.Turns
{
    public sealed class TurnRequest
    {
        public TurnRequest(
            DefenseBattleRequest defenseRequest = null,
            ExpeditionStartRequest expeditionStartRequest = null,
            DefenseAssignment defenseAssignment = null,
            ExpeditionAssignment expeditionAssignment = null,
            ExpeditionAreaRegistry expeditionAreaRegistry = null,
            ActingLeaderAssignment actingLeaderAssignment = null)
        {
            if (defenseRequest != null && defenseAssignment != null)
                throw new System.ArgumentException("Choose CPU or explicit defense, not both.");
            if (expeditionStartRequest != null && expeditionAssignment != null)
                throw new System.ArgumentException("Choose CPU or explicit expedition, not both.");
            if (expeditionAssignment != null && expeditionAreaRegistry == null)
                throw new System.ArgumentNullException(nameof(expeditionAreaRegistry));
            DefenseRequest = defenseRequest;
            ExpeditionStartRequest = expeditionStartRequest;
            DefenseAssignment = defenseAssignment;
            ExpeditionAssignment = expeditionAssignment;
            ExpeditionAreaRegistry = expeditionAreaRegistry;
            ActingLeaderAssignment = actingLeaderAssignment;
        }

        public DefenseBattleRequest DefenseRequest { get; }
        public ExpeditionStartRequest ExpeditionStartRequest { get; }
        public DefenseAssignment DefenseAssignment { get; }
        public ExpeditionAssignment ExpeditionAssignment { get; }
        public ExpeditionAreaRegistry ExpeditionAreaRegistry { get; }
        public ActingLeaderAssignment ActingLeaderAssignment { get; }
    }
}
