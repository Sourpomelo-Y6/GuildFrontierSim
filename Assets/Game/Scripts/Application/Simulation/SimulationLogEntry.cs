using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Application.Simulation
{
    public sealed class SimulationLogEntry
    {
        private readonly List<string> relatedCharacterIds;

        public SimulationLogEntry(
            int turnNumber,
            SimulationLogCategory category,
            string message,
            IEnumerable<string> relatedCharacterIds = null)
        {
            if (turnNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turnNumber));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Log message cannot be empty.", nameof(message));
            }

            TurnNumber = turnNumber;
            Category = category;
            Message = message.Trim();
            this.relatedCharacterIds = new List<string>(
                relatedCharacterIds ?? Array.Empty<string>());
        }

        public int TurnNumber { get; }
        public SimulationLogCategory Category { get; }
        public string Message { get; }
        public IReadOnlyList<string> RelatedCharacterIds => relatedCharacterIds;
    }
}
