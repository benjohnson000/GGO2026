using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

namespace ggo2026
{
    /// <summary>
    /// Definition of a tile: what it matches as, what it does (a list of <see cref="TileEffect"/>s) and how it looks.
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

        [Tooltip("Optional hand-made sprite. When set, it is drawn untinted (with the outline, unless Custom Sprite Outline is off) instead of the shape, pattern and icon. " +
                 "Use 32x32 at 16 PPU, centred pivot, Mesh Type: Full Rect and a transparent margin for the outline.")]
        [SerializeField] private Sprite customSprite;

        [Tooltip("Add the palette outline around the custom sprite. Turn off for sprites that already have an outline drawn in.")]
        [SerializeField] private bool customSpriteOutline = true;

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

        [Tooltip("What the tile does, fired in order when it is destroyed. Stored in this asset, so each tile keeps its own tuning values.")]
        [SerializeReference, Polymorphic] private List<TileEffect> effects = new List<TileEffect>();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public TileColour Colour => colour;

        /// <summary>
        /// The tile's effects in firing order. Shared by every tile made from this asset.
        /// May contain null entries (added in the inspector with no type picked); callers skip them.
        /// </summary>
        public IReadOnlyList<TileEffect> Effects => effects;
        public Sprite CustomSprite => customSprite;
        public bool HasCustomSprite => customSprite != null;

        /// <summary>Whether the board adds the palette outline around <see cref="CustomSprite"/>.</summary>
        public bool CustomSpriteOutline => customSpriteOutline;
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
