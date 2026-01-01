using Combat.Core;
using Xunit;

namespace Combat.Tests;

public sealed class MovementValidationTests
{
    [Fact]
    public void RejectsOutOfBoundsMove()
    {
        var state = TestBuilders.CreateDefaultState();
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(-1, 0));

        var result = engine.Resolve(state, command);

        Assert.True(result.IsRejected);
        Assert.Same(state, result.NewState);
    }

    [Fact]
    public void RejectsBlockedCellMove()
    {
        var state = TestBuilders.NewBattle()
            .WithBlocked(new GridPos(3, 3))
            .Build();
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(3, 3));

        var result = engine.Resolve(state, command);

        Assert.True(result.IsRejected);
        Assert.Same(state, result.NewState);
    }

    [Fact]
    public void RejectsOccupiedCellMove()
    {
        var state = TestBuilders.CreateDefaultState();
        var occupied = state.Combatants[state.InitiativeOrder[1]].Pos;
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, occupied);

        var result = engine.Resolve(state, command);

        Assert.True(result.IsRejected);
        Assert.Same(state, result.NewState);
    }

    [Fact]
    public void RejectsMoveBeyondSpeed()
    {
        var state = TestBuilders.CreateDefaultState();
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(9, 9));

        var result = engine.Resolve(state, command);

        Assert.True(result.IsRejected);
        Assert.Same(state, result.NewState);
    }
}
