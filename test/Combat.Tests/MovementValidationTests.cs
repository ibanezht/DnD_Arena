using System.Collections.Generic;
using Combat.Core;

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

    [Fact]
    public void RejectsSecondMoveInSameTurn()
    {
        var state = TestBuilders.CreateDefaultState();
        var engine = new BattleEngine(new SequenceRng());
        var firstMove = new MoveCommand(state.ActiveId, new GridPos(3, 1));
        var secondMove = new MoveCommand(state.ActiveId, new GridPos(3, 2));

        var firstResult = engine.Resolve(state, firstMove);
        var secondResult = engine.Resolve(firstResult.NewState, secondMove);

        Assert.False(firstResult.IsRejected);
        Assert.True(secondResult.IsRejected);
        Assert.Same(firstResult.NewState, secondResult.NewState);
    }

    [Fact]
    public void AllowsDiagonalMoveWithinSpeed()
    {
        var state = MoveGoblinAway(TestBuilders.CreateDefaultState());
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(2, 2));

        var result = engine.Resolve(state, command);

        Assert.False(result.IsRejected);
    }

    [Fact]
    public void AllowsDiagonalPathLengthWithinSpeed()
    {
        var state = WithActiveSpeed(MoveGoblinAway(TestBuilders.CreateDefaultState()), 3);
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(4, 4));

        var result = engine.Resolve(state, command);

        Assert.False(result.IsRejected);
    }

    [Fact]
    public void RejectsDiagonalPathLengthBeyondSpeed()
    {
        var state = WithActiveSpeed(MoveGoblinAway(TestBuilders.CreateDefaultState()), 2);
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(4, 4));

        var result = engine.Resolve(state, command);

        Assert.True(result.IsRejected);
    }

    [Fact]
    public void RejectsDiagonalMoveWhenCornerIsBlocked()
    {
        var state = MoveHeroTo(
            MoveGoblinAway(
                TestBuilders.NewBattle()
                    .WithBlocked(
                        new GridPos(1, 0),
                        new GridPos(0, 1)
                    )
                    .Build()
            ),
            new GridPos(0, 0)
        );
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(1, 1));

        var result = engine.Resolve(state, command);

        Assert.True(result.IsRejected);
    }

    [Fact]
    public void AllowsDiagonalMoveWhenOnlyOneCornerIsBlocked()
    {
        var state = MoveGoblinAway(
            TestBuilders.NewBattle()
                .WithBlocked(new GridPos(2, 1))
                .Build()
        );
        var engine = new BattleEngine(new SequenceRng());
        var command = new MoveCommand(state.ActiveId, new GridPos(2, 2));

        var result = engine.Resolve(state, command);

        Assert.False(result.IsRejected);
    }

    private static BattleState MoveGoblinAway(BattleState state)
    {
        var goblinId = state.InitiativeOrder[1];
        var goblin = state.Combatants[goblinId];
        var movedGoblin = goblin with { Pos = new GridPos(8, 8) };

        var updatedCombatants = new Dictionary<Guid, CombatantState>(state.Combatants)
        {
            [goblinId] = movedGoblin
        };
        var updatedOccupancy = new Dictionary<GridPos, Guid>(state.Grid.Occupancy);
        updatedOccupancy.Remove(goblin.Pos);
        updatedOccupancy[movedGoblin.Pos] = goblinId;

        return state with
        {
            Combatants = updatedCombatants,
            Grid = state.Grid with { Occupancy = updatedOccupancy }
        };
    }

    private static BattleState MoveHeroTo(BattleState state, GridPos destination)
    {
        var hero = state.Combatants[state.ActiveId];
        var movedHero = hero with { Pos = destination };

        var updatedCombatants = new Dictionary<Guid, CombatantState>(state.Combatants)
        {
            [hero.Id] = movedHero
        };
        var updatedOccupancy = new Dictionary<GridPos, Guid>(state.Grid.Occupancy);
        updatedOccupancy.Remove(hero.Pos);
        updatedOccupancy[movedHero.Pos] = hero.Id;

        return state with
        {
            Combatants = updatedCombatants,
            Grid = state.Grid with { Occupancy = updatedOccupancy }
        };
    }

    private static BattleState WithActiveSpeed(BattleState state, int speed)
    {
        var hero = state.Combatants[state.ActiveId];
        var updatedStats = hero.Stats with { Speed = speed };
        var updatedHero = hero with { Stats = updatedStats };

        var updatedCombatants = new Dictionary<Guid, CombatantState>(state.Combatants)
        {
            [updatedHero.Id] = updatedHero
        };

        return state with { Combatants = updatedCombatants };
    }
}
