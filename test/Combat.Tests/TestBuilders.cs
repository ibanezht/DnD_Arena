using Combat.Core;

namespace Combat.Tests;

internal static class TestBuilders
{
    public static BattleState CreateDefaultState()
    {
        return NewBattle().Build();
    }

    public static BattleStateBuilder NewBattle()
    {
        return new BattleStateBuilder();
    }
}

internal sealed class BattleStateBuilder
{
    private readonly HashSet<GridPos> _blocked;
    private readonly Dictionary<Guid, CombatantState> _combatants;
    private readonly int _height;
    private readonly List<Guid> _initiativeOrder;
    private readonly int _width;
    private Guid _activeId;
    private int _round;

    public BattleStateBuilder()
    {
        var heroId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var goblinId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var hero = new CombatantState(
            heroId,
            "Hero",
            Faction.Player,
            new GridPos(1, 1),
            new Stats(16, 30, 30, 6, 5, 3, 8, 1),
            false
        );

        var goblin = new CombatantState(
            goblinId,
            "Goblin",
            Faction.Enemy,
            new GridPos(2, 1),
            new Stats(13, 10, 10, 6, 4, 2, 6, 1),
            false
        );

        _combatants = new Dictionary<Guid, CombatantState>
        {
            [heroId] = hero,
            [goblinId] = goblin
        };

        _initiativeOrder = [heroId, goblinId];
        _blocked = [];
        _round = 1;
        _activeId = heroId;
        _width = 10;
        _height = 10;
    }

    public BattleStateBuilder WithBlocked(params GridPos[] blocked)
    {
        foreach (var pos in blocked)
        {
            _blocked.Add(pos);
        }

        return this;
    }

    public BattleStateBuilder WithCombatant(CombatantState combatant)
    {
        _combatants[combatant.Id] = combatant;
        return this;
    }

    public BattleStateBuilder WithActive(Guid activeId)
    {
        _activeId = activeId;
        return this;
    }

    public BattleStateBuilder WithRound(int round)
    {
        _round = round;
        return this;
    }

    public BattleStateBuilder WithInitiativeOrder(params Guid[] ids)
    {
        _initiativeOrder.Clear();
        _initiativeOrder.AddRange(ids);
        return this;
    }

    public BattleState Build()
    {
        var occupancy = new Dictionary<GridPos, Guid>();
        foreach (var combatant in _combatants.Values)
        {
            if (combatant.IsDead)
            {
                continue;
            }

            occupancy[combatant.Pos] = combatant.Id;
        }

        var grid = new GridState(_width, _height, _blocked, occupancy);

        return new BattleState(
            _round,
            _activeId,
            false,
            _initiativeOrder,
            _combatants,
            grid
        );
    }
}