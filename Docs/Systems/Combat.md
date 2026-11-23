# Combat (health, block, statuses)

`Assets/Scripts/Combat/`: the fight between the player and one enemy. Like the [Board](Board.md), the rules
are plain C# that resolve instantly and return an ordered list of events; views only play them back.

| Type | Role |
|---|---|
| `Combatant` | One side: `Health`, `MaxHealth`, `Block`, `IsDead`, `Statuses`. Holds state only. |
| `Combat` | Owns both combatants, the action queue and non-status listeners. All changes go through it. |
| `CombatAction` (`Actions/`) | A queued change: `DealDamageAction`, `GainBlockAction`, `HealAction`, `ApplyStatusAction`, `StartTurnAction`, `EndSwapAction`, `SetCountdownAction`, `ChangeCountdownAction`. |
| `CombatEvent` (`Events/`) | What happened. Carries the values after the change. |
| `DamageInfo`, `DamageKind` | One hit going through the damage pipeline. |
| `CombatListener` | Base for statuses, charms and affixes: modifier and trigger hooks. |
| `StatusEffect`, `StatusDataSO` (`Status/`) | A status's runtime stacks and behaviour, and its asset. |
| `PoisonStatusSO`, `PoisonStatus` | The first status, and the example to copy. |
| `CombatantDataSO` | Name and max health for a player or enemy. |
| `DamagePreview`, `PreviewedDamage` | What statuses will cost at the owner's next turn start. See [Damage preview](#damage-preview). |
| `CombatantView`, `EncounterView` (`View/`) | Placeholder UI and the scene component that builds a `Combat`. |
| `HealthBarView`, `HealthBarModel`, `HealthBarSettingsSO` (`View/`) | Layered health bar, its underfill logic and its tuning. See [Health bar](#health-bar). |
| `StatusRowView`, `StatusIconView` (`View/`) | Status icons under each combatant (block is on the health bar). See [Status icons](#status-icons). |
| `DamageProjectile` (`View/`) | Prefab component for any tile effect in flight (to a combatant, or a laser to a tile), despite the name. See [Views](#views). |
| `ProjectileStyleSO` (`View/`) | Which projectile prefab an effect uses and how it flies, lands and sounds. See [Projectile styles](#projectile-styles). |

## Public API

```csharp
Combat combat = new Combat(playerData.CreateCombatant(), enemyData.CreateCombatant());
List<CombatEvent> events = combat.Execute(new DealDamageAction(source, target, 3, TileColour.Red));
List<CombatEvent> events = combat.StartTurn(combat.Enemy);

combat.Enqueue(actionA);                  // queue several, then
List<CombatEvent> events = combat.Resolve();

combat.AddListener(charm, combat.Player); // charms, affixes (statuses are added by ApplyStatusAction)
DamagePreview preview = combat.PreviewDamage(combat.Enemy); // changes nothing
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

All `Modify*` hooks must be free of side effects, because [previews](#damage-preview) run the damage and
block-loss ones for hits that haven't happened. "The first hit each fight deals less" reduces in the
modifier and gets used up in `OnDamageDealt`.

Modifiers should only add or subtract (DESIGN.md asks charms to stay additive). Multipliers such as colour
weaknesses still need an agreed order.

## Turns and block

`StartTurn(combatant)` logs `TurnStartedEvent`, then removes the combatant's block, then fires
`OnTurnStarted` for every listener. The owner's listeners can reduce the block lost with `ModifyBlockLost`
(for example, a charm that keeps half). Per-turn poison ticks in `OnTurnStarted`, after block is gone.

`EndSwapAction` fires `OnSwapEnded(combat)` for every listener (the player's first). `Encounter.AdvanceSwap`
runs it once per accepted swap, before the countdown ticks, and stops there if someone died. Per-swap
poison ticks here; passives could use it too.

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
| `OnDamageDealt`, `OnBlockChanged`, `OnHealed`, `OnStatusChanged`, `OnTurnStarted`, `OnSwapEnded`, `OnCountdownChanged`, `OnCombatantDied` | Every listener | Triggers. Check `Owner` |

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

**Create › GGO2026 › Status › Poison**. Each tick it deals damage equal to its stacks
(`DamageKind.Status`, no source), then loses `stacksLostPerTick` (default 1) stacks. `tickType`
(`StatusTickType`) sets when it ticks:

| `tickType` | Ticks |
|---|---|
| `PerTurn` (default) | At the start of its owner's turn (`OnTurnStarted`), i.e. when the enemy intent fires |
| `PerSwap` | After every accepted swap (`OnSwapEnded`), before the countdown ticks. Both sides' poison ticks |

`ignoresBlock` (default on) is a placeholder until the design settles it. Per turn, block is lost before
poison ticks, so the toggle only matters when block is preserved (`ModifyBlockLost`) or gained again first.
Per swap, block is not lost first, so the toggle matters more.

### Damage preview

`Combat.PreviewDamage(combatant)` returns the health each status would cost if that combatant's turn
started now, in trigger order. It uses the same damage and block-loss code as the real turn:

- Block the combatant would keep absorbs hits that don't ignore block, and is used up from one hit to the next.
- Damage modifiers apply. Hits stop at 0 health, and statuses that would cost nothing are left out.
- It can't foresee triggers: block gained at turn start, or reactions to the hits themselves.

To show a new status (burn, bleed, etc.) that deals damage at turn start, override
`StatusEffect.CreateTurnStartDamage()` and deal that same `DamageInfo` in `OnTurnStarted`, as `PoisonStatus`
does. The health bar needs no changes. Damage at any other time isn't previewed yet, except per-swap
poison, which previews its next tick the same way (one tick, even though it may tick several times before
the turn).

## Events

All derive from `CombatEvent`, which has `Target` (the combatant it happened to) and `GetPreview(combatant)`,
either side's [damage preview](#damage-preview) just after the event (null for events `Combat` didn't log).

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

- `CombatantView`: a placeholder UI with optional references (name, [`HealthBarView`](#health-bar) for the
  bar, health number and block, status row). `Bind(combatant, preview)` copies
  the current state, and `Apply(event)` updates from events about its combatant (and the damage preview from
  any event), so the display follows playback rather than live state.
- `CombatantView.intentText` (enemy only) shows the intent and countdown, e.g. "Attack 6 in 3".
- **Hit sounds** (optional `SoundProfileSO`s on `CombatantView`, played through `SFXManager` when a
  `DamageEvent` is shown, so for tile damage when its projectile lands). Each stands for one thing that
  happened, so one hit can play two:

  | Field | Plays when |
  |---|---|
  | `damageBlockedSound` | Block absorbed the hit and some is left (`Blocked > 0`, `BlockAfter > 0`) |
  | `shieldBrokenSound` | The hit took block to 0 (`Blocked > 0`, `BlockAfter == 0`), with the [block break](#health-bar) |
  | `damageUnblockedSound` | The hit cost health (`HealthLost > 0`), with the portrait flash. Formerly `damageSound` |

  A hit that breaks the block and gets through plays the broken and unblocked sounds together. A 0-damage hit
  is silent. `CombatantView.prefab` assigns `Assets/Audio/DamageBlocked`, `ShieldBroken` and
  `DamageUnblocked`; override them on a view in the scene to give the player and enemy different sounds.
- **Damage flash:** at the same moment as `damageUnblockedSound` (a `DamageEvent` with `HealthLost > 0`),
  `CombatantView.portraitFlash` plays: a [`GraphicFlash`](Utilities.md#misc) that turns the portrait solid
  white for 0.08 s, then snaps back. Left empty, it is the `GraphicFlash` on `hitPoint`, which in
  `CombatantView.prefab` is the portrait. Without one, there is no flash. Colour, hold and fade times are set
  on the `GraphicFlash`. A hit mid-flash restarts it.
  - **Setup:** `CombatantView.prefab`'s `portrait` already has a `GraphicFlash` with
    `Assets/Art/Shaders/UIFlash.shader` (`GGO2026/UI Flash`) as its `flashShader`. A new portrait needs the same.
- `EncounterView`: holds the player and enemy data and their views. It builds the `Encounter` and its
  `Combat` in `Awake` (so a `BoardView` can pick it up in `Start`), binds the views and picks the first
  intent in `Start`. `Play(events)` sends each event to both views and shows `victoryRoot`/`defeatRoot` on a
  death. `BoardView` calls `Play(events, origin, colour, style)` with each tile effect's `CombatEvents`, the tile's world
  position, its shape colour and the effect's `Projectile` style as soon as that tile has disappeared, and `Play(events)` with the enemy-turn events after a swap. See
  [Encounter](Encounter.md).
- **Projectiles.** Every change played with an origin (i.e. from a tile) flies from the tile to the affected
  combatant's `HitPoint` in a [projectile style](#projectile-styles): the firing effect's own `Projectile`,
  else `EncounterView.defaultProjectile`. The change is shown when it lands. The event's `Target` picks the side.
  - Flies: `DamageEvent`, `BlockChangedEvent`, `HealedEvent`, `StatusChangedEvent`, `CountdownChangedEvent`
    (see `EncounterView.FliesToTarget`). A new event type that should fly goes in that list.
  - Doesn't fly: `CombatantDiedEvent`, `TurnStartedEvent`, `IntentChangedEvent`. They wait in the queue.
  - Colour: the tile's shape colour, as drawn on the board (`ProjectileStyleSO.tintWithTileColour`, default on).
    The colour shows the tile; the style shows the effect.
  - One projectile per event, so a tile with two effects sends two, each in its own effect's style, and a
    charm's reaction to a tile also flies from that tile. An effect that changes nothing (a heal at full
    health) logs no event, so sends none.
  - With no style (or a style with no prefab), or for events played without an origin (enemy turns, poison
    ticks, debug actions), changes show instantly.
  - `CombatantView.hitPoint` is the prefab's portrait. Left empty, it is the view's pivot, which is at the
    screen-edge side of each panel.
  - When the fight's scene unloads mid-flight, the callback is ignored so it doesn't update destroyed UI.
- **Event queue.** `EncounterView` shows events strictly in order: every event goes into a queue, and the
  front is shown only once it is ready. A projectile's event is not ready until it lands, so later events
  (death, the next change) wait behind it. Projectiles still launch straight away, so a run of effect
  tiles sends them in quick succession. `IsPlaying` is true while anything is queued; `BoardView` waits for
  it before playing the enemy turn and returning to Idle, so every projectile lands before the enemy acts.
- **`DamageProjectile`** (component on the prefab): flies a quadratic arc from the tile to the target,
  following it if it moves, and calls back on impact, all as its `ProjectileStyleSO` says. On impact it
  spawns the style's `impactPrefab`, plays its `impactSound`, hides its sprites, stops its trails and
  particle systems emitting, and destroys itself after `lingerTime` so they fade out. If it is destroyed
  mid-flight it still releases its event, so playback never stalls.
  `Launch(style, start, targetTransform, colour, callback)` follows a `Transform`;
  `Launch(style, start, end, colour, callback = null)` flies to a fixed world point (used by the board's
  [lasers and colour bombs](BoardView.md#laser-and-colour-bomb-projectiles)). Its only own field is `lingerTime` (0.5 s), as it depends on the prefab's
  trails and particles.

  The world-space `Canvas` and the board share world space, so positions need no conversion. Give the
  prefab's renderers a sorting layer/order that draws above the board and the canvas.

### Projectile styles

`ProjectileStyleSO` (`View/`, **Create › GGO2026 › Projectile Style**) is how one kind of projectile looks
and flies. Every `TileEffect` has an optional `Projectile` field (first in its inspector entry) that picks
one, so each effect defines its own projectile, and several styles can share one prefab with different
tuning. An effect with none uses the default: `EncounterView.defaultProjectile` for combat hits,
`BoardViewSettingsSO.defaultLaserProjectile` for laser shots. A colour bomb has no default: with a style it
sends a projectile to each tile it destroys, without one it sends none
([Board View](BoardView.md#laser-and-colour-bomb-projectiles)). Line clears never send one: their
`UsesProjectile` is false, so the inspector hides the field on them (see
[Board › Adding an effect](Board.md#adding-an-effect)).

| Field | Default | Meaning |
|---|---|---|
| `prefab` | – | The `DamageProjectile` spawned. Empty = nothing flies, hits land at once |
| `duration` | 0.35 s | Launch to impact |
| `arcHeight` | 1 | Upward bow at the middle of the path, world units. 0 = straight |
| `arcSpread` | 0.75 | Random sideways bow per projectile, so a run of hits fans out |
| `easePower` | 2 | 1 = constant speed; higher accelerates into the hit |
| `faceDirection` | off | Rotates the projectile to face where it is heading. Draw the prefab pointing along +X (lasers, arrows) |
| `impactPrefab` | – | Optional, spawned at the impact point. Destroy it from its own settings |
| `tintWithTileColour` | on | Multiplies sprites, trails and single-colour particles (the impact's too) by the tile's colour, through `RendererTint`. Use white or grey art |
| `launchSound` | – | Optional `SoundProfileSO`, played as each projectile launches |
| `impactSound` | – | Optional `SoundProfileSO`, played as each projectile lands |

`TileEffect.Projectile` is presentation only: the board and combat rules never read it.
- **Debug:** in Play mode, open the `EncounterView` component's ⋮ menu for *Debug: Damage Enemy / Damage
  Player / Player Gains Block / Heal Player / Apply Status … / Delay Enemy Intent / Advance Swap*. They use
  `debugAmount` and `debugStatus`.

### Health bar

`HealthBarView` is a component on the bar's root, so the bar can be its own prefab. Assign it to
`CombatantView.healthBar`. Without one, the view shows no bar, health number or block.

| Field | Required | Meaning |
|---|---|---|
| `healthFill` | yes | Front fill. Set its pivot to the end it should shrink toward |
| `underfill` | no | Your own underfill Image, which keeps its colour and art. Same parent, rect and pivot as the fill, before it in the hierarchy |
| `healthText` | no | Shows "health / max health" |
| `blockRoot` | no | Root of the block icon and number. Active only while block is above 0 |
| `blockText` | no | Shows the amount of block |
| `blockIcon` | no | The whole block icon. Hidden while it breaks |
| `brokenBlockLeft`, `brokenBlockRight` | no | The broken icon's halves, placed over the whole icon. Hidden at rest. The break needs these and `blockIcon` |
| `settings` | no | A `HealthBarSettingsSO` |

```
Healthbar      HealthBarView
├─ bg
├─ Underfill   optional
├─ fill        status layers are inserted just before it at runtime
├─ Frame       optional
├─ Health      text
└─ BlockRoot   icon and number, drawn over everything
   ├─ left     broken halves, optional
   ├─ right
   ├─ Icon
   └─ BlockText
```

Layers are rectangles scaled on X. Status layers, and the underfill if none is assigned, are copies of the
fill made when the bar is first bound. Back to front:

| Layer | Ends at | Colour |
|---|---|---|
| Underfill | Health before the current chain of hits | An assigned Image's own, otherwise `damageColour` |
| One per previewed status, first to hit at the back | Health left before that status hits | `StatusDataSO.BarColour` |
| Health fill | Health left after every previewed status | The Image's own, or `blockedHealthColour` with block |

Each layer covers the start of the one behind it, so any number of statuses read as separate bands. If
they cover all remaining health, the next turn start is lethal.

- **Hierarchy:** layers are inserted directly behind the fill. Put a frame and the text after the fill. To
  keep the bar inside a frame, inset the fill's rect; the copies follow it.
- **Block** is shown here, not as a status icon: `blockRoot` and `blockText`, plus the fill's tint. It follows
  playback like health, so it changes when the event is shown.
- **Block break:** when a hit takes block from above 0 to 0 (`DamageEvent` with `Blocked > 0` and
  `BlockAfter == 0`), `CombatantView` calls `HealthBarView.BreakBlock()` before showing the hit. The icon and
  number hide, the two halves appear where the icon was, slide apart horizontally (eased out) and fade, then
  `blockRoot` hides. Block lost at turn start doesn't break, it just disappears. Gaining block mid-break cancels
  it and shows the whole icon again. Without the icon and both halves, the block just disappears.
- **Band colour:** a status's `colour`, or its `barColour` with `overrideBarColour` on (for full-colour
  icons, whose `colour` is white). Always opaque.
- **Damage:** the fill moves as each hit is shown and the underfill stays. `EncounterView.Settle()` makes it
  wait `holdDuration`, then drain over `drainDuration`. Another hit pauses the drain until the next settle.
  Healing grows the fill over the underfill.
- **Settling:** whoever plays a chain of events calls `Settle()` after the last of them; it takes effect once
  the queue has been shown. `BoardView` does so after the board and any enemy turn have played. Don't use
  `IsPlaying` for this: the queue also empties between cascades.
- **Tuning:** optionally create **Create › GGO2026 › Health Bar Settings** and assign it to
  `HealthBarView.settings` on the prefab. It is read when the bar is first bound, so restart Play after
  editing it.

| Field | Default | Meaning |
|---|---|---|
| `damageColour` | muted tan | Generated underfill (not an assigned one). Keep it duller than status colours |
| `blockedHealthColour` | blue | Health fill while there is block |
| `holdDuration` | 0.3 s | Wait after settling |
| `drainDuration` | 0.4 s | Underfill catching up |
| `blockBreakDuration` | 0.35 s | Broken halves sliding apart and fading. 0 hides the block straight away |
| `blockBreakDistance` | 0.75 | How far each half slides, in canvas units (the block icon is 2 wide) |

### Status icons

`CombatantView.statusRow` is a `StatusRowView`, which makes a copy of its `iconPrefab`
(`Assets/Prefabs/StatusIconView.prefab`) for each status. Without a row, statuses aren't shown. A row without
an icon prefab logs an error.

The prefab owns the icon's look: size, layout, font, badge and colours. On its root is a `StatusIconView`:

| Field | Required | Meaning |
|---|---|---|
| `iconImage` | yes | Gets the status's sprite and colour |
| `countText` | yes | Gets the stacks. Let layout components size the badge around it |
| `letterText` | no | The status's first letter, shown only while it has no icon |

The root's size and scale set the space each icon takes. Its anchors, pivot and position are set by the row.

- **Order:** the order statuses were shown. A status that ran out and is applied again goes to the end.
  Lines are centred and wrap downward.
- **Block** never appears here. It is a field on `Combatant`, not a status, and the [health bar](#health-bar) shows it.
- **Look:** `StatusDataSO.icon` tinted by `StatusDataSO.colour` (white leaves full-colour art as it is).
  Without an icon, it is a square of that colour, with the first letter of the status's name if the prefab
  has a `letterText`.
- **Outline:** `iconImage` gets a 2px [`PixelOutline`](Utilities.md#misc) at runtime, in the tiles' default
  outline colour. It doesn't follow the palette. To tune it, or turn it off with thickness 0, add a
  `PixelOutline` to that Image in the prefab and it is used instead. A pixel is 1/16 of a canvas unit, so draw
  icons at that scale (a 32px slice in a 2-unit rect), or the outline's pixels won't match the art's.
- **Animation:** icons pop in, pulse when the count changes (so a poison tick pulses as it loses a stack)
  and shrink out at 0. Whatever is active at `Bind` appears without animating.
- **Timing:** icons change when the `EncounterView` queue shows the event, not when the rules resolve.

Icons hang from the row rect's top centre, so move that rect to move them. On `StatusRowView`:

| Field | Default | Meaning |
|---|---|---|
| `iconPrefab` | – | Copied for each status |
| `spacing` | 0.25 | Gap between icons, across and down, in canvas units (world units on this canvas). Negative overlaps them; later icons draw on top |
| `iconsPerLine` | 5 | Icons before wrapping |
| `pulseScale`, `pulseDuration` | 1.35, 0.25 s | Pop and pulse |
| `removeDuration` | 0.15 s | Shrink out |

### Scene setup (Game scene)

Place one `CombatantView` on the world-space `Canvas` to the left of the board (player) and one to the
right (enemy), and an `EncounterView` that references both views, a player `CombatantDataSO`
(**Create › GGO2026 › Combatant**) and an `EnemyDataSO` (**Create › GGO2026 › Enemy**). Optionally assign a
`defaultProjectile` ([projectile style](#projectile-styles)), set each view's `hitPoint` to its portrait, its `healthBar` to its
[health bar](#health-bar) and its `statusRow` to its [status row](#status-icons), whose `iconPrefab` is the
status icon prefab.

## Not built yet

- A full shared board/combat timeline. Tile effects' combat events already play as their tile disappears,
  and their changes fly as projectiles. Enemy turn events still show instantly.
- Run state, so player health carries over between encounters (`CreateCombatant(health)` already accepts it),
  and the setting or charm that turns this off.
- Status tooltips (`StatusDataSO.description` is not shown yet) and damage numbers.
- Health bar overlays that aren't turn-start damage, such as a kill threshold growing from the left.
