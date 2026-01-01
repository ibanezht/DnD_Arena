using Combat.Core;

namespace Combat.Tests;

public sealed class ReproducibilityTests
{
    [Fact]
    public void SameSeedAndCommandsProduceIdenticalEvents()
    {
        var state = TestBuilders.CreateDefaultState();
        var seed = 12345;
        var engineA = new BattleEngine(new SeededRng(seed));
        var engineB = new BattleEngine(new SeededRng(seed));
        var commands = new ICommand[]
        {
            new AttackCommand(state.ActiveId, state.InitiativeOrder[1], AttackType.Melee),
            new EndTurnCommand(state.ActiveId),
            new AttackCommand(state.InitiativeOrder[1], state.ActiveId, AttackType.Melee)
        };

        var eventsA = Execute(engineA, state, commands);
        var eventsB = Execute(engineB, state, commands);

        Assert.Equal(eventsA, eventsB);
    }

    private static List<IEvent> Execute(BattleEngine engine, BattleState start, IEnumerable<ICommand> commands)
    {
        var state = start;
        var events = new List<IEvent>();

        foreach (var command in commands)
        {
            var result = engine.Resolve(state, command);
            state = result.NewState;
            events.AddRange(result.Events);
        }

        return events;
    }
}