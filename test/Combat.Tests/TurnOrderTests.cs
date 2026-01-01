using Combat.Core;

namespace Combat.Tests;

public sealed class TurnOrderTests
{
    [Fact]
    public void EndTurnAdvancesActiveCombatant()
    {
        var state = TestBuilders.CreateDefaultState();
        var engine = new BattleEngine(new SequenceRng());
        var command = new EndTurnCommand(state.ActiveId);

        var result = engine.Resolve(state, command);

        Assert.Equal(state.InitiativeOrder[1], result.NewState.ActiveId);
        Assert.Contains(result.Events, e => e is TurnEnded ended && ended.ActorId == state.ActiveId);
        Assert.Contains(result.Events, e => e is TurnBegan began && began.ActorId == state.InitiativeOrder[1]);
    }

    [Fact]
    public void DeadCombatantsAreSkipped()
    {
        var state = TestBuilders.CreateDefaultState();
        var goblinId = state.InitiativeOrder[1];
        var deadGoblin = state.Combatants[goblinId] with { IsDead = true };
        var updated = TestBuilders.NewBattle()
            .WithCombatant(deadGoblin)
            .Build();
        var engine = new BattleEngine(new SequenceRng());
        var command = new EndTurnCommand(state.ActiveId);

        var result = engine.Resolve(updated, command);

        Assert.Equal(state.ActiveId, result.NewState.ActiveId);
        Assert.Equal(2, result.NewState.Round);
    }
}