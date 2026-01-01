using System;

namespace Combat.Core;

public sealed record AttackRolled(
    Guid ActorId,
    Guid TargetId,
    int D20,
    int Total,
    bool IsCrit,
    bool IsHit
) : IEvent;
