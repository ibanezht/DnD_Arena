using System;

namespace Combat.Core;

public sealed record AttackDeclared(Guid ActorId, Guid TargetId, AttackType AttackType) : IEvent;