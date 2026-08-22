namespace GuildFrontierSim.Application.Assignments.Defense
{
    public enum DefenseAssignmentValidationError
    {
        None,
        InvalidCharacterId,
        DuplicateCharacterId,
        CharacterNotFound,
        CharacterUnavailable,
        HitPointsBelowMinimum,
        TooManyDefenders
    }
}
