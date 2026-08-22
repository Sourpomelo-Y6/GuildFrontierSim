using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Definitions;

namespace GuildFrontierSim.Application.Assignments.Expeditions
{
    public sealed class ExpeditionAreaRegistry
    {
        private readonly Dictionary<string, ExpeditionAreaDefinition> areas;

        public ExpeditionAreaRegistry(IEnumerable<ExpeditionAreaDefinition> areas)
        {
            if (areas == null) throw new ArgumentNullException(nameof(areas));
            this.areas = new Dictionary<string, ExpeditionAreaDefinition>(StringComparer.Ordinal);
            foreach (ExpeditionAreaDefinition area in areas)
            {
                if (area == null || string.IsNullOrWhiteSpace(area.Id))
                    throw new ArgumentException("Areas must have a valid ID.", nameof(areas));
                if (!this.areas.TryAdd(area.Id, area))
                    throw new ArgumentException($"Duplicate area ID: {area.Id}", nameof(areas));
            }
        }

        public bool TryGet(string areaId, out ExpeditionAreaDefinition area)
        {
            if (string.IsNullOrWhiteSpace(areaId))
            {
                area = null;
                return false;
            }

            return areas.TryGetValue(areaId, out area);
        }
    }
}
