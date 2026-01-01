using System;

namespace Combat.Core;

public sealed record EndTurnCommand(Guid ActorId) : ICommand;
