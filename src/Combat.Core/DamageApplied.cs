using System;

namespace Combat.Core;

public sealed record DamageApplied(Guid TargetId, int Amount, int NewHp) : IEvent;