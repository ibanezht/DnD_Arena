# PLAN.md — Vertical Slice #1 (Grid Tactical PvE Combat)

Goal: a playable, repeatable, testable **turn-based grid combat** loop (PvE) using a **pure C# rules engine** + **Unity front-end** that only renders/animates events.

Non-goal: “BG3 but smaller.” This slice proves the core loop, not content scale.

---

## Definition of Done (Vertical Slice #1)
A player can:
- Start a combat encounter on a small grid map
- Select a unit, see valid moves/targets
- Move + attack on their turn
- End turn
- Win/lose the fight

The system:
- Runs deterministically from a seed
- Produces an event stream Unity plays back
- Has unit tests for core rules
- Has a combat log (text UI is fine)

---

## Scope (Slice #1)
### Map & Units
- Grid: square grid (recommended: 10x10 or 12x12)
- Obstacles: 3–8 blocked cells (static)
- Party: 1 hero (fighter-ish)
- Enemies: 3 goblins (melee + ranged mix optional)

### Actions (Keep it tiny)
- Move (bounded by speed, path blocked by occupied/blocked cells)
- Melee attack (d20 vs AC)
- Ranged attack (same attack resolution; range limited; no LoS for slice #1 unless you want it)
- End turn

### Rules (Slice #1)
- Initiative order (fixed at start; no rerolls)
- 1 Action per turn (skip bonus actions/reactions for slice #1)
- Attack roll: d20 + attack mod vs AC
- Natural 1 = miss, Natural 20 = crit (double dice OR +max dice; pick one and lock it)
- Damage: weapon die + mod
- Death: HP <= 0 => dead, removed from initiative/occupancy
- Win condition: all enemies dead
- Lose condition: hero dead

### Deferred (Not in Slice #1)
- Spells
- Saving throws
- Conditions, duration
- Concentration
- Opportunity attacks / reactions
- Cover, high ground, surfaces
- Inventory, equipment, leveling
- Multi-character party

---

## Architecture (Minimal but solid)

### Projects
- `/Combat.Core` (pure C#, no Unity refs)
- `/Combat.Tests` (unit tests)
- `/UnityClient` (Unity project)

> Recommendation: `Combat.Core` targets `netstandard2.1` for Unity compatibility.

### Engine Contract
- Input: `BattleState` + `Command`
- Output: `BattleState` (new) + `List<IEvent>` (resolved events)

**No randomness outside engine.** Engine owns RNG and dice.

---

## Data Model (Combat.Core)

### State
- `BattleState`
  - `int Round`
  - `Guid ActiveId`
  - `IReadOnlyList<Guid> InitiativeOrder`
  - `Dictionary<Guid, CombatantState> Combatants`
  - `GridState Grid` (blocked + occupancy)

- `CombatantState`
  - `Guid Id`
  - `string Name`
  - `Faction Faction` (Player/Enemy)
  - `GridPos Pos`
  - `Stats Stats` (AC, HP, MaxHP, Speed, AttackMod, DamageMod, DamageDie, Range)
  - `bool IsDead`

- `GridState`
  - width/height
  - blocked cells set
  - occupancy map: cell -> combatantId

### Commands
- `MoveCommand(actorId, toCell)`
- `AttackCommand(actorId, targetId, AttackType melee|ranged)`
- `EndTurnCommand(actorId)`

### Events (Engine emits; Unity animates)
- `CombatStarted(seed, initiativeOrder)`
- `TurnBegan(actorId)`
- `Moved(actorId, from, to)`
- `AttackDeclared(actorId, targetId, attackType)`
- `AttackRolled(actorId, targetId, d20, total, isCrit, isHit)`
- `DamageRolled(actorId, targetId, amount, isCrit)`
- `DamageApplied(targetId, amount, newHp)`
- `CombatantDied(targetId)`
- `TurnEnded(actorId)`
- `CombatEnded(result)` (Win/Lose)

> Keep events “dumb” and explicit. Unity should never infer outcomes.

### Services / Helpers
- `BattleEngine.Resolve(BattleState, ICommand) -> ResolveResult`
- `Rules`
  - `ValidateMove(...)`
  - `ValidateAttack(...)`
  - `RollAttack(...)`
  - `RollDamage(...)`
  - `ApplyDamage(...)`
- `Pathfinding` (BFS or A*; BFS is fine for small grids)
- `IRng` + `SeededRng` (deterministic)
- `Dice` helpers (d20, d6, etc.)

---

## Testing Plan (Combat.Tests)

### Must-have tests (Slice #1)
1) Attack resolution
- nat 1 => miss
- nat 20 => crit
- hit when total >= AC
- miss when total < AC

2) Damage + death
- damage reduces HP
- HP <= 0 => dead + removed from grid occupancy

3) Turn order
- active combatant advances correctly on EndTurn
- dead combatants are skipped

4) Movement validity
- can’t move out of bounds
- can’t move onto blocked cell
- can’t move onto occupied cell
- can’t exceed speed (path length)

### Nice-to-have tests
- reproducibility: same seed + same commands => identical event stream

---

## UnityClient Implementation Plan

### Scene Setup
- One scene: `CombatScene`
- Prefabs:
  - `GridCell` (optional)
  - `UnitView` (hero/goblin)
- UI:
  - Turn indicator
  - Combat log (scrollable text)
  - Basic buttons: End Turn
  - Hover/click highlights

### MonoBehaviours
#### `BattleController`
Responsibilities:
- Own `BattleState state`
- Own `BattleEngine engine`
- Collect input (click cell/unit)
- Convert to `Command`
- Call `Resolve`
- Push emitted events to `EventPlayer`
- Update local `state` to `result.NewState`

#### `EventPlayer`
Responsibilities:
- Queue events
- Play sequentially (Coroutine):
  - Move animation on `Moved`
  - Attack animation on `AttackDeclared/AttackRolled`
  - Damage popups on `DamageApplied`
  - Death animation on `CombatantDied`
- Update combat log UI for each event
- When queue is empty, signal `BattleController` that input is allowed

### Input Flow (Slice #1)
- If it’s player turn:
  - Click hero => select
  - Click cell => preview path; click again to confirm move (or single click confirm)
  - Click enemy => confirm attack
  - End Turn button
- If it’s enemy turn:
  - Basic AI chooses move/attack then pushes commands automatically

---

## Enemy AI (Very Simple)
For each enemy turn:
1) If in attack range => Attack hero
2) Else move towards hero (shortest path) up to speed
3) If now in range => Attack
4) EndTurn

> Score-based behavior can come later.

---

## Milestones

### Milestone 0 — Repo & Build (0.5 day)
- Create solution + projects
- Reference `Combat.Core` in Unity (asmdef or DLL build pipeline)
- CI optional (later)

### Milestone 1 — Engine Skeleton + Deterministic RNG (1 day)
- Implement state, commands, events
- Implement RNG + dice
- Implement `BattleEngine.Resolve` with validation scaffolding

### Milestone 2 — Movement + Pathfinding (1–2 days)
- Grid occupancy + blocked
- BFS path + speed limit
- Emit `Moved` events

### Milestone 3 — Attack + Damage + Death (1–2 days)
- Attack roll resolution
- Damage roll
- Apply damage + death handling
- Emit full event chain

### Milestone 4 — Turn Order + Win/Lose (1 day)
- Initiative fixed at start
- Turn begin/end events
- Skip dead
- End conditions and `CombatEnded`

### Milestone 5 — Unity Playback (2–4 days)
- Spawn grid + units
- BattleController + EventPlayer
- Combat log
- Selection + targeting

### Milestone 6 — AI + “Playable Fight” (1–2 days)
- Simple enemy AI loop
- Tune stats so fight is winnable and interesting
- Bugfix + polish pass

---

## Risks & How We Avoid Them
- **Over-architecture:** keep Core small and flat; don’t add patterns until pain demands it
- **Unity coupling:** Core never references Unity types
- **Non-determinism:** single RNG source, seeded, owned by engine
- **UI confusion:** combat log + highlights are mandatory

---

## Stats (Suggested Defaults)
Hero:
- HP 30, AC 16, Speed 6
- AttackMod +5, DamageDie d8, DamageMod +3
Goblins:
- HP 10, AC 13, Speed 6
- AttackMod +4, DamageDie d6, DamageMod +2
Ranged goblin (optional):
- same, Range 6–8 tiles

---

## Next Slice Preview (Post Vertical Slice #1)
Add in this order:
1) Saving throws + 2–3 spells
2) Conditions with duration (prone, poisoned)
3) Concentration
4) Opportunity attacks (reaction)
5) Better UI prediction (hit chance, damage range, AoE previews)

---
