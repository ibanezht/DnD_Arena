# AGENTS.md — Combat Vertical Slice #1 (C#/.NET + Unity)

This repo is a .NET solution intended to be developed in Rider. The Unity project will consume the `Combat.Core` library, but Unity is **not required** to complete the initial engine + tests.

This file tells coding agents (and humans) how to work in this repo.

---

## Prime Directive
Ship the first vertical slice by building a **pure C# combat rules engine** with **unit tests** first, then integrating into Unity via an **event playback** adapter.

**Do not** add architectural patterns (DDD/CQRS/event sourcing) unless a concrete pain point appears. Prefer small, testable functions.

---

## Repo Structure (Target)
Create a .NET solution with these projects:

- `src/Combat.Core/`
  - Pure C# library. **No Unity references.**
  - Target: `netstandard2.1` (recommended for Unity compatibility)
    - If you’re not integrating Unity yet, `net8.0` is fine, but plan to retarget later.

- `test/Combat.Tests/`
  - Unit tests for `Combat.Core`.
  - Use either **xUnit** or **NUnit** (pick one and stick to it).
  - Target: `net8.0`.

- `tools/` (optional)
  - Small utilities later (e.g., replay runner). Not required for slice #1.

Unity project is out-of-solution or in `/unity/` later. For slice #1, focus on core + tests.

---

## Design Rules (Non-Negotiable)
### 1) Determinism
- The engine must be deterministic given:
  - an initial `BattleState`
  - a random seed
  - a list/stream of commands
- All randomness MUST go through a single RNG abstraction:
  - `IRng` + `SeededRng`
- Do not use `System.Random` directly outside `SeededRng`.
- Never use Unity randomness in Core.

### 2) Command In, Events Out
Engine API is conceptually:

- `Resolve(state, command) -> (newState, events[])`

Unity will:
- send Commands
- receive Events
- animate Events

Core must never “know” about animations, coroutines, GameObjects, etc.

### 3) Keep It Small
- Favor straightforward code over frameworks.
- Keep types and files small.
- Avoid inheritance-heavy designs. Prefer composition and plain records/classes.

---

## Implementation Priorities (Vertical Slice #1)
### Must Implement
- Grid movement with obstacles/occupancy + speed limit pathing
- Initiative + turns
- Melee and ranged attacks:
  - d20 + mods vs AC
  - nat 1 miss, nat 20 crit
- Damage + death + win/lose
- Emit a complete event stream for every command

### Must Defer
- Spells, saving throws, conditions, concentration
- Reactions / opportunity attacks
- Cover, LoS, surfaces, elevation
- Inventory/equipment/leveling

---

## Core Domain Model (Suggested)
### State
- `BattleState`
  - `Round`
  - `ActiveId`
  - `InitiativeOrder`
  - `Combatants`
  - `Grid`

- `CombatantState`
  - `Id`, `Name`, `Faction`
  - `Pos`
  - `Stats` (HP/AC/Speed/AttackMod/DamageDie/DamageMod/Range)
  - `IsDead`

- `GridState`
  - dimensions
  - blocked set
  - occupancy map

### Commands
- `MoveCommand(actorId, to)`
- `AttackCommand(actorId, targetId, AttackType melee|ranged)`
- `EndTurnCommand(actorId)`

### Events
Events should be explicit and replayable:
- `TurnBegan`
- `Moved`
- `AttackDeclared`
- `AttackRolled`
- `DamageRolled`
- `DamageApplied`
- `CombatantDied`
- `TurnEnded`
- `CombatEnded`

---

## Testing Requirements
Write tests BEFORE Unity integration.

### Required Tests
1) Attack resolution
- nat 1 => miss
- nat 20 => crit
- hit when total >= AC
- miss when total < AC

2) Damage & death
- damage reduces HP
- HP <= 0 => `CombatantDied` and removed from occupancy / skipped in turns

3) Turn order
- `EndTurn` advances active correctly
- dead combatants are skipped

4) Movement validation
- out of bounds rejected
- blocked cell rejected
- occupied cell rejected
- path length > speed rejected

### Repro Test (Strongly Recommended)
- same seed + same commands => identical event stream

---

## Coding Style
- C# 12 / modern idioms are fine.
- Prefer `record` / `record struct` for immutable value objects.
- Keep `BattleState` as immutable-ish where practical; if mutability is used, confine it and test it.
- No reflection-driven “magic.”
- No DI container required.

---

## Error Handling Policy
For invalid commands, prefer:
- return a `RejectedCommand` event with a reason, OR
- return a `ResolveResult` with `IsRejected` + reason

Do not throw for normal invalid player input.

Exceptions are for programmer errors (nulls, invariant violations).

---

## Agent Workflow
When acting as an agent (CLI/autonomous coding):
1) Implement smallest vertical slice end-to-end in Core.
2) Add unit tests per section above.
3) Only then consider Unity adapters.

### Commit Granularity (if using git)
- Commit after each milestone:
  - skeleton + RNG
  - movement + tests
  - attacks + tests
  - turns/win/lose + tests

---

## “Stop Signs” (Do NOT do these)
- Do NOT add DDD layers (repositories, aggregates, UoW)
- Do NOT add CQRS read models
- Do NOT add event sourcing persistence
- Do NOT build a content pipeline
- Do NOT implement spells/conditions in slice #1

---

## Completion Checklist (Slice #1)
- [ ] `dotnet test` passes
- [ ] A sample fight can be simulated in tests (hero vs goblins)
- [ ] Engine emits a readable event stream for each command
- [ ] Results reproducible with seed

---

## Commit & Pull Request Guidelines
Commits: small, imperative (e.g., Combat.Core: Fix null target check). Avoid vague messages.
Branching: feature branches from development; rebase or merge frequently.
PRs: clear description, linked issues/PRs, affected projects, test evidence (logs/screenshots), and any config notes.
CI: PRs must build cleanly and pass dotnet test.
