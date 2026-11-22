# Combat (health, block, statuses)

`Assets/Scripts/Combat/`: the fight between the player and one enemy. Like the [Board](Board.md), the rules
are plain C# that resolve instantly and return an ordered list of events; views only play them back.

| Type | Role |
|---|---|
| `Combatant` | One side: `Health`, `MaxHealth`, `Block`, `IsDead`, `Statuses`. Holds state only. |
| `Combat` | Owns both combatants, the action queue and non-status listeners. All changes go through it. |
| `CombatAction` (`Actions/`) | A queued change: `DealDamageAction`, `GainBlockAction`, `HealAction`, `ApplyStatusAction`, `StartTurnAction`, `SetCountdownAction`, `ChangeCountdownAction`. |
| `CombatEvent` (`Events/`) | What happened. Carries the values after the change. |
| `DamageInfo`, `DamageKind` | One hit going through the damage pipeline. |
| `CombatListener` | Base for statuses, charms and affixes: modifier and trigger hooks. |
| `StatusEffect`, `StatusDataSO` (`Status/`) | A status's runtime stacks and behaviour, and its asset. |
| `PoisonStatusSO`, `PoisonStatus` | The first status, and the example to copy. |
| `CombatantDataSO` | Name and max health for a player or enemy. |
| `CombatantView`, `EncounterView` (`View/`) | Placeholder UI and the scene component that builds a `Combat`. |
| `StatusRowView`, `StatusIconView` (`View/`) | Block and status icons under each combatant. See [Status icons](#status-icons). |
| `DamageProjectile` (`View/`) | Prefab component for any tile effect flying to the combatant it affects, despite the name. See [Views](#views). |

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

`StartTurn` is called by [`Encounter`](Encounter.md) when the enemy's intent fires: enemy turn, the
intent's actions, then the player's turn. Swaps where the countdown only ticks are not turns.

The intent countdown is combat state too (`Combatant.IntentCountdown`, changed by `SetCountdownAction`
and `ChangeCountdownAction`), so statuses such as stun and charms can change it.

## Listeners

`CombatListener` has `Owner` and hooks that do nothing by default:

| Hook | Runs for | Use |
|---|---|---|
| `ModifyOutgoingDamage(combat, damage)` | Attacker's listeners | Strength, Weak, charms on your hits |
| `ModifyIncomingDamage(combat, damage)` | Target's listeners | Vulnerable, resistances |
| `ModifyBlockGained(combat, amount)` | Owner's listeners | Block bonuses |
| `ModifyBlockLost(combat, amount)` | Owner's listeners | Keep some block between turns |
| `OnDamageDealt`, `OnBlockChanged`, `OnHealed`, `OnStatusChanged`, `OnTurnStarted`, `OnCountdownChanged`, `OnCombatantDied` | Every listener | Triggers. Check `Owner` |

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
| `CountdownChangedEvent` | `OldCountdown`, `NewCountdown` | Intent countdown ticked, delayed or set |
| `IntentChangedEvent` | `Intent` | A new intent was picked. Logged by `Encounter`, so listeners don't get it |
| `CombatantDiedEvent` | – | Health reached 0 |

## Views

- `CombatantView`: a placeholder UI with optional references (name, health fill `Image` scaled on X from
  0 at no health to 1 at full health, so set its pivot to the end it should shrink toward, health text,
  block root and text, status row). `Bind(combatant)` copies the current state, and `Apply(event)` updates
  from events about its combatant, so the display follows playback rather than live state.
- `CombatantView.intentText` (enemy only) shows the intent and countdown, e.g. "Attack 6 in 3".
- `CombatantView.damageSound` (optional `SoundProfileSO`) plays through `SFXManager` when a `DamageEvent`
  with `HealthLost > 0` is applied, so it sounds when the hit is shown (for tile damage, when its projectile
  lands). Hits fully absorbed by block are silent. Assign a different profile on the player and enemy views.
- `EncounterView`: holds the player and enemy data and their views. It builds the `Encounter` and its
  `Combat` in `Awake` (so a `BoardView` can pick it up in `Start`), binds the views and picks the first
  intent in `Start`. `Play(events)` sends each event to both views and shows `victoryRoot`/`defeatRoot` on a
  death. `BoardView` calls `Play(events, origin, colour)` with each tile effect's `CombatEvents`, the tile's world
  position and its shape colour as soon as that tile has disappeared, and `Play(events)` with the enemy-turn events after a swap. See
  [Encounter](Encounter.md).
- **Projectiles.** If `EncounterView.damageProjectile` is set, every change played with an origin (i.e. from
  a tile) spawns that prefab at the tile, which flies to the affected combatant's `HitPoint`. The change is
  shown when it lands. Tiles need no setup: the event's `Target` picks the side.
  - Flies: `DamageEvent`, `BlockChangedEvent`, `HealedEvent`, `StatusChangedEvent`, `CountdownChangedEvent`
    (see `EncounterView.FliesToTarget`). A new event type that should fly goes in that list.
  - Doesn't fly: `CombatantDiedEvent`, `TurnStartedEvent`, `IntentChangedEvent`. They wait in the queue.
  - Colour: the tile's shape colour, as drawn on the board (`DamageProjectile.tintWithTileColour`, default on).
    It shows the tile, not the kind of effect, so every projectile from a tile with two effects matches.
  - One projectile per event, so a tile with two effects sends two, and a charm's reaction to a tile also
    flies from that tile. An effect that changes nothing (a heal at full health) logs no event, so sends none.
  - Without the prefab, or for events played without an origin (enemy turns, poison ticks, debug actions),
    changes show instantly.
  - `CombatantView.hitPoint` is the prefab's portrait. Left empty, it is the view's pivot, which is at the
    screen-edge side of each panel.
  - When the fight's scene unloads mid-flight, the callback is ignored so it doesn't update destroyed UI.
- **Event queue.** `EncounterView` shows events strictly in order: every event goes into a queue, and the
  front is shown only once it is ready. A projectile's event is not ready until it lands, so later events
  (death, the next change) wait behind it. Projectiles still launch straight away, so a run of effect
  tiles sends them in quick succession. `IsPlaying` is true while anything is queued; `BoardView` waits for
  it before playing the enemy turn and returning to Idle, so every projectile lands before the enemy acts.
- **`DamageProjectile`** (component on the prefab): flies a quadratic arc from the tile to the target,
  following it if it moves, and calls back on impact. On impact it spawns the optional `impactPrefab`,
  hides its sprites, stops its trails and particle systems emitting, and destroys itself after
  `lingerTime` so they fade out. If it is destroyed mid-flight it still releases its event, so playback
  never stalls.

  | Field | Default | Meaning |
  |---|---|---|
  | `duration` | 0.35 s | Launch to impact |
  | `arcHeight` | 1 | Upward bow at the middle of the path, world units. 0 = straight |
  | `arcSpread` | 0.75 | Random sideways bow per projectile, so a run of hits fans out |
  | `easePower` | 2 | 1 = constant speed; higher accelerates into the hit |
  | `lingerTime` | 0.5 s | Kept alive after impact for trails/particles to fade |
  | `impactPrefab` | – | Optional, spawned at the impact point. Destroy it from its own settings |
  | `tintWithTileColour` | on | Multiplies sprites, trails and single-colour particles (the impact's too) by the tile's colour, through `RendererTint`. Use white or grey art |

  The world-space `Canvas` and the board share world space, so positions need no conversion. Give the
  prefab's renderers a sorting layer/order that draws above the board and the canvas.
- **Debug:** in Play mode, open the `EncounterView` component's ⋮ menu for *Debug: Damage Enemy / Damage
  Player / Player Gains Block / Heal Player / Apply Status … / Delay Enemy Intent / Advance Swap*. They use
  `debugAmount` and `debugStatus`.

### Status icons

`CombatantView` shows statuses in a `StatusRowView`. It needs no setup: with `statusRow` empty, `Bind`
creates a row just below the view. Icons are built in code (`StatusIconView`), so there is no prefab.

- **Order:** block first, then statuses in the order they were shown. A status that ran out and is applied
  again goes to the end. Lines are centred and wrap downward.
- **Block** is an icon only while the view's `blockRoot` and `blockText` are both empty, so it is never
  shown twice. It is still a field on `Combatant`, not a status.
- **Look:** `StatusDataSO.icon` tinted by `StatusDataSO.colour` (white leaves full-colour art as it is).
  Without an icon, a square of that colour shows the first letter of the status's name. Both have a badge
  with the stack count.
- **Animation:** icons pop in, pulse when the count changes (so a poison tick pulses as it loses a stack)
  and shrink out at 0. Whatever is active at `Bind` appears without animating.
- **Timing:** icons change when the `EncounterView` queue shows the event, not when the rules resolve.

To move or tune a row, add a `StatusRowView` to a RectTransform and assign it to `CombatantView.statusRow`.
Icons hang from that rect's top centre.

| Field | Default | Meaning |
|---|---|---|
| `iconSize`, `spacing` | 1.25, 0.25 | In canvas units (world units on this canvas) |
| `iconsPerLine` | 5 | Icons before wrapping |
| `font` | – | Defaults to the font of the view's health or name text |
| `badgeColour`, `countColour` | dark, cream | Stack badge. The placeholder letter also uses `badgeColour` |
| `blockSprite`, `blockColour` | –, blue | Block icon. A "B" square without a sprite |
| `pulseScale`, `pulseDuration` | 1.35, 0.25 s | Pop and pulse |
| `removeDuration` | 0.15 s | Shrink out |

### Scene setup (Game scene)

Place one `CombatantView` on the world-space `Canvas` to the left of the board (player) and one to the
right (enemy), and an `EncounterView` that references both views, a player `CombatantDataSO`
(**Create › GGO2026 › Combatant**) and an `EnemyDataSO` (**Create › GGO2026 › Enemy**). Optionally assign a
`DamageProjectile` prefab, and set each view's `hitPoint` to its portrait.

## Not built yet

- A full shared board/combat timeline. Tile effects' combat events already play as their tile disappears,
  and their changes fly as projectiles. Enemy turn events still show instantly.
- Run state, so player health carries over between encounters (`CreateCombatant(health)` already accepts it),
  and the setting or charm that turns this off.
- Status tooltips (`StatusDataSO.description` is not shown yet), damage numbers and health bar animation.
