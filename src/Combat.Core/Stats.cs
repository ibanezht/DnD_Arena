namespace Combat.Core;

public sealed record Stats(
    int Ac,
    int Hp,
    int MaxHp,
    int Speed,
    int AttackMod,
    int DamageMod,
    int DamageDie,
    int Range
);