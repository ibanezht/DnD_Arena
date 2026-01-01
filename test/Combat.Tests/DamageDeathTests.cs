using Combat.Core;

namespace Combat.Tests;

public sealed class DamageDeathTests
{
    [Fact]
    public void DamageReducesHp()
    {
        var state = TestBuilders.CreateDefaultState();
        var rng = new SequenceRng(12, 3);
        var engine = new BattleEngine(rng);
        var command = new AttackCommand(state.ActiveId, state.InitiativeOrder[1], AttackType.Melee);

        var result = engine.Resolve(state, command);
        var targetId = state.InitiativeOrder[1];
        var updatedTarget = result.NewState.Combatants[targetId];
        var damageApplied = result.Events.OfType<DamageApplied>().Single();

        Assert.Equal(4, updatedTarget.Stats.Hp);
        Assert.Equal(4, damageApplied.NewHp);
    }

    [Fact]
    public void DeadCombatantRemovedFromOccupancy()
    {
        var state = TestBuilders.CreateDefaultState();
        var rng = new SequenceRng(20, 6, 6);
        var engine = new BattleEngine(rng);
        var command = new AttackCommand(state.ActiveId, state.InitiativeOrder[1], AttackType.Melee);

        var result = engine.Resolve(state, command);
        var targetId = state.InitiativeOrder[1];
        var updatedTarget = result.NewState.Combatants[targetId];

        Assert.True(updatedTarget.IsDead);
        Assert.Contains(result.Events, e => e is CombatantDied died && died.TargetId == targetId);
        Assert.False(result.NewState.Grid.Occupancy.ContainsKey(updatedTarget.Pos));
    }
}