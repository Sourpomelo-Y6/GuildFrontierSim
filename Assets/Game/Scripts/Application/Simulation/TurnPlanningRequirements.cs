namespace GuildFrontierSim.Application.Simulation
{
    public sealed class TurnPlanningRequirements
    {
        public TurnPlanningRequirements(
            bool requiresDefense,
            bool requiresExpedition,
            bool requiresActingLeader)
        {
            RequiresDefense = requiresDefense;
            RequiresExpedition = requiresExpedition;
            RequiresActingLeader = requiresActingLeader;
        }

        public bool RequiresDefense { get; }
        public bool RequiresExpedition { get; }
        public bool RequiresActingLeader { get; }
    }
}
