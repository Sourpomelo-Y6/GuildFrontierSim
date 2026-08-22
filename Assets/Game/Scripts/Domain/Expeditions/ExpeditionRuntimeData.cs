using System;
using System.Collections.Generic;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Domain.Expeditions
{
    public sealed class ExpeditionRuntimeData
    {
        private readonly List<string> participantIds;
        private readonly List<string> rescuedCharacterIds = new List<string>();

        public ExpeditionRuntimeData(
            string expeditionId,
            string areaId,
            IEnumerable<string> participantIds,
            float enemyBasePower,
            int maximumStages,
            float rewardMultiplier,
            bool canContainCaptives)
        {
            ExpeditionId = ValidateId(expeditionId, nameof(expeditionId));
            AreaId = ValidateId(areaId, nameof(areaId));

            if (participantIds == null)
            {
                throw new ArgumentNullException(nameof(participantIds));
            }

            if (enemyBasePower < 0f || float.IsNaN(enemyBasePower) || float.IsInfinity(enemyBasePower))
            {
                throw new ArgumentOutOfRangeException(nameof(enemyBasePower));
            }

            if (maximumStages < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumStages));
            }

            if (rewardMultiplier < 0f || float.IsNaN(rewardMultiplier) || float.IsInfinity(rewardMultiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(rewardMultiplier));
            }

            this.participantIds = new List<string>();
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string participantId in participantIds)
            {
                string validatedId = ValidateId(participantId, nameof(participantIds));
                if (!uniqueIds.Add(validatedId))
                {
                    throw new ArgumentException(
                        $"Duplicate expedition participant ID: {validatedId}",
                        nameof(participantIds));
                }

                this.participantIds.Add(validatedId);
            }

            if (this.participantIds.Count == 0)
            {
                throw new ArgumentException(
                    "An expedition requires at least one participant.",
                    nameof(participantIds));
            }

            EnemyBasePower = enemyBasePower;
            MaximumStages = maximumStages;
            RewardMultiplier = rewardMultiplier;
            CanContainCaptives = canContainCaptives;
            CurrentStage = 1;
            Status = ExpeditionStatus.Active;
            TemporaryInventory = new GuildInventory();
        }

        public string ExpeditionId { get; }
        public string AreaId { get; }
        public IReadOnlyList<string> ParticipantIds => participantIds;
        public float EnemyBasePower { get; }
        public int MaximumStages { get; }
        public float RewardMultiplier { get; }
        public bool CanContainCaptives { get; }
        public int CurrentStage { get; private set; }
        public int TemporaryFunds { get; private set; }
        public GuildInventory TemporaryInventory { get; }
        public IReadOnlyList<string> RescuedCharacterIds => rescuedCharacterIds;
        public ExpeditionStatus Status { get; private set; }

        public void AddTemporaryFunds(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            TemporaryFunds = checked(TemporaryFunds + amount);
        }

        public void AddRescuedCharacter(string characterId)
        {
            string validatedId = ValidateId(characterId, nameof(characterId));
            if (rescuedCharacterIds.Contains(validatedId))
            {
                return;
            }

            rescuedCharacterIds.Add(validatedId);
        }

        public void RetainTemporaryLoot(float ratio)
        {
            if (ratio < 0f || ratio > 1f || float.IsNaN(ratio))
            {
                throw new ArgumentOutOfRangeException(nameof(ratio));
            }

            TemporaryFunds = (int)Math.Floor(TemporaryFunds * ratio);
            TemporaryInventory.RetainFraction(ratio);
        }

        public void DiscardTemporaryLoot()
        {
            TemporaryFunds = 0;
            TemporaryInventory.Clear();
            rescuedCharacterIds.Clear();
        }

        public void ConsumeTemporaryLoot()
        {
            TemporaryFunds = 0;
            TemporaryInventory.Clear();
        }

        public void AdvanceStage()
        {
            EnsureActive();
            if (CurrentStage >= MaximumStages)
            {
                throw new InvalidOperationException("The expedition is already at its final stage.");
            }

            CurrentStage++;
        }

        public void BeginReturn()
        {
            EnsureActive();
            Status = ExpeditionStatus.Returning;
        }

        public void BeginDecision()
        {
            EnsureActive();
            Status = ExpeditionStatus.AwaitingDecision;
        }

        public void ContinueAfterDecision()
        {
            EnsureAwaitingDecision();
            if (CurrentStage >= MaximumStages)
                throw new InvalidOperationException("The expedition is already at its final stage.");
            CurrentStage++;
            Status = ExpeditionStatus.Active;
        }

        public void ReturnAfterDecision()
        {
            EnsureAwaitingDecision();
            Status = ExpeditionStatus.Returning;
        }

        public void Complete()
        {
            if (Status != ExpeditionStatus.Returning)
            {
                throw new InvalidOperationException("Only a returning expedition can complete.");
            }

            Status = ExpeditionStatus.Completed;
        }

        public void MarkCaptured()
        {
            EnsureActive();
            Status = ExpeditionStatus.Captured;
        }

        private void EnsureActive()
        {
            if (Status != ExpeditionStatus.Active)
            {
                throw new InvalidOperationException("The expedition is not active.");
            }
        }

        private void EnsureAwaitingDecision()
        {
            if (Status != ExpeditionStatus.AwaitingDecision)
                throw new InvalidOperationException("The expedition is not awaiting a decision.");
        }

        private static string ValidateId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ID cannot be empty.", parameterName);
            }

            return value.Trim();
        }
    }
}
