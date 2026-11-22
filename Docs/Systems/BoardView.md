# Board View (presentation and input)

`Assets/Scripts/Board/View/` — turns pointer drags into `Board.TrySwap` calls and animates the
`BoardEvent`s that come back. Contains no rules.

| Type | Role |
|---|---|
| `BoardView` | `StateMachine<EBoardState>`. Builds the board in `Start`, handles input, plays events. |
| `BoardIdleState` | Accepting input. Empty state; `BoardView` checks for it. |
| `BoardAnimatingState` | Runs `BoardView.PlayEvents` as a coroutine, then returns to Idle. |
| `TileView` | One tile's sprite(s). Built entirely in code — no prefab. |
| `LineBlast` | Prefab component: one arm of a striped tile's blast. See [Line blast](#line-blast). |
| `BoardViewSettingsSO` | Layout, visuals, timing and drag tuning. |
| `PaletteSO` / `ColourSwatch` | Display colours per `TileColour`, plus outline colour/thickness. |

## Setup in a scene

The `Board` object in `Game.unity` has a `BoardView` with:

| Field | Required | Asset |
|---|---|---|
| `config` | yes | `Assets/Board Config.asset` |
| `settings` | yes | `Assets/Board View Settings.asset` |
| `startingDeck` | no | A `StartingDeckSO`. Without one every tile is a filler. |
| `encounter` | no | The scene's `EncounterView`. Its `Combat` is given to `Board.Combat` in `Start`, so combat tile effects (damage, block…) work. After each accepted swap the board view calls `encounter.AdvanceSwap()` and plays the returned enemy-turn events once the swap's animations finish. Input stops when the fight is over. Without it, none of this happens. |

It also needs an `InputManager` in a loaded scene (`GameUtilities`) and a `Camera.main` (`Camera` scene).
The board is centred on the `BoardView` transform; one cell = `CellSize` world units in its local space.

## Lifecycle

```
Awake   register Idle + Animating states
OnEnable try to subscribe to InputManager (may not exist yet)
Start   validate assets → subscribe again → new System.Random
        → TilePool(fillerTiles) → startingDeck.AddTo(pool)
        → new Board(w, h, pool, rng) → board.Combat = encounter.Combat → Play(board.Fill())
OnDisable cancel drag, unsubscribe
```

`Play(events)` stores the list and transitions to **Animating**. `BoardAnimatingState.Enter` takes the
pending events and starts the playback coroutine; when it finishes the state machine goes back to
**Idle**. Input is only accepted in Idle.

## Input

Drag-to-swap, as settled in the design:

1. `PointerPressed` → project the screen point onto the board's plane (`TryScreenToLocal`, works with
   any camera, not just orthographic) → record the cell under it.
2. `PointerMoved` → once the drag exceeds `DragThreshold × CellSize`, pick the dominant axis, compute
   the neighbour, and call `Board.TrySwap`. **One swap per drag** — the drag ends there.
3. `PointerReleased` → cancel.

Drags off the edge of the board are ignored.

## Event playback

`PlayEvents` walks the list and groups consecutive events into beats:

| Events | Animation | Timing |
|---|---|---|
| `SwapEvent` | Both tiles slide past each other; rejected swaps slide back | `SwapDuration` each way, smoothstep |
| `MatchEvent` + `TileDestroyedEvent` + `TileEffectActivatedEvent` run | Destroyed tiles pop one at a time in event order: each (including those a tile effect hits) starts shrinking `PopInterval` after the previous one (except in a [line blast](#line-blast), which sets its own times), and as soon as a tile has fully disappeared its effects' `CombatEvents` go to `EncounterView.Play(events, origin, colour)`, with the tile's world position as the origin for projectiles and its shape colour as their tint. Each tile plays `PopSound` as it starts, pitched up `PopPitchStep` per tile (capped at `PopPitchMax`). The count carries across every cascade of one playback (a swap and everything it sets off) and resets when the next playback starts. Shrinks overlap; the run ends when the last tile is gone, then all popped views are destroyed | `PopInterval` between tiles or `LineBlastSpeed` along a blast, `PopDuration` per tile, ease-in |
| `ComboCreatedEvent` run | New combo tiles are created at their cell and grow in from zero | `PopDuration`, smoothstep |
| `TileMovedEvent` + `TileSpawnedEvent` run | Falling and new tiles drop together; spawns are created at `SpawnCell` | `distance / FallSpeed`, ease-in |
| `BoardShuffledEvent` | Moved tiles slide to their new cells | `ShuffleDuration`, smoothstep |

So each cascade step reads as "pop, (combos appear,) then drop". After the last event, playback waits
until `EncounterView.IsPlaying` is false (every projectile has landed), then plays the enemy-turn
events and returns to Idle. `BoardView` keeps a `Dictionary<Tile, TileView>` to find
the view for each event; views are added on spawn and removed once their pop has fully shrunk.

The board logs a batch's effects **after** all of that batch's destroys (a colour bomb is "destroy ×n, then
effect ×n"), so `AnimatePop` reads effects with a second cursor rather than in step with the pops. It plays
each effect once its tile has left `views`, and stops at the first one still visible. Effects fire in destroy
order and every shrink has the same duration, so this launches each tile's hits the frame it disappears
without reordering combat events.

### Line blast

`SchedulePops` sets every pop and blast time before a run plays:

- A blast launches when its striped tile has disappeared, or when the last pop logged before it starts,
  if that is later. This wait keeps start times in event order, which the effect cursor relies on. A blast
  with no tiles left to hit doesn't wait.
- Each tile it hits starts to pop when the head reaches the cell's centre (`distance / LineBlastSpeed`
  after launch). The board lists line targets nearest first, so the times never decrease. A tile in two
  lines pops with the blast that the board resolved first.
- Other effects' tiles continue at `PopInterval` after the last scheduled pop.
- The run ends once every tile is gone and every head has reached the board's edge, so nothing falls
  through a moving blast. The fade can overlap the fall.

`LineClearActivatedEvent.Axis` gives the direction. `LineBlast.Create` spawns one prefab per side under
the board, tinted with the striped tile's shape colour. Timing and sound work without a prefab.

Prefab setup:

1. Root: empty object with `LineBlast`. Draw everything pointing along +X; the code rotates each arm, so
   art that is not symmetrical top to bottom appears flipped on the opposite arm.
2. `head` (optional): a child with an arrow or flare sprite, trail or particles. Set its particle
   simulation space to World for a streak. Keep trail time within `lineBlastFadeTime`, as the arm is
   destroyed when the fade ends.
3. `beam` (optional): a child at the root's origin with a left-pivot sprite (Mesh Type: Full Rect) and
   Draw Mode Sliced or Tiled. Its height is the beam's thickness; the code sets its width.
4. Give all renderers a sorting order of 3 or more (tiles use 0 to 2).
5. `tintWithTileColour` (default on) multiplies sprites, trails and single-colour particles (through
   `RendererTint`), so use white or grey art.

Move animations share reusable lists (`moveTargets`, …) because only one animation runs at a time.
Any new animation must go through `AddMove` / `RunMoves` or keep that assumption.

## TileView

`TileView.Create(parent, tile, localPosition, settings)`:

- Shape sprite: `tile.Data.Shape`, or `settings.FallbackSprite` for plain fillers / tiles without a shape.
- Shape colour: `TileDataSO.GetShapeColor` (default palette colour 2; plain fillers always use colour 2),
  outlined.
- Pattern (optional, needs a shape): child `SpriteRenderer` named `Pattern`, `TileDataSO.GetPatternColor`
  (default colour 1), sorting order +1, masked to the shape by the shader (see below), no outline.
- Icon (optional): child `SpriteRenderer` named `Icon`, `TileDataSO.GetIconColor` (default colour 3),
  sorting order +2, no outline.
- Each layer's colour source is chosen per tile: Primary, Secondary, Tertiary, Highlight or Override.
- Custom sprite: if `TileDataSO.HasCustomSprite`, the root renderer draws `CustomSprite` in white with the
  outline, and no pattern or icon layers are created.
- All layers share the tile material. Outline and mask values are set per renderer with a
  `MaterialPropertyBlock`.
- Scale: `CellSize × TileScale`, leaving a gap between tiles.

Missing palette → tiles render white. Missing or wrong tile material → no outline. Both log an error at
start. Missing fallback sprite → error per tile that needs it.

## Tile Outline shader

`Assets/Art/Shaders/TileOutline.shader` (`GGO2026/Tile Outline`), URP 2D, unlit.

- Draws the sprite tinted by the renderer colour.
- For transparent texels, samples a rounded radius (up to 3 px) for opaque neighbours and draws
  `_OutlineColor` there. `limit = r² + 1` keeps a 2 px outline round.
- Requirements on sprite import: **Mesh Type: Full Rect** and a transparent margin of at least the
  outline thickness around each slice, otherwise the outline is clipped or bleeds from neighbouring
  atlas cells.
- Texel size comes from `_MainTex.GetDimensions`, not `_MainTex_TexelSize`: the sprite texture is bound
  per renderer, so a material-level texel size would describe the wrong texture.
- **Shape mask** (patterns): when `_MaskRect` has a size, the sprite's alpha is multiplied by the alpha of
  the mask sprite (`_MaskTex`, `_MaskRect` = shape texture rect in texels; `_SpriteRect` = this sprite's
  rect). The two are aligned centre to centre, texel for texel, so the pattern and shape must share
  pixels per unit and use centred pivots. A per-renderer mask is used instead of `SpriteMask` because
  `SpriteMask` stencils affect every masked sprite, which would cut patterns into neighbouring tiles while
  they overlap during swaps.
- The `TileMaterial` in `BoardViewSettingsSO` must use this shader (`Assets/Materials/Tile.mat`), or no
  outline is drawn and patterns are not masked.
- The Tile inspector preview reproduces the outline and mask on the CPU (see [Tile Editor](TileEditor.md)).

## BoardViewSettingsSO

`Assets/Board View Settings.asset`. **Create › GGO2026 › Board View Settings**.

| Group | Field | Default | Meaning |
|---|---|---|---|
| Layout | `cellSize` | 1 | World units between cell centres |
| | `tileScale` | 0.85 | Tile size relative to the cell |
| Visuals | `fallbackSprite` | – | Sprite for plain fillers and shapeless tiles |
| | `tileMaterial` | – | `Assets/Materials/Tile.mat` (uses `GGO2026/Tile Outline`) |
| | `tilePalette` | – | `PaletteSO` |
| Timing | `swapDuration` | 0.15 s | |
| | `popDuration` | 0.15 s | How long one tile takes to shrink |
| | `popInterval` | 0.05 s | Gap between successive pops; effects add no gap of their own. 0 = all at once |
| | `fallSpeed` | 12 cells/s | |
| | `shuffleDuration` | 0.4 s | |
| Audio | `popSound` | – | `SoundProfileSO` played per popping tile through `SFXManager`. Silent when unassigned, with no clips, or with no `SFXManager` loaded |
| | `popPitchStep` | 0.04 | Pitch added per successive pop after a swap, across all its cascades. 0 = flat |
| | `popPitchMax` | 1.6 | Cap on the rising pitch |
| | `lineBlastSound` | – | `SoundProfileSO` played once as each line blast launches |
| Line Blast | `lineBlastPrefab` | – | One `LineBlast` arm. No visual when unassigned |
| | `lineBlastSpeed` | 20 cells/s | Head speed, and so the pop rhythm along a line |
| | `lineBlastFadeTime` | 0.15 s | Fade after the head reaches the edge |
| Input | `dragThreshold` | 0.35 | Fraction of a cell to drag before swapping |

## PaletteSO

`Assets/Art/Palette.asset`. **Create › GGO2026 › Palette**.

- `swatches`: one `ColourSwatch` per `TileColour` with `primary`, `secondary` and `tertiary` (colours 1–3).
  Each tile layer picks one via its `TileColourSource`; defaults are shape = secondary, pattern = primary,
  icon = tertiary. A colour with no swatch falls back to white.
- `outlineColor`, `outlineThickness` (0–3 px): shared by every tile.
- `highlightColor` (default white): one colour shared by every tile colour. Any layer can use it by setting
  its source to `Highlight`.

## Extending

- New event type → add a branch in `PlayEvents` (and decide whether it batches with pops or falls).
  Unknown events are skipped, so the board can emit gameplay-only events safely.
- New view state (e.g. game over, enemy turn) → add to `EBoardState`, create a `BaseState<EBoardState>`,
  register it in `Awake`. `CanAcceptInput` only allows input in `Idle`.
- Feedback such as SFX or screen shake belongs here, triggered from the same playback beats. Sounds go
  through `SFXManager`, with a `SoundProfileSO` reference in `BoardViewSettingsSO` (see `PlayPopSound`).
