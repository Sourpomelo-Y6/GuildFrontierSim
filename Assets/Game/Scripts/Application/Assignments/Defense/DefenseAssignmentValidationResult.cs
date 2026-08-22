using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Characters;

namespace GuildFrontierSim.Application.Assignments.Defense
{
    public sealed class DefenseAssignmentValidationResult
    {
        public DefenseAssignmentValidationResult(
            DefenseAssignmentValidationError error,
            string characterId,
            IReadOnlyList<CharacterRuntimeData> defenders)
        {
            Error = error;
            CharacterId = characterId ?? string.Empty;
            Defenders = defenders ?? throw new ArgumentNullException(nameof(defenders));
        }

        public bool IsValid => Error == DefenseAssignmentValidationError.None;
        public DefenseAssignmentValidationError Error { get; }
        public string CharacterId { get; }
        public IReadOnlyList<CharacterRuntimeData> Defenders { get; }
    }
}
