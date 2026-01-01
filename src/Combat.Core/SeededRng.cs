using System;

namespace Combat.Core;

public sealed class SeededRng : IRng
{
    private readonly Random _random;

    public SeededRng(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }
}
