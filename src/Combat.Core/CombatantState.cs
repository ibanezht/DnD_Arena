using System;

namespace Combat.Core;

public sealed record CombatantState(
    Guid Id,
    string Name,
    Faction Faction,
    GridPos Pos,
    Stats Stats,
    bool IsDead
);