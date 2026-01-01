namespace Combat.Core;

public interface IRng
{
    int NextInt(int minInclusive, int maxExclusive);
}
