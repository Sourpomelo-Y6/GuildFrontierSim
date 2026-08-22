namespace GuildFrontierSim.Domain.Characters
{
    public static class CharacterAvailability
    {
        public static bool CanBeAssigned(CharacterRuntimeData character)
        {
            return character != null &&
                   character.Status == CharacterStatus.Available &&
                   character.CurrentHp > 0 &&
                   character.UnavailableTurnsRemaining == 0 &&
                   !character.IsDeparturePending;
        }
    }
}
