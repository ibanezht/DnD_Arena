namespace Combat.Core;

public sealed record CombatEnded(CombatResult Result) : IEvent;