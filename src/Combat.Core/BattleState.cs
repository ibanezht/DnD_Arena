using System;
using System.Collections.Generic;

namespace Combat.Core;

public sealed record BattleState(
    int Round,
    Guid ActiveId,
    bool HasActiveMovedThisTurn,
    IReadOnlyList<Guid> InitiativeOrder,
    IReadOnlyDictionary<Guid, CombatantState> Combatants,
    GridState Grid
);