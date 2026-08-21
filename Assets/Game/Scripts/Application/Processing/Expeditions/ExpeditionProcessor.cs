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

        public ExpeditionReturnResult CompleteReturn(
            GuildRuntimeData guild,
            string expeditionId)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (!guild.TryGetExpedition(expeditionId, out ExpeditionRuntimeData expedition))
            {
                throw new ArgumentException("Expedition was not found.", nameof(expeditionId));
            }

            if (expedition.Status != ExpeditionStatus.Returning)
            {
                throw new InvalidOperationException("Only a returning expedition can be completed.");
            }

            List<CharacterRuntimeData> participants = ResolveParticipants(guild, expedition);
            List<CharacterRuntimeData> rescuedCharacters = ResolveRescuedCharacters(guild, expedition);

            checked
            {
                _ = guild.Funds + expedition.TemporaryFunds;
            }

            guild.Inventory.EnsureCanAdd(expedition.TemporaryInventory.Quantities);

            int transferredFunds = expedition.TemporaryFunds;
            var transferredItems = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> item in expedition.TemporaryInventory.Quantities)
            {
                transferredItems.Add(item.Key, item.Value);
            }
            var rescuedIds = new List<string>(expedition.RescuedCharacterIds);

            guild.AddFunds(transferredFunds);
            guild.Inventory.AddRange(transferredItems);
            for (int index = 0; index < participants.Count; index++)
            {
                participants[index].SetStatus(CharacterStatus.Available);
            }

            for (int index = 0; index < rescuedCharacters.Count; index++)
            {
                rescuedCharacters[index].SetStatus(CharacterStatus.Available);
            }

            expedition.ConsumeTemporaryLoot();
            expedition.Complete();
            if (!guild.RemoveExpedition(expedition.ExpeditionId))
            {
                throw new InvalidOperationException("Completed expedition could not be removed.");
            }

            return new ExpeditionReturnResult(
                expedition.ExpeditionId,
                transferredFunds,
                transferredItems,
                rescuedIds);
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
                        $"Expedition participant is unavailable for return: {participantId}");
                }

                participants.Add(participant);
            }

            return participants;
        }

        private static List<CharacterRuntimeData> ResolveRescuedCharacters(
            GuildRuntimeData guild,
            ExpeditionRuntimeData expedition)
        {
            var participants = new HashSet<string>(expedition.ParticipantIds, StringComparer.Ordinal);
            var rescuedCharacters = new List<CharacterRuntimeData>(
                expedition.RescuedCharacterIds.Count);
            for (int index = 0; index < expedition.RescuedCharacterIds.Count; index++)
            {
                string rescuedId = expedition.RescuedCharacterIds[index];
                if (participants.Contains(rescuedId) ||
                    !guild.TryGetCharacter(rescuedId, out CharacterRuntimeData rescued) ||
                    rescued.Status != CharacterStatus.Captured)
                {
                    throw new InvalidOperationException(
                        $"Rescued character is invalid for return: {rescuedId}");
                }

                rescuedCharacters.Add(rescued);
            }

            return rescuedCharacters;
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
