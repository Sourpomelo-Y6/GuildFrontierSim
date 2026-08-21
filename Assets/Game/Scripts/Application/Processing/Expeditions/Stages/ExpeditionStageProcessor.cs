using System;
using System.Collections.Generic;
using System.Linq;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using GuildFrontierSim.Infrastructure.Random;

namespace GuildFrontierSim.Application.Processing.Expeditions.Stages
{
    public sealed class ExpeditionStageProcessor
    {
        private readonly IRandomSource randomSource;
        private readonly BattleResolver battleResolver;
        private readonly ExpeditionDecisionPolicy decisionPolicy;

        public ExpeditionStageProcessor(
            IRandomSource randomSource,
            BattleResolver battleResolver = null,
            ExpeditionDecisionPolicy decisionPolicy = null)
        {
            this.randomSource = randomSource ??
                throw new ArgumentNullException(nameof(randomSource));
            this.battleResolver = battleResolver ?? new BattleResolver(randomSource);
            this.decisionPolicy = decisionPolicy ?? new ExpeditionDecisionPolicy();
        }

        public ExpeditionStageResult ProcessStage(
            GuildRuntimeData guild,
            string expeditionId,
            BattleBalanceSettings battleSettings,
            ExpeditionBalanceSettings expeditionSettings)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            ValidateSettings(expeditionSettings);
            if (!guild.TryGetExpedition(expeditionId, out ExpeditionRuntimeData expedition))
            {
                throw new ArgumentException("Expedition was not found.", nameof(expeditionId));
            }

            if (expedition.Status != ExpeditionStatus.Active)
            {
                throw new InvalidOperationException("Only an active expedition can process a stage.");
            }

            List<CharacterRuntimeData> participants = ResolveParticipants(guild, expedition);
            float enemyPower = CalculateEnemyPower(expedition, expeditionSettings);
            BattleResult battleResult = battleResolver.Resolve(
                new BattleInput(participants, enemyPower),
                battleSettings);

            return battleResult.Outcome == BattleOutcome.Victory
                ? ProcessVictory(guild, expedition, participants, battleResult, expeditionSettings)
                : ProcessDefeat(expedition, participants, battleResult, expeditionSettings);
        }

        private ExpeditionStageResult ProcessVictory(
            GuildRuntimeData guild,
            ExpeditionRuntimeData expedition,
            IReadOnlyList<CharacterRuntimeData> participants,
            BattleResult battleResult,
            ExpeditionBalanceSettings settings)
        {
            int baseReward = randomSource.Range(
                settings.MinimumStageFunds,
                checked(settings.MaximumStageFunds + 1));
            int reward = checked((int)Math.Round(
                baseReward * expedition.RewardMultiplier,
                MidpointRounding.AwayFromZero));
            string rescuedId = DetermineRescuedCharacterId(guild, expedition, settings);

            expedition.AddTemporaryFunds(reward);
            if (!string.IsNullOrEmpty(rescuedId))
            {
                expedition.AddRescuedCharacter(rescuedId);
            }

            ExpeditionDecision decision = decisionPolicy.Decide(expedition, participants, settings);
            ExpeditionStageOutcome outcome;
            if (decision == ExpeditionDecision.Return)
            {
                expedition.BeginReturn();
                outcome = ExpeditionStageOutcome.VictoryReturning;
            }
            else
            {
                expedition.AdvanceStage();
                outcome = ExpeditionStageOutcome.VictoryContinued;
            }

            return new ExpeditionStageResult(
                expedition.ExpeditionId,
                outcome,
                battleResult,
                reward,
                expedition.TemporaryFunds,
                rescuedId);
        }

        private ExpeditionStageResult ProcessDefeat(
            ExpeditionRuntimeData expedition,
            IReadOnlyList<CharacterRuntimeData> participants,
            BattleResult battleResult,
            ExpeditionBalanceSettings settings)
        {
            bool escaped = NextChance() < settings.EscapeChance;
            ExpeditionStageOutcome outcome;
            if (escaped)
            {
                expedition.RetainTemporaryLoot(settings.EscapedLootRetentionRatio);
                expedition.BeginReturn();
                outcome = ExpeditionStageOutcome.EscapedReturning;
            }
            else
            {
                expedition.DiscardTemporaryLoot();
                expedition.MarkCaptured();
                for (int index = 0; index < participants.Count; index++)
                {
                    participants[index].SetStatus(CharacterStatus.Captured);
                }

                outcome = ExpeditionStageOutcome.Captured;
            }

            return new ExpeditionStageResult(
                expedition.ExpeditionId,
                outcome,
                battleResult,
                0,
                expedition.TemporaryFunds);
        }

        private string DetermineRescuedCharacterId(
            GuildRuntimeData guild,
            ExpeditionRuntimeData expedition,
            ExpeditionBalanceSettings settings)
        {
            if (!expedition.CanContainCaptives)
            {
                return string.Empty;
            }

            var candidates = new List<string>();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                if (character.Status == CharacterStatus.Captured &&
                    !expedition.RescuedCharacterIds.Contains(character.CharacterId))
                {
                    candidates.Add(character.CharacterId);
                }
            }

            if (candidates.Count == 0 || NextChance() >= settings.CaptiveRescueChance)
            {
                return string.Empty;
            }

            candidates.Sort(StringComparer.Ordinal);
            return candidates[0];
        }

        private static List<CharacterRuntimeData> ResolveParticipants(
            GuildRuntimeData guild,
            ExpeditionRuntimeData expedition)
        {
            var participants = new List<CharacterRuntimeData>(expedition.ParticipantIds.Count);
            for (int index = 0; index < expedition.ParticipantIds.Count; index++)
            {
                string participantId = expedition.ParticipantIds[index];
                if (!guild.TryGetCharacter(participantId, out CharacterRuntimeData participant) ||
                    participant.Status != CharacterStatus.Expedition)
                {
                    throw new InvalidOperationException(
                        $"Expedition participant is unavailable: {participantId}");
                }

                participants.Add(participant);
            }

            return participants;
        }

        private static float CalculateEnemyPower(
            ExpeditionRuntimeData expedition,
            ExpeditionBalanceSettings settings)
        {
            return expedition.EnemyBasePower *
                   (1f + settings.EnemyPowerGrowthPerStage * (expedition.CurrentStage - 1));
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

        private static void ValidateSettings(ExpeditionBalanceSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.MaximumStageFunds < settings.MinimumStageFunds ||
                settings.MaximumStageFunds == int.MaxValue)
            {
                throw new ArgumentException("Expedition reward settings are invalid.", nameof(settings));
            }
        }
    }
}
