# Board (grid rules)

`Assets/Scripts/Board/` — the match-3 rules, with no Unity presentation. Pure C# apart from `Vector2Int`
and `Debug`, so it can be unit-tested without a scene.

| Type | Role |
|---|---|
| `Board` | Owns the grid. Swaps, resolves matches/cascades, refills, reshuffles. |
| `MatchFinder` | Finds runs of 3+ and 2x2 squares, and merges overlapping ones into groups. |
| `MatchGroup` | One connected match: colour, cells and tiles. |
| `BoardConfigSO` | Grid width/height and filler colours. Encounter data later. |
| `Events/*` | `BoardEvent` subclasses describing what happened. |

## Coordinates

- Cells are `[x, y]`, `(0, 0)` at the **bottom-left**.
- Gravity pulls toward `y = 0`; new tiles enter from above (`y >= Height`).
- An empty cell is `null`. `GetTile` returns `null` for empty or out-of-bounds cells.

## Public API

```csharp
Board board = new Board(width, height, pool, rng);
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
2. Swaps the cells and checks `MatchFinder.HasMatchThrough` at both cells.
3. No match → swaps back, emits `SwapEvent(accepted: false)`. **Does not** increment `SwapCount`.
4. Match → emits `SwapEvent(accepted: true)`, increments `SwapCount`, then `Resolve` and `EnsureValidMove`.

## Resolve loop (cascades)

```
cascade = 0
loop:
    find all match groups            → if none, stop
    MatchEvent(group, cascade)        for each group
    for each cell in each group:
        clear cell, TileDestroyedEvent, pool.Return(tile)
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

`EnsureValidMove` checks `HasValidMove()` (tries every adjacent swap). If there is none it calls
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

## Board events

`Assets/Scripts/Board/Events/`. All derive from the empty `BoardEvent` base.

| Event | Fields | Emitted when |
|---|---|---|
| `SwapEvent` | `TileA`, `TileB`, `CellA`, `CellB`, `Accepted` | Every legal swap attempt |
| `MatchEvent` | `Group`, `Cascade` | A match group is found. Followed by its `TileDestroyedEvent`s |
| `TileDestroyedEvent` | `Tile`, `Cell` | A tile is cleared and returned to the pool |
| `TileMovedEvent` | `Tile`, `From`, `To` | A tile falls during gravity |
| `TileSpawnedEvent` | `Tile`, `Cell`, `SpawnCell` | A tile is drawn into a cell. `SpawnCell.y >= Height` |
| `BoardShuffledEvent` | `Moves` (`TileMove` list) | No valid moves remained |

Typical order for one accepted swap with one cascade:

```
Swap(accepted) → Match(c0) → Destroyed×n → Moved×k → Spawned×m
               → Match(c1) → Destroyed×n → Moved×k → Spawned×m
               [→ Shuffled → (Match/Destroyed/Moved/Spawned …)]
```

### Hooking gameplay onto events

Effects, charms and enemy intents are not built yet, but the intended hook is the event list:

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

Blocked cells / non-rectangular grids (planned in the design) are **not** supported yet; `Board`
assumes every cell is playable.
