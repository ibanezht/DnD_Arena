using System;

namespace Combat.Core;

public sealed record MoveCommand(Guid ActorId, GridPos To) : ICommand;
