using System;
using System.Collections.Generic;
using System.Linq;

namespace Combat.Core;

public sealed record GridState(
    int Width,
    int Height,
    IReadOnlyCollection<GridPos> Blocked,
    IReadOnlyDictionary<GridPos, Guid> Occupancy
)
{
    public bool IsInBounds(GridPos pos)
    {
        return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
    }

    public bool IsBlocked(GridPos pos)
    {
        return Blocked.Contains(pos);
    }

    public bool IsOccupied(GridPos pos)
    {
        return Occupancy.ContainsKey(pos);
    }
}