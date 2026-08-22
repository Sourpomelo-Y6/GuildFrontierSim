using GuildFrontierSim.Domain.Characters;

namespace GuildFrontierSim.Application.Assignments.Leadership
{
    public sealed class ActingLeaderValidationResult
    {
        public ActingLeaderValidationResult(
            ActingLeaderValidationError error,
            CharacterRuntimeData candidate = null)
        {
            Error = error;
            Candidate = candidate;
        }

        public bool IsValid => Error == ActingLeaderValidationError.None;
        public ActingLeaderValidationError Error { get; }
        public CharacterRuntimeData Candidate { get; }
    }
}
