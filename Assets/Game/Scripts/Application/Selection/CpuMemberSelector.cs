using System;
using System.Collections.Generic;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Battles;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Selection
{
    public sealed class CpuMemberSelector
    {
        private readonly BattlePowerCalculator powerCalculator;

        public CpuMemberSelector(BattlePowerCalculator powerCalculator = null)
        {
            this.powerCalculator = powerCalculator ?? new BattlePowerCalculator();
        }

        public MemberSelectionResult Select(
            GuildRuntimeData guild,
            MemberSelectionRequest request,
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

            if (selectionSettings == null)
            {
                throw new ArgumentNullException(nameof(selectionSettings));
            }

            if (battleSettings == null)
            {
                throw new ArgumentNullException(nameof(battleSettings));
            }

            int requestedCount = GetRequestedCount(request.Purpose, selectionSettings);
            var candidates = BuildRankedCandidates(
                guild,
                request,
                selectionSettings.MinimumHpRatio,
                battleSettings);
            int selectableCount = GetSelectableCount(
                request.Purpose,
                candidates.Count,
                selectionSettings.MinimumGuildMembersRemaining);
            int resultCount = Math.Min(requestedCount, selectableCount);

            var selected = new List<CharacterRuntimeData>(resultCount);
            for (int index = 0; index < resultCount; index++)
            {
                selected.Add(candidates[index].Character);
            }

            return new MemberSelectionResult(request.Purpose, requestedCount, selected);
        }

        private List<RankedCandidate> BuildRankedCandidates(
            GuildRuntimeData guild,
            MemberSelectionRequest request,
            float minimumHpRatio,
            BattleBalanceSettings battleSettings)
        {
            var candidates = new List<RankedCandidate>();
            for (int index = 0; index < guild.Characters.Count; index++)
            {
                CharacterRuntimeData character = guild.Characters[index];
                if (!CharacterAvailability.CanBeAssigned(character) ||
                    request.IsExcluded(character.CharacterId) ||
                    GetHpRatio(character) < minimumHpRatio)
                {
                    continue;
                }

                float power = powerCalculator.Calculate(new[] { character }, battleSettings);
                candidates.Add(new RankedCandidate(character, power));
            }

            candidates.Sort(CompareCandidates);
            return candidates;
        }

        private static int CompareCandidates(RankedCandidate left, RankedCandidate right)
        {
            int powerComparison = right.Power.CompareTo(left.Power);
            return powerComparison != 0
                ? powerComparison
                : string.CompareOrdinal(left.Character.CharacterId, right.Character.CharacterId);
        }

        private static int GetRequestedCount(
            MemberSelectionPurpose purpose,
            CpuSelectionSettings settings)
        {
            return purpose == MemberSelectionPurpose.Defense
                ? settings.DesiredDefenseMembers
                : settings.DesiredExpeditionMembers;
        }

        private static int GetSelectableCount(
            MemberSelectionPurpose purpose,
            int candidateCount,
            int minimumGuildMembersRemaining)
        {
            if (purpose == MemberSelectionPurpose.Defense)
            {
                return candidateCount;
            }

            return Math.Max(0, candidateCount - minimumGuildMembersRemaining);
        }

        private static float GetHpRatio(CharacterRuntimeData character)
        {
            return (float)character.CurrentHp / character.MaxHp;
        }

        private readonly struct RankedCandidate
        {
            public RankedCandidate(CharacterRuntimeData character, float power)
            {
                Character = character;
                Power = power;
            }

            public CharacterRuntimeData Character { get; }
            public float Power { get; }
        }
    }
}
