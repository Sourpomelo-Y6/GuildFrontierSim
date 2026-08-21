using System;
using System.Collections.Generic;

namespace GuildFrontierSim.Domain.Guilds
{
    public sealed class GuildInventory
    {
        private readonly Dictionary<string, int> quantities =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, int> Quantities => quantities;

        public int GetQuantity(string itemId)
        {
            ValidateItemId(itemId);
            return quantities.TryGetValue(itemId, out int quantity) ? quantity : 0;
        }

        public void Add(string itemId, int quantity)
        {
            ValidateItemId(itemId);
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            quantities[itemId] = checked(GetQuantity(itemId) + quantity);
        }

        public bool TryRemove(string itemId, int quantity)
        {
            ValidateItemId(itemId);
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            int currentQuantity = GetQuantity(itemId);
            if (currentQuantity < quantity)
            {
                return false;
            }

            int remaining = currentQuantity - quantity;
            if (remaining == 0)
            {
                quantities.Remove(itemId);
            }
            else
            {
                quantities[itemId] = remaining;
            }

            return true;
        }

        private static void ValidateItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
            }
        }
    }
}
