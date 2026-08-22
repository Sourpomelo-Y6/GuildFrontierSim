using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Planning
{
    public sealed class GuildControlPolicy
    {
        private readonly HashSet<TurnDecisionType> automatedDecisions;

        public GuildControlPolicy(
            GuildControlMode mode,
            string playerCharacterId = "",
            bool canDelegateToCpu = true,
            IEnumerable<TurnDecisionType> automatedDecisions = null)
        {
            if (mode == GuildControlMode.Player &&
                string.IsNullOrWhiteSpace(playerCharacterId))
            {
                throw new ArgumentException(
                    "Player character ID is required in player mode.",
                    nameof(playerCharacterId));
            }

            Mode = mode;
            PlayerCharacterId = playerCharacterId?.Trim() ?? string.Empty;
            CanDelegateToCpu = canDelegateToCpu;
            this.automatedDecisions = automatedDecisions == null
                ? new HashSet<TurnDecisionType>()
                : new HashSet<TurnDecisionType>(automatedDecisions);
        }

        public GuildControlMode Mode { get; }
        public string PlayerCharacterId { get; }
        public bool CanDelegateToCpu { get; }

        public bool HasPlayerAuthority(GuildRuntimeData guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (Mode != GuildControlMode.Player ||
                !guild.TryGetCharacter(PlayerCharacterId, out CharacterRuntimeData player) ||
                !CharacterAvailability.CanBeAssigned(player))
            {
                return false;
            }

            return string.Equals(guild.LeaderCharacterId, PlayerCharacterId, StringComparison.Ordinal) ||
                   string.Equals(guild.ActingLeaderCharacterId, PlayerCharacterId, StringComparison.Ordinal);
        }

        public bool ShouldUseCpu(GuildRuntimeData guild, TurnDecisionType decision)
        {
            return Mode == GuildControlMode.Cpu ||
                   !HasPlayerAuthority(guild) ||
                   automatedDecisions.Contains(decision);
        }
    }
}
