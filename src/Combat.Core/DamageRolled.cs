using System;

namespace Combat.Core;

public sealed record DamageRolled(
    Guid ActorId,
    Guid TargetId,
    int Amount,
    bool IsCrit
) : IEvent;
