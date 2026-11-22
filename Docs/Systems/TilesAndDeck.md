# Tiles and Deck

`Assets/Scripts/Tiles/` â€” tile definitions, runtime tiles, and the deck-as-pool.
Design background: [DESIGN.md â€º Tiles](../DESIGN.md#tiles) and [â€º Deck](../DESIGN.md#deck).

| Type | Kind | Role |
|---|---|---|
| `TileDataSO` | ScriptableObject | Definition of a deck tile: name, colour, effects, shape, pattern, icon |
| `Tile` | Plain C# | One physical tile, in the pool or on the board |
| `TilePool` | Plain C# | The deck. Draw / return, fillers when empty |
| `StartingDeckSO` | ScriptableObject | List of `DeckEntry` (tile + count) used to seed a pool |
| `DeckEntry` | Serializable | One line of a deck asset |
| `TileColour` | Enum | Match colour |
| `TileColourUtils` | Static | `All`, `IsMatchable`, `PickRandom` |

## Definition vs. instance

- `TileDataSO` is the **design** of a tile. A deck can list it many times.
- `Tile` is a **physical copy**. `StartingDeckSO.AddTo` creates one `new Tile(data)` per copy.
- Tiles are compared **by reference**: three copies of "Red Plain" are three distinct tiles. `BoardView`
  relies on this to map each tile to its `TileView`.
- `Tile.Colour` is copied from the data at construction.

## Filler tiles

- Created when `TilePool.Draw()` is called on an empty pool: `Tile.CreateFiller(data)` with a random
  entry from `BoardConfigSO.FillerTiles`, so fillers use that tile's colour, shape, pattern and icon.
- If there are no valid filler tiles, `Tile.CreateFiller(colour)` makes a plain filler (`Data == null`,
  random colour) drawn with `BoardViewSettingsSO.FallbackSprite`. `BoardView` logs a warning.
- `IsFiller` is an explicit flag, not `Data == null`. Fillers should use tiles with no effect.
- **Never** part of the deck: `TilePool.Add` and `TilePool.Return` silently discard fillers.

## Combo tiles

- Made on the board by special match shapes with `Tile.CreateCombo(data)`. `IsCombo` is true; what it
  does is its data's effects. See [Board › Combos](Board.md#combos).
- Temporary, like fillers: `TilePool.Add` and `TilePool.Return` discard them, so they never join the deck.
- Their `TileDataSO`s live in `Assets/Tiles/Data/Combos/` (e.g. `4-line/<Colour>-Striped`) and are
  listed in `BoardConfigSO.comboRules`.
- `IsCombo` belongs to the copy, not the asset: a combo's `TileDataSO` listed in a starting deck (e.g. a
  colour bomb) makes normal deck tiles, which return to the pool when destroyed.

## TilePool

```
             Draw()  (random, O(1) swap-remove)
   â”Œâ”€â”€â”€â”€â”€â”€â” â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º â”Œâ”€â”€â”€â”€â”€â”€â”€â”
   â”‚ Pool â”‚                                   â”‚ Board â”‚
   â””â”€â”€â”€â”€â”€â”€â”˜ â—„â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ â””â”€â”€â”€â”€â”€â”€â”€â”˜
             Return(tile)  (when destroyed; fillers dropped)
```

- `Count` is the number of tiles currently **in the pool**, not the deck size. Tiles on the board are
  not counted.
- There is no discard pile or reshuffle â€” destroyed tiles go straight back.
- Filler tiles are de-duplicated and filtered to matchable colours; `HasFillerTiles` is false when
  none are left, and fillers fall back to plain tiles of any of the five colours.

## Colours

```csharp
public enum TileColour { None = 0, Red = 1, Blue = 2, Green = 4, Yellow = 8, Purple = 16 }
```

- Values are non-contiguous because they used to be flags (multi-colour tiles were dropped by design).
  **Serialized in assets â€” append new values, never renumber.**
- `None` (colourless) never matches, but is allowed in decks (e.g. colour bombs). Such a tile only leaves
  the board when an effect destroys it or, if it activates on swap, when swapped.
- `StartingDeckSO.AddTo` logs an error and skips tiles whose colour is neither `None` nor in
  `TileColourUtils.All` (e.g. a leftover multi-colour value). Fillers must still have a matchable colour.
- `TileColourUtils.All` is the authoritative list of matchable colours. If a colour is added (e.g. black
  enemy tiles, wild tiles), update `All` deliberately â€” it drives filler rolls and validation.

## StartingDeckSO

**Create â€º GGO2026 â€º Starting Deck**. A list of `{ tile, count }`.

```csharp
TilePool pool = new TilePool(config.FillerTiles, rng);
startingDeck.AddTo(pool);
```

It stands in for the run's deck until run state exists. When runs are built, the deck should become
runtime data (a list of `Tile`s or `TileDataSO`s that persists between encounters) and the starting
deck asset only seeds it.

## TileDataSO

**Create â€º GGO2026 â€º Tile**, or use the [Tile Editor](TileEditor.md). Assets live in `Assets/Tiles/Data/`.

| Field | Notes |
|---|---|
| `displayName` | Falls back to the asset name |
| `colour` | Renamed from `colours` (kept via `FormerlySerializedAs`) |
| `customSprite` | Optional hand-made sprite. When set, it replaces shape, pattern and icon (see below) |
| `shape` | A slice of `Assets/Art/tile_shapes.png` (white, tinted at runtime) |
| `shapeColorSource`, `shapeColor` | Shape tint source (default Secondary); `shapeColor` is used for Override |
| `pattern` | Optional slice of `Assets/Art/tile_patterns.png`, masked to the shape |
| `patternColorSource`, `patternColor` | Pattern tint source (default Primary) |
| `icon` | Optional slice of `Assets/Art/tile_icons.png` |
| `iconColorSource`, `iconColor` | Icon tint source (default Tertiary) |
| `effects` | Ordered `[SerializeReference]` list of `TileEffect`s, any number. Was the single `effect` field (combo assets migrated) |

`TileColourSource` is `Primary`, `Secondary`, `Tertiary` (palette colours 1â€“3 for the tile's `colour`),
`Highlight` (the palette's shared highlight colour, the same for every tile colour) or `Override` (the
layer's own colour field). It is serialized: append values, never renumber.

Visuals are composed at runtime (outlined shape, pattern masked to it, icon on top), so there is no baked
sprite per tile. `GetShapeColor`, `GetPatternColor` and `GetIconColor(palette)` resolve each layer's tint
through the static `ResolveColor`, which the editor preview also uses. Plain fillers with no data use
palette colour 2, matching the default shape source.

### Custom sprites

`customSprite` (`HasCustomSprite`) is for tiles that need hand-drawn art. When set, `TileView` draws only
that sprite, untinted (its own colours) and with the palette outline; shape, pattern, icon and their colour
sources are ignored but kept, so clearing it restores the generated look. `colour` still decides matching.
Art should be 32Ã—32 at 16 PPU with a centred pivot, Mesh Type: Full Rect and â‰¥2 px transparent margin for
the outline.

## Effects

`TileDataSO.effects` is an ordered `[SerializeReference]` list of `TileEffect`s. Entries are added, removed
and reordered in the tile inspector, each with a type picker (Animancer's polymorphic drawer), and stored
inside the tile asset with their own values. How effects fire and how to add one is in
[Board › Tile effects](Board.md#tile-effects). Board effects: `LineClearEffect`, `ColourBombEffect`. Combat
effects: `DealDamageEffect`, `GainBlockEffect`, `HealEffect`, `ApplyStatusEffect`. Trigger order within a
match and cascade interaction are still **open design questions**.
Constraints already settled by the design that the implementation must respect:

- Effects never influence matching â€” only `Colour` does.
- Every tile in a match triggers its effect.
- Order must be simple and deterministic.
- Aim for ~12â€“15 effects, identified visually by shape.

Confirm trigger rules for new kinds of effect (damage, block, passives) with the user before building them.
