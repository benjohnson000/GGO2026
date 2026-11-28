# GGO2026

A match-3 roguelike deckbuilder for **GitHub Game Off 2026** (theme: *Shift*). Swap tiles to make
matches; between encounters, build the deck of tiles that fills the grid.

- Engine: Unity 6 (`6000.6.8f1`), URP 2D, Input System, TextMesh Pro.
- Status: core match-3 board playable with placeholder visuals. Effects, encounters and charms are
  designed but not built yet.

## Getting started

1. Open the project in Unity `6000.6.8f1`.
2. Open `Assets/Scenes/PersistentScene.unity` and press **Play**. It loads the camera, utilities and
   the `Game` scene, which shows a 6×6 board.
3. Drag a tile toward a neighbour to swap.

## Documentation

| Doc | What's in it |
|---|---|
| [Docs/DESIGN.md](Docs/DESIGN.md) | Game design: settled decisions and open questions |
| [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) | Scenes, boot flow, code layout, logic/view split, what's built |
| [Docs/Systems/Board.md](Docs/Systems/Board.md) | Grid rules: swaps, matching, cascades, reshuffle, board events |
| [Docs/Systems/BoardView.md](Docs/Systems/BoardView.md) | Input, event playback, tile visuals, outline shader, view settings |
| [Docs/Systems/TilesAndDeck.md](Docs/Systems/TilesAndDeck.md) | Tile data, the deck-as-pool, fillers, colours |
| [Docs/Systems/Combat.md](Docs/Systems/Combat.md) | Health, block, statuses, damage pipeline, action queue, listeners |
| [Docs/Systems/Encounter.md](Docs/Systems/Encounter.md) | Turn flow, enemy intents and countdowns, victory/defeat |
| [Docs/Systems/Coins.md](Docs/Systems/Coins.md) | Coin purse, coin tiles, the top bar counter and flying coins |
| [Docs/Systems/Rewards.md](Docs/Systems/Rewards.md) | The reward screen after a win: reward sources on enemies, coin and tile-choice rewards |
| [Docs/Systems/AmbientBirds.md](Docs/Systems/AmbientBirds.md) | Birds that fly in, perch on scene points and fly off |
| [Docs/Systems/TileEditor.md](Docs/Systems/TileEditor.md) | Tile inspector and Tile Editor window |
| [Docs/Systems/Utilities.md](Docs/Systems/Utilities.md) | Singleton, state machine, scenes, transitions, input, audio, save/load |
| [Assets/Scripts/SCRIPTING_GUIDELINES.md](Assets/Scripts/SCRIPTING_GUIDELINES.md) | C# conventions and pre-commit checklist |
| [AGENTS.md](AGENTS.md) | Rules for AI coding agents |
