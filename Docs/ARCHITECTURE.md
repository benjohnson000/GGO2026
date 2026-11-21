# Architecture

How the project is put together: scenes, code layout, and how data flows from assets to the screen.
For what the game *is*, see [DESIGN.md](DESIGN.md). For coding conventions, see
[SCRIPTING_GUIDELINES.md](../Assets/Scripts/SCRIPTING_GUIDELINES.md).

## Code layout

| Folder | Namespace | Contents | Doc |
|---|---|---|---|
| `Assets/Scripts/Board/` | `ggo2026` | Grid rules: `Board`, `MatchFinder`, `MatchGroup`, `BoardConfigSO` | [Board](Systems/Board.md) |
| `Assets/Scripts/Board/Events/` | `ggo2026` | `BoardEvent` subclasses the board emits | [Board](Systems/Board.md#board-events) |
| `Assets/Scripts/Board/View/` | `ggo2026` | `BoardView`, `TileView`, view states, palette and view settings | [Board View](Systems/BoardView.md) |
| `Assets/Scripts/Tiles/` | `ggo2026` | `Tile`, `TilePool`, `TileDataSO`, `StartingDeckSO`, `TileColour` | [Tiles and Deck](Systems/TilesAndDeck.md) |
| `Assets/Scripts/Combat/` | `ggo2026` | `Combat`, `Combatant`, actions, events, listeners, statuses, `CombatantView`, `EncounterView` | [Combat](Systems/Combat.md) |
| `Assets/Scripts/Editor/` | `ggo2026` | Tile inspector and Tile Editor window (editor-only assembly) | [Tile Editor](Systems/TileEditor.md) |
| `Assets/Scripts/Utilities/` | `benjohnson` | Game-agnostic infrastructure | [Utilities](Systems/Utilities.md) |
| `Assets/Art/Shaders/` | â€“ | `GGO2026/Tile Outline` sprite shader | [Board View](Systems/BoardView.md#tile-outline-shader) |

`Utilities/` must never reference `ggo2026` code. Game code may use `benjohnson` freely.

## Logic / view split

The core rule of the codebase: **rules are plain C#, presentation is MonoBehaviour.**

```
 ScriptableObjects (data)           Plain C# (rules)              MonoBehaviours (view)
 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€            â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€              â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
 BoardConfigSO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
 StartingDeckSO â”€â–º TilePool â”€â–º Board â”€â”€ List<BoardEvent> â”€â”€â–º BoardView â”€â–º TileView
 TileDataSO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜       â–²                              â”‚
 BoardViewSettingsSO, PaletteSO â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                                 â””â”€â”€â”€â”€â”€â”€â”€â”€ TrySwap(a, b) â—„â”€â”€â”€â”€ InputManager (pointer drag)
```

- `Board` resolves every operation **instantly** and returns an ordered list of `BoardEvent`s describing
  what happened (swap, match, destroy, fall, spawn, shuffle).
- `BoardView` plays those events back as animations and ignores input while doing so.
- Because the board never waits on the view, game logic (future tile effects, charms, enemy intents)
  should hook into the **event list**, not into animations. This is the "clean events" hook that
  [DESIGN.md â€º Charms â€º Implementation notes](DESIGN.md#implementation-notes) asks for.

## Scenes and boot flow

All gameplay runs in additively loaded scenes. Build order (`ProjectSettings/EditorBuildSettings.asset`):

| # | Scene | Role | Key objects |
|---|---|---|---|
| 0 | `PersistentScene` | Always loaded. Boots everything else. | `SceneController`, `TransitionController` (canvas) |
| 1 | `Camera` | Utility scene | `Main Camera` |
| 2 | `GameUtilities` | Utility scene | `InputManager`, `EventSystem` |
| 3 | `Game` | Environment scene (swapped by transitions) | `Board` (`BoardView`), world-space `Canvas` |
| 4 | `UI` | Utility scene (not loaded at startup yet) | `Canvas` |

Startup sequence:

1. `PersistentScene` loads. `SceneController.Start` additively loads each **utility scene** in its
   `startupUtilityScenes` list (`Camera`, `GameUtilities`).
2. It then calls `LoadEnvironment("Game")`, which runs the default transition: curtains close â†’
   previous environment unloads â†’ new one loads â†’ curtains open.
3. In `Game`, `BoardView.Start` builds the pool and board and plays the initial fill.

Notes:

- **Press Play from `PersistentScene`**, or put the `PersistenceCheck` prefab
  (`Assets/Scripts/Utilities/PersistenceCheck.prefab`) in the scene you are editing. It reloads build
  index 0 when no `SceneController` exists, so Play works from any scene.
- Singletons in other scenes may wake after your object. Code that needs one should retry in `Start`
  (see `BoardView.TrySubscribe`) and cameras should be looked up lazily (see `CanvasCameraAssigner`).
- The `UI` scene is in the build list but not in `startupUtilityScenes`, so it is never loaded at runtime
  today. Add it to the list on the `SceneController` in `PersistentScene` when UI work starts.

## Assets

| Asset | Type | Used by |
|---|---|---|
| `Assets/Board Config.asset` | `BoardConfigSO` | `BoardView` in `Game` â€” grid size, filler colours |
| `Assets/Board View Settings.asset` | `BoardViewSettingsSO` | `BoardView` â€” layout, timings, drag threshold, palette, material |
| `Assets/Art/Palette.asset` | `PaletteSO` | Board View Settings, tile inspector preview |
| `Assets/Tiles/Data/*.asset` | `TileDataSO` | Starting decks |
| `Assets/Art/tile_shapes.png`, `tile_patterns.png`, `tile_icons.png` | Sliced sprite sheets | Tile shapes, patterns and icons; read by the Tile Editor pickers |
| `Assets/Art/Shaders/TileOutline.shader` | Shader | Tile material |

Create new data assets from **Assets â€º Create â€º GGO2026 â€º â€¦** (Board Config, Board View Settings,
Palette, Tile, Starting Deck).

## What exists vs. what is planned

Built:

- Grid rules: drag-to-swap, invalid-swap bounce, matches incl. 2x2 squares and L/T merging, gravity, refill, cascades,
  no-match initial fill, reshuffle when no moves remain.
- Deck-as-pool with filler tiles when the pool is empty.
- Placeholder visuals: outlined shape, pattern masked to the shape, and icon, each tinted from the palette by one shader.
- Tile authoring tools in the editor.
- Combat rules: health, block, damage pipeline, action queue, listener hooks, statuses (Poison), with
  placeholder views and debug actions. See [Combat](Systems/Combat.md).
- Infrastructure: scene loading, transitions, input, audio managers, save/load.

Not built yet (see [DESIGN.md â€º Open Questions](DESIGN.md#open-questions) before starting any of these):

- Tile **effects** (`TileDataSO` has a placeholder comment only).
- Encounter flow (turns), enemy intents, enemy tiles. Combat is not wired to the board yet.
- Charms.
- Run state (a run's deck currently comes straight from a `StartingDeckSO`), rewards, map.
- Audio is coded but `SFXManager` / `MusicManager` are not placed in any scene.
- Save data structures (the `SaveLoadSystem` is ready but unused).
