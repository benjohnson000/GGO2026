# Board (grid rules)

`Assets/Scripts/Board/` — the match-3 rules, with no Unity presentation. Pure C# apart from `Vector2Int`
and `Debug`, so it can be unit-tested without a scene.

| Type | Role |
|---|---|
| `Board` | Owns the grid. Swaps, resolves matches/cascades, refills, reshuffles. |
| `MatchFinder` | Finds runs of 3+ and 2x2 squares, and merges overlapping ones into groups. |
| `MatchGroup` | One connected match: colour, cells, tiles, best `MatchShape` and default combo cell. |
| `MatchShape` | Plain, Square, Line4, Cross (T/L/+), Line5. |
| `Combos/ComboRule`, `ComboTable` | Which combo tile each shape makes. See [Combos](#combos). What a tile does is its own `TileEffect` ([Tile effects](#tile-effects)). |
| `BoardConfigSO` | Grid width/height, filler colours and combo rules. Encounter data later. |
| `Events/*` | `BoardEvent` subclasses describing what happened. |

## Coordinates

- Cells are `[x, y]`, `(0, 0)` at the **bottom-left**.
- Gravity pulls toward `y = 0`; new tiles enter from above (`y >= Height`).
- An empty cell is `null`. `GetTile` returns `null` for empty or out-of-bounds cells.

## Public API

```csharp
Board board = new Board(width, height, pool, rng, comboTable); // comboTable optional; null = no combos
List<BoardEvent> events = board.Fill();          // once, on an empty board
List<BoardEvent> events = board.TrySwap(a, b);   // per player swap
bool any = board.HasValidMove();
int turns = board.SwapCount;                     // accepted swaps only
```

Every mutating call **resolves completely before returning** and hands back the events in order.
Nothing is animated or delayed in here.

## Fill

`Fill()` walks the grid bottom-to-top, left-to-right. For each empty cell `DrawWithoutMatch` draws from
the pool and rejects any tile that would complete a run with the two cells to its left or the two below,
or a 2x2 square with the cells to its left, below and diagonally below-left (the only neighbours
already placed). Rejected tiles go back to the pool. After `MaxFillAttempts` (20)
the last draw is used anyway, so a single-colour deck still fills.

All spawned tiles start `Height` cells above their target, so the whole board drops in together.
`EnsureValidMove` runs afterwards.

## Swap

`TrySwap(a, b)`:

1. Returns **no events** if either cell is empty/out of bounds or the cells are not orthogonally adjacent.
2. Swaps the cells and checks `MatchFinder.HasMatchThrough` at both cells. A swap involving a tile
   with an `ActivatesOnSwap` effect (colour bomb) is always accepted, match or not.
3. No match → swaps back, emits `SwapEvent(accepted: false)`. **Does not** increment `SwapCount`.
4. Match → emits `SwapEvent(accepted: true)`, increments `SwapCount`, then `Resolve` and `EnsureValidMove`.

## Resolve loop (cascades)

```
cascade = 0
loop:
    cascade 0 and a swapped tile ActivatesOnSwap:
        destroy it (both, for two bombs) and queue its effect with SwappedWith = the other tile;
        skip matching this step (matches made by the swap resolve as cascade 1)
    else:
    find all match groups            → if none, stop
    MatchEvent(group, cascade)        for each group
    pick a combo tile + cell per group (from ComboTable, not placed yet)
    for each cell in each group:
        clear cell, TileDestroyedEvent, pool.Return(tile)   (combos/fillers are discarded)
        destroyed tile with effects → queued (cause: none)
    for each queued tile (the queue can grow):
        for each effect in its list, in order:
            effect.Activate(context) → TileEffectActivatedEvent (or null)
            Combat.Resolve() → its CombatEvents
            destroy each of its Targets (TileDestroyedEvent each; their effects queue with this as the cause)
    place combo tiles                 → ComboCreatedEvent per combo
    ApplyGravity                      → TileMovedEvent per tile that fell
    Refill (pool.Draw per empty cell) → TileSpawnedEvent per new tile
    cascade++
```

- `Cascade` on `MatchEvent` is `0` for matches made by the swap itself, `1` for the first cascade, etc.
- Destroyed tiles return to the pool **before** refilling, so they can be redrawn in the same refill
  (as the design intends).
- Refill draws plainly from the pool; it does *not* avoid matches. Those become cascades.
- `MaxCascades` (100) stops an endless chain and logs a warning.

## Reshuffle

`EnsureValidMove` checks `HasValidMove()` (tries every adjacent swap; any swap with a swap-activated tile
counts, so a colour bomb on the board always means a move exists). If there is none it calls
`Shuffle`, which:

- Collects the tiles **already on the board** (nothing is drawn or returned).
- Fisher–Yates shuffles them up to `MaxShuffleAttempts` (100) times, keeping the first layout with no
  matches and at least one valid move. Otherwise the last layout is kept.
- Emits one `BoardShuffledEvent` listing only the tiles that changed cell.

A fallback layout may contain matches, so `Resolve` runs after the shuffle. This repeats up to
`MaxShuffleRounds` (5); if there is still no move, a warning is logged (the deck may make a move
impossible, e.g. every tile a different colour).

## Matching (`MatchFinder`)

- A **run** is 3+ (`MinRun`) consecutive tiles of the same `TileColour` in a row or column.
- A **square** is a 2x2 block of one `TileColour`. It counts as a match on its own.
- `TileColour.None` and empty cells never match.
- `FindMatches` scans rows then columns for runs, then every 2x2 block for squares, then uses
  **union-find** over cells so runs and squares that share a cell (L, T, + shapes, a run with a square
  hanging off it, 2x3 / 3x3 blocks) become one `MatchGroup`. Two parallel runs that only touch
  side-by-side are *not* merged by themselves, but if their touching cells form a square of the same
  colour that square links them into one group.
- Group cells are listed bottom row first, left to right; `Cells` and `Tiles` are parallel lists.
- Internal buffers are reused between calls to avoid GC during cascades. `MatchGroup`s themselves are
  new objects each call, so events holding them stay valid.
- `HasMatchThrough(cells, cell)` and `HasAnyMatch(cells)` are cheap static checks used for swap
  validation and shuffling.
- Each group is classified into one `MatchShape` (`Classify`). The order of the checks is the combo
  priority, highest first:
  1. `Line5`: a run of 5+.
  2. `Cross`: a row run and a column run that share a cell (T, L, +).
  3. `Line4`: a run of 4.
  4. `Square`: a 2x2 block.
  5. `Plain`: anything else (runs of 3 only).

  A group with a run joined to a column run only through a square, not by sharing a cell, is not a
  `Cross`. `MatchShape` values are serialized, so append new ones; priority comes from `Classify`, not
  from the enum values.
- `ComboCell` is where a combo goes when the group has no swapped tile: the middle of the longest
  run (for 4, the second tile from the left/bottom) for lines and plain, the shared cell for `Cross`,
  and the bottom-left of the first square for `Square`.

## Combos

Combo tiles are special tiles made by special match shapes (like striped candies). Settled rules:

- **One combo per match group**, from its highest-priority shape (see above).
- **Temporary.** A combo is created on the board and is gone for good when destroyed. `TilePool.Add`
  and `Return` discard combos, so they never join the deck. The matched tiles themselves still
  return to the pool as usual.
- **Colour:** the group's colour. Each `ComboRule` lists one `TileDataSO` per colour. A colourless
  (`None`) tile in the list, such as a colour bomb, is used for any colour without its own tile; a
  colour with neither makes no combo.
- **Placement:** for matches made by the swap (cascade 0), on the dragged tile's cell if it is in the
  group, else the other swapped cell, else `ComboCell`. In cascades (and after a reshuffle), always
  `ComboCell`. The cell was just emptied by the match, so the combo then falls with gravity.
- **What it does** is the combo tile's own effects list, set on its `TileDataSO` — not part of the rule.
  The 4-line rule uses the `<Colour>-Striped` tiles, whose effect is `LineClearEffect`.
- A combo made this step is placed **after** effects fire, so a line clear never destroys a combo
  created in the same step.

`ComboTable` is a plain-C# lookup built from `BoardConfigSO.ComboRules`; the first rule for a shape
with a tile of the right colour wins.

Not designed yet: what the square, cross and 5-line combos do, and swapping two combos together.

## Tile effects

`Assets/Scripts/Tiles/Effects/`. Data-driven: a tile's behaviour is an **ordered list** of small effect
objects, `TileDataSO.effects`, a `[SerializeReference, Polymorphic] List<TileEffect>`. Each entry is a
plain-C# `[Serializable]` class stored **inside** the tile asset with its own values, so two tiles can use
the same effect class with different values (e.g. Strike and Strike+ with different damage) and one tile
can combine several (e.g. clear a line *and* deal damage) without extra assets. `TileEffect` implements
Animancer's `IPolymorphic`, and the list has `[Polymorphic]`, so the inspector draws a type picker for
each entry; add, remove and reorder entries with the normal list controls.

- `Tile.Effects` reads `Data.Effects` (empty for plain fillers). One instance is **shared by every tile
  made from that asset**, so effect fields must only hold tuning values, never per-tile state.
- The list may contain **null** entries (added in the inspector with no type picked). Everything skips
  them: use `Tile.HasEffects`, `Tile.ActivatesOnSwap` and `Tile.HasEffect<T>()` rather than reading the
  list by hand.
- **When it fires:** whenever the tile is destroyed, by a match or by another effect. Every effect
  in the list fires, in list order, with the same `TileEffectContext`; each sees the board as the previous
  one left it. (Whether combat effects should also fire on non-match destruction is still open; see
  DESIGN.md.) If any effect overrides `ActivatesOnSwap` to true, the tile also fires when swapped with any
  neighbour: the swap is a valid move even with no match, and the tile is destroyed by the swap.
- **What it does:** `Activate(TileEffectContext)` returns a `TileEffectActivatedEvent` (or a subclass
  with more detail), or null if nothing happened. Effects never change the board or combatants directly:
  - **Board:** the event lists `Targets`. The board destroys them, skipping empty and out-of-bounds cells.
    Destroyed targets return to the pool, and any with effects fire next with this event as their `Cause`.
  - **Combat:** the effect queues `CombatAction`s on `context.Combat` (null outside a fight). Straight
    after the effect returns, the board calls `Combat.Resolve()` and stores the results on the event's
    `CombatEvents`, so the view can play them with the pop that caused them. If an effect queued actions
    but returned null, the board makes a plain event for it.
- `TileEffectContext`: `Board` (read only), `Tile`, `Cell`, `SwapDirection` (dragged tile → target,
  only for tiles matched by the swap's own matches, else null), `Cause` (the effect event that destroyed
  it, null if matched or swap-fired), `SwappedWith` (the partner tile when the swap fired it, else null),
  `Combat`, `Owner` / `Opponent` (`Combat.Player` / `Combat.Enemy`, null outside a fight), `Random` (the
  board's rng).
- `Board.Combat` is set by `BoardView` from its `encounter` field. Without it, combat effects do nothing.

| Effect | Fields | Does |
|---|---|---|
| `LineClearEffect` | `clearAcrossSwap` (default on) | Destroys the rest of its row or column. Line is picked when it fires: perpendicular to a line clear that hit it; else from the swap that matched it (on: sideways swap → column, up/down → row, as in Candy Crush; off: along the swap); else random. If that line has no tiles left when it fires (emptied by the match or an earlier effect in the same step), it takes the other line, so it only destroys nothing when both are empty. `Targets` run from the tile outward (ties: left before right, below before above), which is also the order their effects fire in. Emits `LineClearActivatedEvent` with the `Axis`. |
| `ColourBombEffect` | `projectile` (optional; the board view sends one to each destroyed tile) | `ActivatesOnSwap`. Destroys every tile of one colour: the swapped tile's colour; when not fired by a swap (e.g. hit by a line clear), the most common colour on the board (ties → earlier in `TileColourUtils.All`). Swapped with another tile that has a colour bomb effect: destroys the whole board. `Targets` are shuffled with the board's rng, so tiles are destroyed, fire their effects and pop in a random order. Meant for a colourless (`None`) tile, which never matches. Emits `ColourBombActivatedEvent` with the `Colour` (`None` for the whole board). |
| `LaserEffect` | `count` (1), `preferTilesWithEffects` (default on) | Fires `count` lasers, each at a different random tile still on the board (picked with the board's rng after the match and any earlier effects in the step have destroyed theirs), and destroys them. With `preferTilesWithEffects` on, it picks from tiles that have effects first and only hits plain tiles once none are left. Destroying them which fires their effects. Fewer fire if fewer tiles are left; returns null if none are. `Targets` are sorted nearest first (straight-line distance), which is also the order their effects fire in. Emits `LaserActivatedEvent`. |
| `ExplosionEffect` | `radius` (1), `activatesOnSwap` (default on) | With `activatesOnSwap` on, `ActivatesOnSwap`: swapping it with any neighbour fires it, centred on the cell it was swapped into (so the partner, now in the bomb's old cell, is caught in the blast). Destroys every tile in the square within `radius` cells of it (1 = 3x3, 2 = 5x5), clipped to the board. Empty cells are skipped. `Targets` run from the tile outward one ring at a time (square distance), edges before corners within a ring, which is also the order their effects fire in. No visual of its own: the board view pops the targets at `PopInterval`. Emits `ExplosionActivatedEvent` with the `Radius`. |
| `DealDamageEffect` | `amount` (3) | `DealDamageAction` from owner to opponent, tagged with the tile's colour. |
| `GainBlockEffect` | `amount` (3) | `GainBlockAction` on the owner. |
| `HealEffect` | `amount` (2) | `HealAction` on the owner. |
| `ApplyStatusEffect` | `status`, `stacks` (3), `target` (Opponent/Owner) | `ApplyStatusAction`. Logs an error and does nothing without a status. |

The four combat effects derive from `CombatTileEffect`, which returns null outside a fight, otherwise
queues the action from `CreateAction(context)` and returns a plain `TileEffectActivatedEvent`.

### Adding an effect

1. Add a `[Serializable]` class in `Tiles/Effects/`, with tuning values as `[SerializeField] private`
   fields and a parameterless constructor:
   - **Combat effect** (one action): derive from `CombatTileEffect` and override `CreateAction`.
   - **Anything else:** derive from `TileEffect`, override `Activate` and return a
     `TileEffectActivatedEvent` (subclass it if listeners need more detail). Queue any combat actions on
     `context.Combat` rather than running them.
2. It appears in each entry's type picker in the tile inspector automatically, with the base class's
   optional `projectile` field ([projectile style](Combat.md#projectile-styles)) for how its hits fly.
   If the effect never sends a projectile (no combat actions, no projectiles of its own), override
   `UsesProjectile` to return false and the inspector hides that field (`ProjectileFieldAttribute`, drawn by
   `Editor/ProjectileFieldDrawer`). `LineClearEffect` does this.
3. Renaming or moving the class breaks existing references; use `[UnityEngine.Scripting.APIUpdating.MovedFrom]`.

## Board events

`Assets/Scripts/Board/Events/`. All derive from the empty `BoardEvent` base.

| Event | Fields | Emitted when |
|---|---|---|
| `SwapEvent` | `TileA`, `TileB`, `CellA`, `CellB`, `Accepted` | Every legal swap attempt |
| `MatchEvent` | `Group`, `Cascade` | A match group is found. Followed by its `TileDestroyedEvent`s |
| `TileDestroyedEvent` | `Tile`, `Cell` | A tile is cleared and returned to the pool (combos and fillers are discarded) |
| `TileEffectActivatedEvent` | `Effect`, `Tile`, `Cell`, `Targets`, `CombatEvents` | One effect of a destroyed tile fires (one event per effect that did something). Followed by the `TileDestroyedEvent`s it causes |
| `LineClearActivatedEvent` | + `Axis` | Subclass for `LineClearEffect` |
| `ColourBombActivatedEvent` | + `Colour` | Subclass for `ColourBombEffect`. `None` = whole board |
| `LaserActivatedEvent` | – | Subclass for `LaserEffect`. `Targets` are the cells shot, nearest first |
| `ExplosionActivatedEvent` | + `Radius` | Subclass for `ExplosionEffect`. `Targets` run from the centre outward, ring by ring |
| `ComboCreatedEvent` | `Tile`, `Cell`, `Group` | A match made a combo tile, after all of the step's destroys and before gravity |
| `TileMovedEvent` | `Tile`, `From`, `To` | A tile falls during gravity |
| `TileSpawnedEvent` | `Tile`, `Cell`, `SpawnCell` | A tile is drawn into a cell. `SpawnCell.y >= Height` |
| `BoardShuffledEvent` | `Moves` (`TileMove` list) | No valid moves remained |

Typical order for one accepted swap with one cascade:

```
Swap(accepted) → Match(c0) → Destroyed×n [→ EffectActivated → Destroyed×r …] [→ ComboCreated×c]
                           → Moved×k → Spawned×m
               → Match(c1) → Destroyed×n → Moved×k → Spawned×m
               [→ Shuffled → (Match/Destroyed/Moved/Spawned …)]
```

### Hooking gameplay onto events

Tile effects reach combat through `Board.Combat` (see [Tile effects](#tile-effects)). Enemy intents tick
once per accepted swap, after the board has resolved; `BoardView` calls the encounter (see
[Encounter](Encounter.md)). Charms are not built yet, but the intended hook is the event list:

- Iterate the returned `List<BoardEvent>` (or have the board raise them) and react to `MatchEvent`
  (every tile in `Group.Tiles` triggers its effect), `TileDestroyedEvent`, cascade index, and
  `SwapCount` changes (turn counter for enemy countdowns).
- Keep that logic in plain C# next to the board, not in `BoardView`, so it does not depend on
  animation timing.
- Effect order and how effects interact with cascades are **open design questions** — check
  [DESIGN.md](../DESIGN.md#open-questions) and ask before deciding.

## Randomness

`Board` and `TilePool` take a `System.Random`. `BoardView` creates one and shares it between them.
Pass a seeded instance for reproducible boards (tests, daily seeds).

## Safety limits

| Constant | Value | Purpose |
|---|---|---|
| `MaxFillAttempts` | 20 | Draws per cell during the no-match fill |
| `MaxShuffleAttempts` | 100 | Shuffles tried per round |
| `MaxShuffleRounds` | 5 | Shuffle + resolve rounds before giving up |
| `MaxCascades` | 100 | Cascades per resolve |

These guarantee an operation always terminates, whatever the deck looks like.

## BoardConfigSO

`Assets/Board Config.asset`. **Create › GGO2026 › Board Config**.

| Field | Default | Notes |
|---|---|---|
| `width`, `height` | 6 | Grid size. The design treats this as per-encounter data. |
| `fillerTiles` | empty | `TileDataSO`s a filler can be, picked at random. Empty/invalid → plain fillers of any colour. |
| `comboRules` | empty | `ComboRule`s: `shape` and one `tiles` entry per colour. Empty → no combos. |

Blocked cells / non-rectangular grids (planned in the design) are **not** supported yet; `Board`
assumes every cell is playable.
