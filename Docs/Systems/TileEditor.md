# Tile Editor (editor tooling)

`Assets/Scripts/Editor/` â€” editor-only code (compiled into `ggo2026.Editor`) for authoring `TileDataSO`s.

| Type | Role |
|---|---|
| `TileDataSOEditor` | Custom inspector for every `TileDataSO` |
| `TileEditorWindow` | **GGO2026 â€º Tile Editor** window for making several tiles in a row |
| `TileEditorGUI` | Shared IMGUI helpers: sheet loading, sprite grids, preview, outline |
| `TileNameSync` | Asset naming (`<Colour>-<Name>`) helpers, plus an `AssetPostprocessor` that fills empty display names |

## Tile inspector

Selecting any tile asset shows:

1. **Preview** â€” the tile as the board draws it: outlined shape, pattern masked to the shape, icon, each
   in the colour its **Color** dropdown selects (defaults: shape colour 2, pattern colour 1, icon colour 3). The layers are composited per texel on the CPU (same outline and
   mask rules as the shader) into one small texture, recomposed only when an input changes, then drawn at
   the largest whole-pixel scale that fits (crisp at any display scaling). **Randomize** picks a random
   colour, shape, pattern (or none) and icon.
2. **Name** (with **Rename Asset**), **Colour**, and the **Effects** list. Use **+** / **-** to add and remove
   entries and drag to reorder; each entry's type picker (Animancer's polymorphic drawer) chooses the effect,
   and its tuning fields appear under it. See [Board › Tile effects](Board.md#tile-effects).
3. **Shape** picker â€” click a cell in the grid of slices from `tile_shapes.png`.
4. **Pattern** picker â€” slices from `tile_patterns.png` followed by every slice from `tile_shapes.png`
   (a shape works as a pattern too, e.g. a smaller inner shape), with a **None** cell. **Randomize** picks
   from the same list.
5. **Icon** picker â€” same for `tile_icons.png`, with a **None** cell.

   A **Custom Sprite** field sits above these. While it is set, the preview shows that sprite untinted
   with the outline, and the Shape/Pattern/Icon sections and **Randomize** are disabled (their values are
   kept). The Tile Editor window accepts either a shape or a custom sprite.

   Each of the three has a **Color** dropdown (Primary / Secondary / Tertiary / Override / Highlight) with a colour
   swatch that is editable only for Override. The chosen colour tints the picker thumbnails and preview.
   **Randomize** leaves the colour sources alone.
6. **Preview Palette** â€” which `PaletteSO` colours the preview (editor-only, not saved on the tile).
   Defaults to the first palette found in the project. **Refresh** reloads sheets and palette.

### Asset naming

- Tile assets are named `<Colour>-<Name>`, e.g. `Red-Bomb` (`TileNameSync.GetAssetName`; invalid file
  characters become `_`, an empty name becomes "New Tile").
- Changing the **Name** (applied on Enter or when the field loses focus) or the **Colour**, or pressing
  **Randomize**, renames the saved asset. The rename runs after the inspector frame (`delayCall`).
- If the target name is taken, the rename is skipped with a warning. **Rename Asset** is enabled whenever
  the asset name doesn't match, e.g. after an undo, a failed rename or for tiles made before this rule.
- The name field is the source of truth: renaming the file by hand does not change the name, and the next
  name or colour edit renames it back.
- An empty name is filled from the asset name, minus any `<Colour>-` prefix, when the asset is imported or
  inspected. At runtime `TileDataSO.DisplayName` still falls back to the asset name if the field is empty.

## Tile Editor window

**GGO2026 â€º Tile Editor**. Edits an in-memory draft with the same inspector, then **Create Tile Asset**
saves a copy. The draft is kept, so you can tweak one field and create the next variant.

- **Save Folder** defaults to `Assets/Tiles/Data` and must be inside `Assets`. Missing folders are created.
- File name is `<Colour>-<Name>` (see [Asset naming](#asset-naming)); duplicates get a unique suffix.
- A shape is required. **Reset** starts a fresh draft.

## Sprite sheets

Paths are constants in `TileEditorGUI`:

```csharp
public const string ShapeSheetPath   = "Assets/Art/tile_shapes.png";
public const string PatternSheetPath = "Assets/Art/tile_patterns.png";
public const string IconSheetPath    = "Assets/Art/tile_icons.png";
```

Update them if the sheets move. Requirements:

- Sheets must be **sliced** (Sprite Mode: Multiple). Fully transparent slices are hidden from the picker
  (pixels are read from a GPU copy, so the textures don't need Read/Write enabled).
- Slices are listed top row first, left to right, matching the sheet layout.
- Shapes should be **white** so tinting gives the palette colour, and need a transparent margin plus
  Mesh Type: Full Rect for the outline shader (see [Board View â€º Tile Outline shader](BoardView.md#tile-outline-shader)).
- Patterns and icons should be white too. Patterns can fill their whole slice; they are masked to the shape.
- Shape, pattern and icon are centred on each other texel for texel, in the preview and on the board, so
  keep pixels per unit the same across the sheets (16) and pivots centred.
- **Refresh** after editing a sheet so the preview re-reads its pixels.

## Making a new tile

1. Open **GGO2026 â€º Tile Editor**.
2. Enter a name, pick a colour, click a shape (and optionally a pattern and an icon).
3. **Create Tile Asset**.
4. Add it to a `StartingDeckSO` with a count.
