using System.Collections.Generic;

namespace Combat.Core;

public sealed record ResolveResult(
    BattleState NewState,
    IReadOnlyList<IEvent> Events,
    string? RejectionReason
)
{
    public bool IsRejected => RejectionReason is not null;
}
