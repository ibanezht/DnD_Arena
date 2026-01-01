using System;

namespace Combat.Core;

public sealed record TurnEnded(Guid ActorId) : IEvent;
