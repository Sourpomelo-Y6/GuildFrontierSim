using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Selection;
using GuildFrontierSim.Data.Definitions;
using GuildFrontierSim.Data.Settings;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;

namespace GuildFrontierSim.Application.Processing.Expeditions
{
    public sealed class ExpeditionProcessor
    {
        private readonly CpuMemberSelector memberSelector;

        public ExpeditionProcessor(CpuMemberSelector memberSelector = null)
        {
            this.memberSelector = memberSelector ?? new CpuMemberSelector();
        }

        public ExpeditionStartResult Start(
            GuildRuntimeData guild,
            ExpeditionStartRequest request,
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

            ValidateArea(request.Area);

            MemberSelectionResult selection = memberSelector.Select(
                guild,
                new MemberSelectionRequest(
                    MemberSelectionPurpose.Expedition,
                    request.ExcludedCharacterIds),
                selectionSettings,
                battleSettings);

            if (selection.SelectedMembers.Count == 0)
            {
                return new ExpeditionStartResult(
                    ExpeditionStartOutcome.NoMembersAvailable,
                    null,
                    Array.Empty<string>(),
                    selection.RequestedCount);
            }

            var participantIds = new List<string>(selection.SelectedMembers.Count);
            for (int index = 0; index < selection.SelectedMembers.Count; index++)
            {
                CharacterRuntimeData participant = selection.SelectedMembers[index];
                participantIds.Add(participant.CharacterId);
                participant.SetStatus(CharacterStatus.Expedition);
            }

            try
            {
                var expedition = new ExpeditionRuntimeData(
                    request.ExpeditionId,
                    request.Area.Id,
                    participantIds,
                    request.Area.EnemyPower,
                    request.Area.MaximumStages,
                    request.Area.RewardMultiplier,
                    request.Area.CanContainCaptives);
                guild.AddExpedition(expedition);

                return new ExpeditionStartResult(
                    ExpeditionStartOutcome.Started,
                    expedition,
                    participantIds,
                    selection.RequestedCount);
            }
            catch
            {
                RestoreParticipants(selection.SelectedMembers);
                throw;
            }
        }

        private static void RestoreParticipants(
            IReadOnlyList<CharacterRuntimeData> participants)
        {
            for (int index = 0; index < participants.Count; index++)
            {
                if (participants[index].Status == CharacterStatus.Expedition)
                {
                    participants[index].SetStatus(CharacterStatus.Available);
                }
            }
        }

        private static void ValidateArea(ExpeditionAreaDefinition area)
        {
            if (string.IsNullOrWhiteSpace(area.Id))
            {
                throw new ArgumentException("Expedition area ID cannot be empty.", nameof(area));
            }

            if (area.EnemyPower < 0 || area.MaximumStages < 1 || area.RewardMultiplier < 0f)
            {
                throw new ArgumentException("Expedition area settings are invalid.", nameof(area));
            }
        }
    }
}
