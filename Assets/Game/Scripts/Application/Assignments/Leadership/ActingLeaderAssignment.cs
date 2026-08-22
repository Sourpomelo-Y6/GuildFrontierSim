using System;

namespace GuildFrontierSim.Application.Assignments.Leadership
{
    public sealed class ActingLeaderAssignment
    {
        public ActingLeaderAssignment(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("Character ID cannot be empty.", nameof(characterId));
            CharacterId = characterId.Trim();
        }

        public string CharacterId { get; }
    }
}
