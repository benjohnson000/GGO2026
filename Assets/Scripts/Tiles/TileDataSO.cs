using UnityEngine;
using UnityEngine.Serialization;

namespace ggo2026
{
    /// <summary>
    /// Definition of a deck tile: what it matches as and how it looks. Effects will be added here later.
    /// Visuals are composed at runtime (outlined shape, pattern masked to it, icon on top, each tinted from the palette), so no sprite is baked per tile.
    /// </summary>
    [CreateAssetMenu(menuName = "GGO2026/Tile", fileName = "new Tile")]
    public class TileDataSO : ScriptableObject
    {
        [Tooltip("Name shown to the player. Falls back to the asset name when empty.")]
        [SerializeField] private string displayName;

        [Tooltip("Colour this tile matches as.")]
        [FormerlySerializedAs("colours")]
        [SerializeField] private TileColour colour = TileColour.Red;

        [Tooltip("Optional hand-made sprite. When set, it is drawn untinted (with the outline) instead of the shape, pattern and icon. " +
                 "Use 32x32 at 16 PPU, centred pivot, Mesh Type: Full Rect and a transparent margin for the outline.")]
        [SerializeField] private Sprite customSprite;

        [Tooltip("Base shape sprite (a slice of tile_shapes). Outlined by the board view.")]
        [SerializeField] private Sprite shape;

        [Tooltip("Where the shape's tint comes from.")]
        [SerializeField] private TileColourSource shapeColorSource = TileColourSource.Secondary;

        [Tooltip("Shape tint when the source is Override.")]
        [SerializeField] private Color shapeColor = Color.white;

        [Tooltip("Optional pattern (a slice of tile_patterns), masked to the shape and drawn under the icon.")]
        [SerializeField] private Sprite pattern;

        [Tooltip("Where the pattern's tint comes from.")]
        [SerializeField] private TileColourSource patternColorSource = TileColourSource.Primary;

        [Tooltip("Pattern tint when the source is Override.")]
        [SerializeField] private Color patternColor = Color.white;

        [Tooltip("Optional icon drawn in front of the shape (a slice of tile_icons).")]
        [SerializeField] private Sprite icon;

        [Tooltip("Where the icon's tint comes from.")]
        [SerializeField] private TileColourSource iconColorSource = TileColourSource.Tertiary;

        [Tooltip("Icon tint when the source is Override.")]
        [SerializeField] private Color iconColor = Color.white;

        // Effect: not designed yet. See the Open Questions in Docs/DESIGN.md.

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public TileColour Colour => colour;
        public Sprite CustomSprite => customSprite;
        public bool HasCustomSprite => customSprite != null;
        public Sprite Shape => shape;
        public Sprite Pattern => pattern;
        public Sprite Icon => icon;

        public Color GetShapeColor(PaletteSO palette) => ResolveColor(shapeColorSource, shapeColor, colour, palette);
        public Color GetPatternColor(PaletteSO palette) => ResolveColor(patternColorSource, patternColor, colour, palette);
        public Color GetIconColor(PaletteSO palette) => ResolveColor(iconColorSource, iconColor, colour, palette);

        /// <summary>
        /// Tint for a layer: the override colour, or the chosen palette colour for <paramref name="tileColour"/>
        /// (white without a palette). Static so the editor preview can resolve unsaved inspector values the same way.
        /// </summary>
        public static Color ResolveColor(TileColourSource source, Color overrideColor, TileColour tileColour, PaletteSO palette)
        {
            if (source == TileColourSource.Override) return overrideColor;
            if (palette == null) return Color.white;

            switch (source)
            {
                case TileColourSource.Secondary: return palette.GetSecondary(tileColour);
                case TileColourSource.Tertiary: return palette.GetTertiary(tileColour);
                case TileColourSource.Highlight: return palette.HighlightColor;
                default: return palette.GetPrimary(tileColour);
            }
        }
    }
}
