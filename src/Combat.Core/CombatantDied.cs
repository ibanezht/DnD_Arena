using System;

namespace Combat.Core;

public sealed record CombatantDied(Guid TargetId) : IEvent;