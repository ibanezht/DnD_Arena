using System;

namespace Combat.Core;

public interface ICommand
{
    Guid ActorId { get; }
}