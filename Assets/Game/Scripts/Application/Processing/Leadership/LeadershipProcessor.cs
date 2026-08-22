using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Assignments.Leadership;
using GuildFrontierSim.Application.Processing.Economy;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Processing.Leadership
{
    public sealed class LeadershipProcessor
    {
        private readonly BattlePowerCalculator powerCalculator;
        private readonly LoyaltyProcessor loyaltyProcessor;
        private readonly ActingLeaderAssignmentValidator assignmentValidator;

        public LeadershipProcessor(
            BattlePowerCalculator powerCalculator = null,
            LoyaltyProcessor loyaltyProcessor = null,
            ActingLeaderAssignmentValidator assignmentValidator = null)
        {
            this.powerCalculator = powerCalculator ?? new BattlePowerCalculator();
            this.loyaltyProcessor = loyaltyProcessor ?? new LoyaltyProcessor();
            this.assignmentValidator = assignmentValidator ??
                new ActingLeaderAssignmentValidator();
        }

        public LeadershipResult Process(
            GuildRuntimeData guild,
            BattleBalanceSettings settings,
            ActingLeaderAssignment assignment,
            int guildRevision)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            ActingLeaderValidationResult validation = assignmentValidator.Validate(
                guild,
                assignment,
                guildRevision);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    $"Invalid acting leader assignment: {validation.Error}.");
            }

            string previousLeaderId = guild.LeaderCharacterId;
            guild.SetActingLeader(validation.Candidate.CharacterId);
            guild.MarkStateChanged();
            return CreateResult(
                LeadershipOutcome.ActingLeaderAssigned,
                previousLeaderId,
                guild,
                null);
        }

        public LeadershipResult Process(
            GuildRuntimeData guild,
            BattleBalanceSettings settings)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            CharacterRuntimeData leader = guild.Leader;
            string previousLeaderId = leader.CharacterId;
            if (leader.IsDeparturePending)
            {
                return ReplaceLeader(guild, leader, settings);
            }

            if (CharacterAvailability.CanBeAssigned(leader))
            {
                guild.SetActingLeader(string.Empty);
                return CreateResult(
                    LeadershipOutcome.LeaderContinues,
                    previousLeaderId,
                    guild,
                    null);
            }

            CharacterRuntimeData actingLeader = SelectCandidate(
                guild,
                leader.CharacterId,
                settings);
            if (actingLeader == null)
            {
                guild.SetActingLeader(string.Empty);
                return CreateResult(
                    LeadershipOutcome.NoCandidate,
                    previousLeaderId,
                    guild,
                    null);
            }

            guild.SetActingLeader(actingLeader.CharacterId);
            return CreateResult(
                LeadershipOutcome.ActingLeaderAssigned,
                previousLeaderId,
                guild,
                null);
        }

        private LeadershipResult ReplaceLeader(
            GuildRuntimeData guild,
            CharacterRuntimeData previousLeader,
            BattleBalanceSettings settings)
        {
            CharacterRuntimeData replacement = SelectCandidate(
                guild,
                previousLeader.CharacterId,
                settings);
            if (replacement == null)
            {
                guild.SetActingLeader(string.Empty);
                return CreateResult(
                    LeadershipOutcome.NoCandidate,
                    previousLeader.CharacterId,
                    guild,
                    null);
            }

            guild.SetLeader(replacement.CharacterId);
            guild.SetActingLeader(string.Empty);
            LoyaltyResult departureResult =
                loyaltyProcessor.ResolvePendingDepartures(guild);
            return CreateResult(
                LeadershipOutcome.LeaderReplaced,
                previousLeader.CharacterId,
                guild,
                departureResult);
        }

        private CharacterRuntimeData SelectCandidate(
            GuildRuntimeData guild,
            string excludedCharacterId,
            BattleBalanceSettings settings)
        {
            CharacterRuntimeData best = null;
            float bestPower = 0f;
            IReadOnlyList<CharacterRuntimeData> candidates =
                assignmentValidator.GetCandidates(guild);
            for (int index = 0; index < candidates.Count; index++)
            {
                CharacterRuntimeData candidate = candidates[index];
                if (string.Equals(
                    candidate.CharacterId,
                    excludedCharacterId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                float candidatePower = powerCalculator.Calculate(
                    new[] { candidate },
                    settings);
                if (best == null || IsBetter(candidate, candidatePower, best, bestPower))
                {
                    best = candidate;
                    bestPower = candidatePower;
                }
            }

            return best;
        }

        private static bool IsBetter(
            CharacterRuntimeData candidate,
            float candidatePower,
            CharacterRuntimeData currentBest,
            float currentBestPower)
        {
            if (candidate.Level != currentBest.Level)
            {
                return candidate.Level > currentBest.Level;
            }

            int powerComparison = candidatePower.CompareTo(currentBestPower);
            if (powerComparison != 0)
            {
                return powerComparison > 0;
            }

            if (candidate.Loyalty != currentBest.Loyalty)
            {
                return candidate.Loyalty > currentBest.Loyalty;
            }

            return string.CompareOrdinal(
                candidate.CharacterId,
                currentBest.CharacterId) < 0;
        }

        private static LeadershipResult CreateResult(
            LeadershipOutcome outcome,
            string previousLeaderId,
            GuildRuntimeData guild,
            LoyaltyResult departureResult)
        {
            return new LeadershipResult(
                outcome,
                previousLeaderId,
                guild.LeaderCharacterId,
                guild.ActingLeaderCharacterId,
                departureResult?.DepartedCharacterIds ?? Array.Empty<string>(),
                departureResult?.PendingDepartureCharacterIds ?? Array.Empty<string>());
        }
    }
}
