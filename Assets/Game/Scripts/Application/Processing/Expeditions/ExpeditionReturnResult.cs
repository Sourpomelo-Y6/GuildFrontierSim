using System.Collections.Generic;

namespace GuildFrontierSim.Application.Processing.Expeditions
{
    public sealed class ExpeditionReturnResult
    {
        public ExpeditionReturnResult(
            string expeditionId,
            int transferredFunds,
            IReadOnlyDictionary<string, int> transferredItems,
            IReadOnlyList<string> rescuedCharacterIds)
        {
            ExpeditionId = expeditionId;
            TransferredFunds = transferredFunds;
            TransferredItems = transferredItems;
            RescuedCharacterIds = rescuedCharacterIds;
        }

        public string ExpeditionId { get; }
        public int TransferredFunds { get; }
        public IReadOnlyDictionary<string, int> TransferredItems { get; }
        public IReadOnlyList<string> RescuedCharacterIds { get; }
    }
}
