using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Processing.Economy
{
    public sealed class SalaryProcessor
    {
        public SalaryResult Process(
            GuildRuntimeData guild,
            BattleBalanceSettings settings)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            ValidateSettings(settings);
            if (guild.CurrentTurn == 0 ||
                guild.CurrentTurn % settings.SalaryIntervalTurns != 0)
            {
                return new SalaryResult(
                    SalaryOutcome.NotDue,
                    0,
                    Array.Empty<string>());
            }

            var characterIds = new List<string>(guild.Characters.Count);
            int totalSalary = 0;
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                characterIds.Add(character.CharacterId);
                totalSalary = checked(totalSalary + character.Salary);
            }

            if (!guild.TrySpendFunds(totalSalary))
            {
                return new SalaryResult(
                    SalaryOutcome.Unpaid,
                    totalSalary,
                    characterIds);
            }

            return new SalaryResult(
                SalaryOutcome.Paid,
                totalSalary,
                characterIds);
        }

        private static void ValidateSettings(BattleBalanceSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.SalaryIntervalTurns < 1)
            {
                throw new ArgumentException(
                    "Salary interval must be at least one turn.",
                    nameof(settings));
            }
        }
    }
}
