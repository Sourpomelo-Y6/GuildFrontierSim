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

        public void RetainFraction(float ratio)
        {
            if (ratio < 0f || ratio > 1f || float.IsNaN(ratio))
            {
                throw new ArgumentOutOfRangeException(nameof(ratio));
            }

            var itemIds = new List<string>(quantities.Keys);
            for (int index = 0; index < itemIds.Count; index++)
            {
                string itemId = itemIds[index];
                int retained = (int)Math.Floor(quantities[itemId] * ratio);
                if (retained == 0)
                {
                    quantities.Remove(itemId);
                }
                else
                {
                    quantities[itemId] = retained;
                }
            }
        }

        public void Clear()
        {
            quantities.Clear();
        }

        public void EnsureCanAdd(IReadOnlyDictionary<string, int> additions)
        {
            if (additions == null)
            {
                throw new ArgumentNullException(nameof(additions));
            }

            foreach (KeyValuePair<string, int> addition in additions)
            {
                ValidateItemId(addition.Key);
                if (addition.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(additions));
                }

                checked
                {
                    _ = GetQuantity(addition.Key) + addition.Value;
                }
            }
        }

        public void AddRange(IReadOnlyDictionary<string, int> additions)
        {
            EnsureCanAdd(additions);
            foreach (KeyValuePair<string, int> addition in additions)
            {
                quantities[addition.Key] = GetQuantity(addition.Key) + addition.Value;
            }
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
