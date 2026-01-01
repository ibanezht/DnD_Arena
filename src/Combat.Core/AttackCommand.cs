using System;

namespace Combat.Core;

public sealed record AttackCommand(Guid ActorId, Guid TargetId, AttackType AttackType) : ICommand;