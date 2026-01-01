using System;
using System.Collections.Generic;
using Combat.Core;

namespace Combat.Tests;

internal sealed class SequenceRng : IRng
{
    private readonly Queue<int> _values;

    public SequenceRng(params int[] values)
    {
        _values = new Queue<int>(values);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (_values.Count == 0)
        {
            throw new InvalidOperationException("SequenceRng exhausted.");
        }

        var value = _values.Dequeue();
        if (value < minInclusive || value >= maxExclusive)
        {
            throw new InvalidOperationException(
                $"SequenceRng value {value} is out of range [{minInclusive}, {maxExclusive})."
            );
        }

        return value;
    }
}
