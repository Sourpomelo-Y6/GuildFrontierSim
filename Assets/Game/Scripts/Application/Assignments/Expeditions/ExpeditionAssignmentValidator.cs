using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Assignments.Expeditions
{
    public sealed class ExpeditionAssignmentValidator
    {
        public ExpeditionAssignmentValidationResult Validate(
            GuildRuntimeData guild,
            ExpeditionAssignment assignment,
            ExpeditionAreaRegistry areaRegistry,
            CpuSelectionSettings settings,
            IEnumerable<string> defenseCharacterIds = null)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            if (areaRegistry == null) throw new ArgumentNullException(nameof(areaRegistry));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (!areaRegistry.TryGet(assignment.AreaId, out ExpeditionAreaDefinition area))
                return Invalid(ExpeditionAssignmentValidationError.AreaNotFound, assignment.AreaId);
            if (guild.TryGetExpedition(assignment.ExpeditionId, out _))
                return Invalid(
                    ExpeditionAssignmentValidationError.DuplicateExpeditionId,
                    assignment.ExpeditionId);
            if (assignment.ParticipantIds.Count > settings.DesiredExpeditionMembers)
                return Invalid(ExpeditionAssignmentValidationError.TooManyParticipants);

            var reservedIds = defenseCharacterIds == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(defenseCharacterIds, StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var participants = new List<CharacterRuntimeData>(assignment.ParticipantIds.Count);
            for (int index = 0; index < assignment.ParticipantIds.Count; index++)
            {
                string characterId = assignment.ParticipantIds[index];
                if (string.IsNullOrWhiteSpace(characterId))
                    return Invalid(ExpeditionAssignmentValidationError.InvalidCharacterId, characterId);
                if (!ids.Add(characterId))
                    return Invalid(ExpeditionAssignmentValidationError.DuplicateCharacterId, characterId);
                if (reservedIds.Contains(characterId))
                    return Invalid(ExpeditionAssignmentValidationError.ReservedForDefense, characterId);
                if (!guild.TryGetCharacter(characterId, out CharacterRuntimeData character))
                    return Invalid(ExpeditionAssignmentValidationError.CharacterNotFound, characterId);
                if (!CharacterAvailability.CanBeAssigned(character))
                    return Invalid(ExpeditionAssignmentValidationError.CharacterUnavailable, characterId);
                if ((float)character.CurrentHp / character.MaxHp < settings.MinimumHpRatio)
                    return Invalid(ExpeditionAssignmentValidationError.HitPointsBelowMinimum, characterId);

                participants.Add(character);
            }

            int assignableCount = 0;
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                if (CharacterAvailability.CanBeAssigned(guild.Characters[index])) assignableCount++;
            }
            if (assignableCount - participants.Count < settings.MinimumGuildMembersRemaining)
                return Invalid(ExpeditionAssignmentValidationError.InsufficientGuildMembersRemaining);

            return new ExpeditionAssignmentValidationResult(
                ExpeditionAssignmentValidationError.None,
                string.Empty,
                area,
                participants);
        }

        private static ExpeditionAssignmentValidationResult Invalid(
            ExpeditionAssignmentValidationError error,
            string subjectId = "")
        {
            return new ExpeditionAssignmentValidationResult(
                error,
                subjectId,
                null,
                Array.Empty<CharacterRuntimeData>());
        }
    }
}
