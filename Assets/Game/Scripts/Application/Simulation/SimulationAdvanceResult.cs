using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Processing.Turns;

namespace GuildFrontierSim.Application.Simulation
{
    public sealed class SimulationAdvanceResult
    {
        public SimulationAdvanceResult(
            TurnResult turnResult,
            IEnumerable<SimulationLogEntry> logs)
        {
            TurnResult = turnResult ?? throw new ArgumentNullException(nameof(turnResult));
            Logs = new List<SimulationLogEntry>(
                logs ?? throw new ArgumentNullException(nameof(logs)));
        }

        public TurnResult TurnResult { get; }
        public IReadOnlyList<SimulationLogEntry> Logs { get; }
    }
}
