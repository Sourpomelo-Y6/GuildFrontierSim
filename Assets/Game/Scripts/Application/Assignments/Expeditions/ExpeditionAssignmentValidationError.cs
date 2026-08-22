namespace GuildFrontierSim.Application.Assignments.Expeditions
{
    public enum ExpeditionAssignmentValidationError
    {
        None,
        AreaNotFound,
        DuplicateExpeditionId,
        InvalidCharacterId,
        DuplicateCharacterId,
        CharacterNotFound,
        CharacterUnavailable,
        HitPointsBelowMinimum,
        ReservedForDefense,
        TooManyParticipants,
        InsufficientGuildMembersRemaining
    }
}
