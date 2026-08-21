namespace GuildFrontierSim.Infrastructure.Random
{
    public interface IRandomSource
    {
        int Range(int minimumInclusive, int maximumExclusive);

        float Value { get; }
    }
}
