namespace GuildFrontierSim.Infrastructure.Random
{
    public sealed class UnityRandomSource : IRandomSource
    {
        public float Value => UnityEngine.Random.value;

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            return UnityEngine.Random.Range(minimumInclusive, maximumExclusive);
        }
    }
}
