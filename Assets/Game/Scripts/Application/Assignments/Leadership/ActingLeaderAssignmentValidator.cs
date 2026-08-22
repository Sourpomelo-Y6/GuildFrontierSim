using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Assignments.Leadership
{
    public sealed class ActingLeaderAssignmentValidator
    {
        public ActingLeaderValidationResult Validate(
            GuildRuntimeData guild,
            ActingLeaderAssignment assignment,
            int guildRevision)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            if (guild.Revision != guildRevision)
                return new ActingLeaderValidationResult(ActingLeaderValidationError.StaleRevision);
            if (guild.Leader.IsDeparturePending)
                return new ActingLeaderValidationResult(
                    ActingLeaderValidationError.LeaderDeparturePending);
            if (CharacterAvailability.CanBeAssigned(guild.Leader))
                return new ActingLeaderValidationResult(ActingLeaderValidationError.LeaderAvailable);
            if (!guild.TryGetCharacter(
                assignment.CharacterId,
                out CharacterRuntimeData candidate))
            {
                return new ActingLeaderValidationResult(
                    ActingLeaderValidationError.CharacterNotFound);
            }
            if (string.Equals(
                candidate.CharacterId,
                guild.LeaderCharacterId,
                StringComparison.Ordinal))
            {
                return new ActingLeaderValidationResult(
                    ActingLeaderValidationError.FormalLeaderSelected);
            }
            if (!CharacterAvailability.CanBeAssigned(candidate))
                return new ActingLeaderValidationResult(
                    ActingLeaderValidationError.CharacterUnavailable);
            return new ActingLeaderValidationResult(
                ActingLeaderValidationError.None,
                candidate);
        }

        public IReadOnlyList<CharacterRuntimeData> GetCandidates(GuildRuntimeData guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            var candidates = new List<CharacterRuntimeData>();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData candidate = guild.Characters[index];
                if (!string.Equals(
                        candidate.CharacterId,
                        guild.LeaderCharacterId,
                        StringComparison.Ordinal) &&
                    CharacterAvailability.CanBeAssigned(candidate))
                {
                    candidates.Add(candidate);
                }
            }
            return candidates;
        }
    }
}
