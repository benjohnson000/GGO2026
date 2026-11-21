# Combat (health, block, statuses)

`Assets/Scripts/Combat/`: the fight between the player and one enemy. Like the [Board](Board.md), the rules
are plain C# that resolve instantly and return an ordered list of events; views only play them back.

| Type | Role |
|---|---|
| `Combatant` | One side: `Health`, `MaxHealth`, `Block`, `IsDead`, `Statuses`. Holds state only. |
| `Combat` | Owns both combatants, the action queue and non-status listeners. All changes go through it. |
| `CombatAction` (`Actions/`) | A queued change: `DealDamageAction`, `GainBlockAction`, `HealAction`, `ApplyStatusAction`, `StartTurnAction`. |
| `CombatEvent` (`Events/`) | What happened. Carries the values after the change. |
| `DamageInfo`, `DamageKind` | One hit going through the damage pipeline. |
| `CombatListener` | Base for statuses, charms and affixes: modifier and trigger hooks. |
| `StatusEffect`, `StatusDataSO` (`Status/`) | A status's runtime stacks and behaviour, and its asset. |
| `PoisonStatusSO`, `PoisonStatus` | The first status, and the example to copy. |
| `CombatantDataSO` | Name and max health for a player or enemy. |
| `CombatantView`, `EncounterView` (`View/`) | Placeholder UI and the scene component that builds a `Combat`. |

## Public API

```csharp
Combat combat = new Combat(playerData.CreateCombatant(), enemyData.CreateCombatant());
List<CombatEvent> events = combat.Execute(new DealDamageAction(source, target, 3, TileColour.Red));
List<CombatEvent> events = combat.StartTurn(combat.Enemy);

combat.Enqueue(actionA);                  // queue several, then
List<CombatEvent> events = combat.Resolve();

combat.AddListener(charm, combat.Player); // charms, affixes (statuses are added by ApplyStatusAction)
```

`Combatant` setters are `internal`. Nothing outside `Combat` should change a combatant.

## Action queue

Every change is a `CombatAction`. `Resolve` runs the queue in order until it is empty. A listener that
reacts to something calls `combat.Enqueue(...)`. The new action runs after those already queued, in the
same resolve, and its events go into the same list.

- Listeners **never change a combatant directly** and never run actions themselves. This keeps chains in a
  predictable order and stops two triggers from recursing into each other.
- `Execute`/`Resolve` called during a resolve just queue the action and return an empty list. The outer
  resolve runs it.
- `MaxActionsPerResolve` (1000) stops runaway trigger loops, logs a warning and clears the queue.
- Actions targeting a dead combatant do nothing.

## Damage pipeline

`DealDamageAction` → `Combat.ResolveDamage`:

1. The **attacker's** listeners run `ModifyOutgoingDamage` (skipped when `Source` is null, as for poison).
2. The **target's** listeners run `ModifyIncomingDamage`.
3. The amount is clamped to 0 or more. Block absorbs what it can unless `IgnoresBlock` is set.
4. Health drops (never below 0). A `DamageEvent` is logged, then every listener gets `OnDamageDealt`.
5. At 0 health the target dies: `IsDead`, `CombatantDiedEvent`, `OnCombatantDied`.

`DamageEvent` fires even when block absorbs the whole hit. For "whenever you take damage", check
`HealthLost > 0`.

Modifiers should only add or subtract (DESIGN.md asks charms to stay additive). Multipliers such as colour
weaknesses still need an agreed order.

## Turns and block

`StartTurn(combatant)` logs `TurnStartedEvent`, then removes the combatant's block, then fires
`OnTurnStarted` for every listener. The owner's listeners can reduce the block lost with `ModifyBlockLost`
(for example, a charm that keeps half). Poison ticks in `OnTurnStarted`, after block is gone.

**Nothing calls `StartTurn` yet.** The turn flow (swap → passives → enemy countdown) comes with enemy
intents. When the enemy gets a turn is an open design question; see DESIGN.md.

## Listeners

`CombatListener` has `Owner` and hooks that do nothing by default:

| Hook | Runs for | Use |
|---|---|---|
| `ModifyOutgoingDamage(combat, damage)` | Attacker's listeners | Strength, Weak, charms on your hits |
| `ModifyIncomingDamage(combat, damage)` | Target's listeners | Vulnerable, resistances |
| `ModifyBlockGained(combat, amount)` | Owner's listeners | Block bonuses |
| `ModifyBlockLost(combat, amount)` | Owner's listeners | Keep some block between turns |
| `OnDamageDealt`, `OnBlockChanged`, `OnHealed`, `OnStatusChanged`, `OnTurnStarted`, `OnCombatantDied` | Every listener | Triggers. Check `Owner` |

**Order:** listeners owned by the event's target come first, then the opponent's. Within one side,
statuses come first (in the order they were applied), then other listeners (in the order they were added).

Example charm, "whenever you take damage, apply 5 poison to the attacker":

```csharp
public override void OnDamageDealt(Combat combat, DamageEvent e)
{
    if (e.Target == Owner && e.HealthLost > 0 && e.Source != null)
        combat.Enqueue(new ApplyStatusAction(e.Source, poison, 5));
}
```

## Statuses

- A status is identified by its `StatusDataSO` **asset**, so make exactly one asset per status.
- `ApplyStatusAction(target, data, stacks)` adds stacks and creates the `StatusEffect` the first time.
  Negative stacks remove stacks. At 0 stacks the status is removed. Each change logs a `StatusChangedEvent`.
- A plain `StatusDataSO` (**Create › GGO2026 › Status › Plain Status**) is just a counter with no behaviour.
- To add a status with behaviour, subclass `StatusDataSO` and give it serialized tuning fields, then override
  `CreateEffect()` to return your own `StatusEffect` subclass, which overrides the hooks it needs. Copy
  `PoisonStatusSO` / `PoisonStatus`.

### Poison

**Create › GGO2026 › Status › Poison**. At the start of its owner's turn it deals damage equal to its
stacks (`DamageKind.Status`, no source), then loses `stacksLostPerTick` (default 1) stacks.
`ignoresBlock` (default on) is a placeholder until the design settles it.

## Events

All derive from `CombatEvent`, which has `Target` (the combatant it happened to).

| Event | Fields | Logged when |
|---|---|---|
| `DamageEvent` | `Damage`, `Source`, `Blocked`, `HealthLost`, `BlockAfter`, `HealthAfter` | Every hit, even a fully blocked one |
| `BlockChangedEvent` | `OldBlock`, `NewBlock` | Block gained, or lost at turn start (block absorbed by a hit is on `DamageEvent`) |
| `HealedEvent` | `Amount`, `HealthAfter` | Health restored (only when more than 0) |
| `StatusChangedEvent` | `Status`, `OldStacks`, `NewStacks`, `WasApplied`, `WasRemoved` | Stacks changed |
| `TurnStartedEvent` | – | `StartTurn`, before block loss |
| `CombatantDiedEvent` | – | Health reached 0 |

## Views

- `CombatantView`: a placeholder UI with optional references (name, filled health `Image`, health text,
  block root and text, status text). `Bind(combatant)` copies the current state, and `Apply(event)` updates
  from events about its combatant, so the display follows playback rather than live state.
- `EncounterView`: holds the player and enemy `CombatantDataSO`s and their views, and builds the `Combat`
  in `Start`. `Play(events)` sends each event to both views, applied instantly for now.
- **Debug:** in Play mode, open the `EncounterView` component's ⋮ menu for *Debug: Damage Enemy / Damage
  Player / Player Gains Block / Heal Player / Apply Status … / Start … Turn*. They use `debugAmount` and
  `debugStatus`.

### Scene setup (Game scene)

Place one `CombatantView` on the world-space `Canvas` to the left of the board (player) and one to the
right (enemy), and an `EncounterView` that references both views and two `CombatantDataSO` assets
(**Create › GGO2026 › Combatant**).

## Not built yet

- Tile effects dealing damage, and enemy intents. Both will create `CombatAction`s.
- A shared board/combat timeline, so combat events animate in step with the matches that caused them.
- Run state, so player health carries over between encounters (`CreateCombatant(health)` already accepts it),
  and the setting or charm that turns this off.
- Status icons, damage numbers and health bar animation.
