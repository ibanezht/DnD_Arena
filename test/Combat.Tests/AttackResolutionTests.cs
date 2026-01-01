using System.Linq;
using Combat.Core;
using Xunit;

namespace Combat.Tests;

public sealed class AttackResolutionTests
{
    [Fact]
    public void Nat1_IsMiss()
    {
        var state = TestBuilders.CreateDefaultState();
        var rng = new SequenceRng(1);
        var engine = new BattleEngine(rng);
        var command = new AttackCommand(state.ActiveId, state.InitiativeOrder[1], AttackType.Melee);

        var result = engine.Resolve(state, command);

        var roll = result.Events.OfType<AttackRolled>().Single();
        Assert.False(roll.IsHit);
        Assert.False(roll.IsCrit);
        Assert.Empty(result.Events.OfType<DamageRolled>());
    }

    [Fact]
    public void Nat20_IsCrit()
    {
        var state = TestBuilders.CreateDefaultState();
        var rng = new SequenceRng(20, 5, 6);
        var engine = new BattleEngine(rng);
        var command = new AttackCommand(state.ActiveId, state.InitiativeOrder[1], AttackType.Melee);

        var result = engine.Resolve(state, command);

        var roll = result.Events.OfType<AttackRolled>().Single();
        var damage = result.Events.OfType<DamageRolled>().Single();

        Assert.True(roll.IsCrit);
        Assert.True(roll.IsHit);
        Assert.Equal(14, damage.Amount);
    }

    [Fact]
    public void Hit_WhenTotalAtLeastAc()
    {
        var state = TestBuilders.CreateDefaultState();
        var rng = new SequenceRng(10, 4);
        var engine = new BattleEngine(rng);
        var command = new AttackCommand(state.ActiveId, state.InitiativeOrder[1], AttackType.Melee);

        var result = engine.Resolve(state, command);
        var roll = result.Events.OfType<AttackRolled>().Single();

        Assert.True(roll.IsHit);
    }

    [Fact]
    public void Miss_WhenTotalBelowAc()
    {
        var state = TestBuilders.CreateDefaultState();
        var rng = new SequenceRng(5);
        var engine = new BattleEngine(rng);
        var command = new AttackCommand(state.ActiveId, state.InitiativeOrder[1], AttackType.Melee);

        var result = engine.Resolve(state, command);
        var roll = result.Events.OfType<AttackRolled>().Single();

        Assert.False(roll.IsHit);
    }
}
