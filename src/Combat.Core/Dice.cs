namespace Combat.Core;

public static class Dice
{
    public static int D20(IRng rng)
    {
        return D(rng, 20);
    }

    public static int D(IRng rng, int sides)
    {
        return rng.NextInt(1, sides + 1);
    }
}