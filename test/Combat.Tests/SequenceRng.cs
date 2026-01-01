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

        return _values.Dequeue();
    }
}
