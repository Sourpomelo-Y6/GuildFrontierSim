namespace GuildFrontierSim.Application.Assignments.Leadership
{
    public enum ActingLeaderValidationError
    {
        None,
        StaleRevision,
        LeaderAvailable,
        LeaderDeparturePending,
        CharacterNotFound,
        FormalLeaderSelected,
        CharacterUnavailable
    }
}
