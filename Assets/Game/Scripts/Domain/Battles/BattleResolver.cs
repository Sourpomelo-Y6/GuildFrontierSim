using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Infrastructure.Random;

namespace GuildFrontierSim.Domain.Battles
{
    public sealed class BattleResolver
    {
        private const float DrawTolerance = 0.0001f;

        private readonly BattlePowerCalculator powerCalculator;
        private readonly IRandomSource randomSource;

        public BattleResolver(
            IRandomSource randomSource,
            BattlePowerCalculator powerCalculator = null)
        {
            this.randomSource = randomSource ??
                throw new ArgumentNullException(nameof(randomSource));
            this.powerCalculator = powerCalculator ?? new BattlePowerCalculator();
        }

        public BattleResult Resolve(BattleInput input, BattleBalanceSettings settings)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateSettings(settings);

            float partyBasePower = powerCalculator.Calculate(input.Participants, settings);
            float partyMultiplier = NextMultiplier(settings);
            float enemyMultiplier = NextMultiplier(settings);
            float finalPartyPower = partyBasePower * partyMultiplier;
            float finalEnemyPower = input.EnemyBasePower * enemyMultiplier;
            BattleOutcome outcome = DetermineOutcome(finalPartyPower, finalEnemyPower);

            var participantIds = new List<string>(input.Participants.Count);
            for (int index = 0; index < input.Participants.Count; index++)
            {
                participantIds.Add(input.Participants[index].CharacterId);
            }

            return new BattleResult(
                outcome,
                partyBasePower,
                input.EnemyBasePower,
                partyMultiplier,
                enemyMultiplier,
                participantIds);
        }

        private float NextMultiplier(BattleBalanceSettings settings)
        {
            float value = randomSource.Value;
            if (value < 0f || value > 1f || float.IsNaN(value))
            {
                throw new InvalidOperationException("Random value must be between 0 and 1.");
            }

            return settings.MinimumPowerMultiplier +
                   (settings.MaximumPowerMultiplier - settings.MinimumPowerMultiplier) * value;
        }

        private static BattleOutcome DetermineOutcome(float partyPower, float enemyPower)
        {
            float difference = partyPower - enemyPower;
            if (Math.Abs(difference) <= DrawTolerance)
            {
                return BattleOutcome.Draw;
            }

            return difference > 0f ? BattleOutcome.Victory : BattleOutcome.Defeat;
        }

        private static void ValidateSettings(BattleBalanceSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.MinimumPowerMultiplier < 0f ||
                settings.MaximumPowerMultiplier < settings.MinimumPowerMultiplier ||
                float.IsNaN(settings.MinimumPowerMultiplier) ||
                float.IsNaN(settings.MaximumPowerMultiplier))
            {
                throw new ArgumentException("Power multiplier settings are invalid.", nameof(settings));
            }
        }
    }
}
