using System;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Domain.Characters;

namespace GuildFrontierSim.Application.Factories
{
    public sealed class CharacterRuntimeDataFactory
    {
        public CharacterRuntimeData Create(
            CharacterDefinition definition,
            bool isPlayerCharacter = false)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new CharacterRuntimeData(
                definition.Id,
                definition.StartingLevel,
                definition.MaxHp,
                definition.Attack,
                definition.Defense,
                definition.Speed,
                definition.Salary,
                definition.StartingLoyalty,
                isPlayerCharacter);
        }
    }
}
