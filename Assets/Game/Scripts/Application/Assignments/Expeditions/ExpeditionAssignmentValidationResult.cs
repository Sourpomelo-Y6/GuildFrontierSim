using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Domain.Characters;

namespace GuildFrontierSim.Application.Assignments.Expeditions
{
    public sealed class ExpeditionAssignmentValidationResult
    {
        public ExpeditionAssignmentValidationResult(
            ExpeditionAssignmentValidationError error,
            string subjectId,
            ExpeditionAreaDefinition area,
            IReadOnlyList<CharacterRuntimeData> participants)
        {
            Error = error;
            SubjectId = subjectId ?? string.Empty;
            Area = area;
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
        }

        public bool IsValid => Error == ExpeditionAssignmentValidationError.None;
        public ExpeditionAssignmentValidationError Error { get; }
        public string SubjectId { get; }
        public ExpeditionAreaDefinition Area { get; }
        public IReadOnlyList<CharacterRuntimeData> Participants { get; }
    }
}
