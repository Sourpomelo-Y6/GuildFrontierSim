using System;

namespace GuildFrontierSim.Application.Processing.Expeditions.Stages
{
    public sealed class ExpeditionParticipantHp
    {
        public ExpeditionParticipantHp(string characterId, int currentHp, int maximumHp)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("Character ID cannot be empty.", nameof(characterId));
            if (maximumHp < 1 || currentHp < 0 || currentHp > maximumHp)
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            CharacterId = characterId;
            CurrentHp = currentHp;
            MaximumHp = maximumHp;
        }

        public string CharacterId { get; }
        public int CurrentHp { get; }
        public int MaximumHp { get; }
    }
}
