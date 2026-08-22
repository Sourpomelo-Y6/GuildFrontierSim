using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Assignments.Defense
{
    public sealed class DefenseAssignmentValidator
    {
        public DefenseAssignmentValidationResult Validate(
            GuildRuntimeData guild,
            DefenseAssignment assignment,
            CpuSelectionSettings settings)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (assignment.DefenderIds.Count > settings.DesiredDefenseMembers)
            {
                return Invalid(DefenseAssignmentValidationError.TooManyDefenders);
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var defenders = new List<CharacterRuntimeData>(assignment.DefenderIds.Count);
            for (int index = 0; index < assignment.DefenderIds.Count; index++)
            {
                string characterId = assignment.DefenderIds[index];
                if (string.IsNullOrWhiteSpace(characterId))
                {
                    return Invalid(
                        DefenseAssignmentValidationError.InvalidCharacterId,
                        characterId);
                }

                if (!ids.Add(characterId))
                {
                    return Invalid(
                        DefenseAssignmentValidationError.DuplicateCharacterId,
                        characterId);
                }

                if (!guild.TryGetCharacter(characterId, out CharacterRuntimeData character))
                {
                    return Invalid(
                        DefenseAssignmentValidationError.CharacterNotFound,
                        characterId);
                }

                if (!CharacterAvailability.CanBeAssigned(character))
                {
                    return Invalid(
                        DefenseAssignmentValidationError.CharacterUnavailable,
                        characterId);
                }

                if ((float)character.CurrentHp / character.MaxHp < settings.MinimumHpRatio)
                {
                    return Invalid(
                        DefenseAssignmentValidationError.HitPointsBelowMinimum,
                        characterId);
                }

                defenders.Add(character);
            }

            return new DefenseAssignmentValidationResult(
                DefenseAssignmentValidationError.None,
                string.Empty,
                defenders);
        }

        private static DefenseAssignmentValidationResult Invalid(
            DefenseAssignmentValidationError error,
            string characterId = "")
        {
            return new DefenseAssignmentValidationResult(
                error,
                characterId,
                Array.Empty<CharacterRuntimeData>());
        }
    }
}
