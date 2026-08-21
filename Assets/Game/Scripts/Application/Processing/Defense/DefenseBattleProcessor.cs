using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Selection;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Infrastructure.Random;

namespace GuildFrontierSim.Application.Processing.Defense
{
    public sealed class DefenseBattleProcessor
    {
        private readonly CpuMemberSelector memberSelector;
        private readonly BattleResolver battleResolver;
        private readonly IRandomSource randomSource;

        public DefenseBattleProcessor(
            IRandomSource randomSource,
            CpuMemberSelector memberSelector = null,
            BattleResolver battleResolver = null)
        {
            this.randomSource = randomSource ??
                throw new ArgumentNullException(nameof(randomSource));
            this.memberSelector = memberSelector ?? new CpuMemberSelector();
            this.battleResolver = battleResolver ?? new BattleResolver(randomSource);
        }

        public DefenseBattleResult Process(
            GuildRuntimeData guild,
            DefenseBattleRequest request,
            CpuSelectionSettings selectionSettings,
            BattleBalanceSettings battleSettings)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ValidateBattleSettings(battleSettings);

            MemberSelectionResult selection = memberSelector.Select(
                guild,
                new MemberSelectionRequest(
                    MemberSelectionPurpose.Defense,
                    request.ExcludedCharacterIds),
                selectionSettings,
                battleSettings);

            if (selection.SelectedMembers.Count == 0)
            {
                return new DefenseBattleResult(
                    DefenseOutcome.NoDefenders,
                    null,
                    0,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>());
            }

            SetDefending(selection.SelectedMembers);
            try
            {
                BattleResult battleResult = battleResolver.Resolve(
                    new BattleInput(selection.SelectedMembers, request.EnemyBasePower),
                    battleSettings);
                DefenseOutcome outcome = ToDefenseOutcome(battleResult.Outcome);
                int reward = CalculateReward(outcome, battleSettings);
                _ = checked(guild.Funds + reward);

                DefenseBattleResult result = ApplyConsequences(
                    selection.SelectedMembers,
                    battleResult,
                    outcome,
                    reward,
                    battleSettings);
                guild.AddFunds(reward);
                return result;
            }
            catch
            {
                RestoreDefendingMembers(selection.SelectedMembers);
                throw;
            }
        }

        private DefenseBattleResult ApplyConsequences(
            IReadOnlyList<CharacterRuntimeData> defenders,
            BattleResult battleResult,
            DefenseOutcome outcome,
            int reward,
            BattleBalanceSettings settings)
        {
            var defenderIds = new List<string>(defenders.Count);
            var injuredIds = new List<string>();
            var hospitalizedIds = new List<string>();
            var consequences = new List<DefenseConsequence>(defenders.Count);
            float injuryChance = outcome == DefenseOutcome.Victory
                ? settings.VictoryInjuryChance
                : settings.InjuryChance;

            for (int index = 0; index < defenders.Count; index++)
            {
                CharacterRuntimeData defender = defenders[index];
                defenderIds.Add(defender.CharacterId);

                if (NextChance() >= injuryChance)
                {
                    consequences.Add(new DefenseConsequence(
                        defender,
                        CharacterStatus.Available,
                        0));
                    continue;
                }

                int recoveryTurns = randomSource.Range(
                    settings.MinimumRecoveryTurns,
                    checked(settings.MaximumRecoveryTurns + 1));
                if (NextChance() < settings.HospitalizationChance)
                {
                    consequences.Add(new DefenseConsequence(
                        defender,
                        CharacterStatus.Hospitalized,
                        recoveryTurns));
                    hospitalizedIds.Add(defender.CharacterId);
                }
                else
                {
                    consequences.Add(new DefenseConsequence(
                        defender,
                        CharacterStatus.Injured,
                        recoveryTurns));
                    injuredIds.Add(defender.CharacterId);
                }
            }

            for (int index = 0; index < consequences.Count; index++)
            {
                DefenseConsequence consequence = consequences[index];
                consequence.Character.SetStatus(consequence.Status, consequence.RecoveryTurns);
            }

            return new DefenseBattleResult(
                outcome,
                battleResult,
                reward,
                defenderIds,
                injuredIds,
                hospitalizedIds);
        }

        private int CalculateReward(
            DefenseOutcome outcome,
            BattleBalanceSettings settings)
        {
            int baseReward = randomSource.Range(
                settings.MinimumDefenseReward,
                checked(settings.MaximumDefenseReward + 1));
            if (outcome == DefenseOutcome.Victory)
            {
                return baseReward;
            }

            return (int)Math.Round(
                baseReward * settings.DefeatDefenseRewardMultiplier,
                MidpointRounding.AwayFromZero);
        }

        private float NextChance()
        {
            float value = randomSource.Value;
            if (value < 0f || value > 1f || float.IsNaN(value))
            {
                throw new InvalidOperationException("Random value must be between 0 and 1.");
            }

            return value;
        }

        private static DefenseOutcome ToDefenseOutcome(BattleOutcome battleOutcome)
        {
            return battleOutcome == BattleOutcome.Victory
                ? DefenseOutcome.Victory
                : DefenseOutcome.RepelledWithLoss;
        }

        private static void SetDefending(IReadOnlyList<CharacterRuntimeData> defenders)
        {
            for (int index = 0; index < defenders.Count; index++)
            {
                defenders[index].SetStatus(CharacterStatus.Defending);
            }
        }

        private static void RestoreDefendingMembers(
            IReadOnlyList<CharacterRuntimeData> defenders)
        {
            for (int index = 0; index < defenders.Count; index++)
            {
                if (defenders[index].Status == CharacterStatus.Defending)
                {
                    defenders[index].SetStatus(CharacterStatus.Available);
                }
            }
        }

        private static void ValidateBattleSettings(BattleBalanceSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.MaximumDefenseReward < settings.MinimumDefenseReward ||
                settings.MaximumDefenseReward == int.MaxValue ||
                settings.MaximumRecoveryTurns < settings.MinimumRecoveryTurns ||
                settings.MaximumRecoveryTurns == int.MaxValue)
            {
                throw new ArgumentException("Defense settings are invalid.", nameof(settings));
            }
        }

        private readonly struct DefenseConsequence
        {
            public DefenseConsequence(
                CharacterRuntimeData character,
                CharacterStatus status,
                int recoveryTurns)
            {
                Character = character;
                Status = status;
                RecoveryTurns = recoveryTurns;
            }

            public CharacterRuntimeData Character { get; }
            public CharacterStatus Status { get; }
            public int RecoveryTurns { get; }
        }
    }
}
