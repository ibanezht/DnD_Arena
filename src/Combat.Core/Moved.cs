using System;

namespace Combat.Core;

public sealed record Moved(Guid ActorId, GridPos From, GridPos To) : IEvent;
