using System;
using System.Collections.Generic;
using System.Linq;

namespace Combat.Core;

public sealed class BattleEngine(IRng rng)
{
    private readonly IRng _rng = rng ?? throw new ArgumentNullException(nameof(rng));

    public ResolveResult Resolve(BattleState state, ICommand command)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        return command switch
        {
            MoveCommand move => ResolveMove(state, move),
            AttackCommand attack => ResolveAttack(state, attack),
            EndTurnCommand endTurn => ResolveEndTurn(state, endTurn),
            _ => Reject(state, "Unknown command.")
        };
    }

    private ResolveResult ResolveMove(BattleState state, MoveCommand command)
    {
        if (!TryGetActiveCombatant(state, command.ActorId, out var actor, out var rejection))
        {
            return Reject(state, rejection);
        }

        if (state.HasActiveMovedThisTurn)
        {
            return Reject(state, "Actor has already moved this turn.");
        }

        if (!state.Grid.IsInBounds(command.To))
        {
            return Reject(state, "Move out of bounds.");
        }

        if (command.To.Equals(actor.Pos))
        {
            return Reject(state, "Already at destination.");
        }

        if (state.Grid.IsBlocked(command.To))
        {
            return Reject(state, "Destination is blocked.");
        }

        if (state.Grid.IsOccupied(command.To))
        {
            return Reject(state, "Destination is occupied.");
        }

        if (!TryFindPathLength(state.Grid, actor.Pos, command.To, out var length))
        {
            return Reject(state, "No path to destination.");
        }

        if (length > actor.Stats.Speed)
        {
            return Reject(state, "Path exceeds speed.");
        }

        var updatedActor = actor with { Pos = command.To };
        var updatedCombatants = new Dictionary<Guid, CombatantState>(state.Combatants)
        {
            [actor.Id] = updatedActor
        };

        var updatedOccupancy = new Dictionary<GridPos, Guid>(state.Grid.Occupancy);
        updatedOccupancy.Remove(actor.Pos);
        updatedOccupancy[command.To] = actor.Id;

        var updatedGrid = state.Grid with { Occupancy = updatedOccupancy };
        var newState = state with
        {
            Combatants = updatedCombatants,
            Grid = updatedGrid,
            HasActiveMovedThisTurn = true
        };
        var events = new List<IEvent> { new Moved(actor.Id, actor.Pos, command.To) };

        return new ResolveResult(newState, events, null);
    }

    private ResolveResult ResolveAttack(BattleState state, AttackCommand command)
    {
        if (!TryGetActiveCombatant(state, command.ActorId, out var actor, out var rejection))
        {
            return Reject(state, rejection);
        }

        if (!state.Combatants.TryGetValue(command.TargetId, out var target))
        {
            return Reject(state, "Target not found.");
        }

        if (target.IsDead)
        {
            return Reject(state, "Target is already dead.");
        }

        var distance = ChebyshevDistance(actor.Pos, target.Pos);
        var range = command.AttackType == AttackType.Melee ? 1 : actor.Stats.Range;
        if (distance > range)
        {
            return Reject(state, "Target out of range.");
        }

        var events = new List<IEvent> { new AttackDeclared(actor.Id, target.Id, command.AttackType) };

        var d20 = Dice.D20(_rng);
        var total = d20 + actor.Stats.AttackMod;
        var isCrit = d20 == 20;
        var isHit = isCrit || (d20 != 1 && total >= target.Stats.Ac);

        events.Add(new AttackRolled(actor.Id, target.Id, d20, total, isCrit, isHit));

        if (!isHit)
        {
            return new ResolveResult(state, events, null);
        }

        var damage = RollDamage(actor.Stats.DamageDie, actor.Stats.DamageMod, isCrit);
        events.Add(new DamageRolled(actor.Id, target.Id, damage, isCrit));

        var rawHp = target.Stats.Hp - damage;
        var clampedHp = Math.Max(rawHp, 0);
        var updatedStats = target.Stats with { Hp = clampedHp };
        var updatedTarget = target with { Stats = updatedStats, IsDead = rawHp <= 0 };

        var updatedCombatants = new Dictionary<Guid, CombatantState>(state.Combatants)
        {
            [target.Id] = updatedTarget
        };

        var updatedGrid = state.Grid;
        var updatedOccupancy = new Dictionary<GridPos, Guid>(state.Grid.Occupancy);
        var newState = state with { Combatants = updatedCombatants, Grid = updatedGrid };
        events.Add(new DamageApplied(target.Id, damage, clampedHp));

        if (updatedTarget.IsDead)
        {
            updatedOccupancy.Remove(target.Pos);
            updatedGrid = updatedGrid with { Occupancy = updatedOccupancy };
            newState = newState with { Grid = updatedGrid };
            events.Add(new CombatantDied(target.Id));
        }

        if (TryGetCombatResult(newState, out var result))
        {
            events.Add(new CombatEnded(result));
        }

        return new ResolveResult(newState, events, null);
    }

    private ResolveResult ResolveEndTurn(BattleState state, EndTurnCommand command)
    {
        if (!TryGetActiveCombatant(state, command.ActorId, out var actor, out var rejection))
        {
            return Reject(state, rejection);
        }

        var events = new List<IEvent> { new TurnEnded(actor.Id) };

        var initiative = state.InitiativeOrder;
        var currentIndex = IndexOf(initiative, actor.Id);
        if (currentIndex < 0)
        {
            return Reject(state, "Active combatant not in initiative.");
        }

        var round = state.Round;
        var nextId = actor.Id;

        for (var step = 1; step <= initiative.Count; step++)
        {
            var nextIndex = (currentIndex + step) % initiative.Count;
            var candidateId = initiative[nextIndex];
            if (!state.Combatants.TryGetValue(candidateId, out var candidate))
            {
                continue;
            }

            if (candidate.IsDead)
            {
                continue;
            }

            nextId = candidateId;
            if (nextIndex <= currentIndex)
            {
                round += 1;
            }

            break;
        }

        var newState = state with
        {
            ActiveId = nextId,
            Round = round,
            HasActiveMovedThisTurn = false
        };

        if (TryGetCombatResult(newState, out var result))
        {
            events.Add(new CombatEnded(result));
            return new ResolveResult(newState, events, null);
        }

        events.Add(new TurnBegan(nextId));
        return new ResolveResult(newState, events, null);
    }

    private int RollDamage(int die, int modifier, bool isCrit)
    {
        var total = Dice.D(_rng, die) + modifier;
        if (isCrit)
        {
            total += Dice.D(_rng, die);
        }

        return total;
    }

    // Chebyshev distance (diagonal-friendly) matches D&D 5e grid range; movement stays cardinal-only.
    private static int ChebyshevDistance(GridPos from, GridPos to)
    {
        return Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));
    }

    private static bool TryFindPathLength(
        GridState grid,
        GridPos start,
        GridPos goal,
        out int length
    )
    {
        length = 0;

        if (start.Equals(goal))
        {
            return true;
        }

        var queue = new Queue<GridPos>();
        var distances = new Dictionary<GridPos, int>
        {
            [start] = 0
        };

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDistance = distances[current];

            foreach (var next in GetNeighbors(current))
            {
                if (!grid.IsInBounds(next))
                {
                    continue;
                }

                if (grid.IsBlocked(next))
                {
                    continue;
                }

                if (grid.IsOccupied(next) && !next.Equals(goal))
                {
                    continue;
                }

                if (distances.ContainsKey(next))
                {
                    continue;
                }

                var nextDistance = currentDistance + 1;
                if (next.Equals(goal))
                {
                    length = nextDistance;
                    return true;
                }

                distances[next] = nextDistance;
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static IEnumerable<GridPos> GetNeighbors(GridPos pos)
    {
        yield return new GridPos(pos.X + 1, pos.Y);
        yield return new GridPos(pos.X - 1, pos.Y);
        yield return new GridPos(pos.X, pos.Y + 1);
        yield return new GridPos(pos.X, pos.Y - 1);
    }

    private static int IndexOf(IReadOnlyList<Guid> list, Guid value)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] == value)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryGetActiveCombatant(
        BattleState state,
        Guid actorId,
        out CombatantState combatant,
        out string rejection
    )
    {
        combatant = default!;
        rejection = string.Empty;

        if (actorId != state.ActiveId)
        {
            rejection = "Actor is not active.";
            return false;
        }

        if (!state.Combatants.TryGetValue(actorId, out combatant))
        {
            rejection = "Actor not found.";
            return false;
        }

        if (combatant.IsDead)
        {
            rejection = "Actor is dead.";
            return false;
        }

        return true;
    }

    private static bool TryGetCombatResult(BattleState state, out CombatResult result)
    {
        var hasPlayers = state.Combatants.Values.Any(c => !c.IsDead && c.Faction == Faction.Player);
        var hasEnemies = state.Combatants.Values.Any(c => !c.IsDead && c.Faction == Faction.Enemy);

        if (!hasEnemies)
        {
            result = CombatResult.Win;
            return true;
        }

        if (!hasPlayers)
        {
            result = CombatResult.Lose;
            return true;
        }

        result = default;
        return false;
    }

    private static ResolveResult Reject(BattleState state, string reason)
    {
        return new ResolveResult(state, [], reason);
    }
}
