using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Presets;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Factories
{
    public sealed class GuildRuntimeDataFactory
    {
        private readonly CharacterRuntimeDataFactory characterFactory;

        public GuildRuntimeDataFactory(CharacterRuntimeDataFactory characterFactory = null)
        {
            this.characterFactory = characterFactory ?? new CharacterRuntimeDataFactory();
        }

        public GuildRuntimeData Create(GuildStartingPreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            CharacterDefinition[] memberDefinitions = preset.StartingMembers;
            if (memberDefinitions == null)
            {
                throw new InvalidOperationException("Starting member definitions cannot be null.");
            }

            var characters = new List<CharacterRuntimeData>(memberDefinitions.Length);
            foreach (CharacterDefinition definition in memberDefinitions)
            {
                if (definition == null)
                {
                    throw new InvalidOperationException("Starting members cannot contain null.");
                }

                characters.Add(characterFactory.Create(definition));
            }

            return new GuildRuntimeData(
                preset.GuildName,
                preset.StartingFunds,
                characters,
                preset.StartingLeaderId);
        }
    }
}
