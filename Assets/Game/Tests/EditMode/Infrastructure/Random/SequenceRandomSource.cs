using System;
using System.Collections.Generic;
using GuildFrontierSim.Infrastructure.Random;

namespace GuildFrontierSim.Tests.Infrastructure.Random
{
    internal sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<int> integerValues;
        private readonly Queue<float> floatValues;

        public SequenceRandomSource(
            IEnumerable<int> integerValues = null,
            IEnumerable<float> floatValues = null)
        {
            this.integerValues = new Queue<int>(integerValues ?? Array.Empty<int>());
            this.floatValues = new Queue<float>(floatValues ?? Array.Empty<float>());
        }

        public float Value
        {
            get
            {
                if (floatValues.Count == 0)
                {
                    throw new InvalidOperationException("No float random values remain.");
                }

                return floatValues.Dequeue();
            }
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (integerValues.Count == 0)
            {
                throw new InvalidOperationException("No integer random values remain.");
            }

            int value = integerValues.Dequeue();
            if (value < minimumInclusive || value >= maximumExclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Expected a value in [{minimumInclusive}, {maximumExclusive}).");
            }

            return value;
        }
    }
}
