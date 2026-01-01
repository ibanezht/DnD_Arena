using System;

namespace Combat.Core;

public sealed record TurnBegan(Guid ActorId) : IEvent;
