using System;

namespace GuildFrontierSim.Infrastructure.Random
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly System.Random random;

        public SystemRandomSource(int seed)
        {
            random = new System.Random(seed);
        }

        public float Value => (float)random.NextDouble();

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
            }

            return random.Next(minimumInclusive, maximumExclusive);
        }
    }
}
