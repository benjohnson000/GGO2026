using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>All tile colour data: three colours per <see cref="TileColour"/> plus the outline and highlight shared by every tile.</summary>
    [CreateAssetMenu(menuName = "GGO2026/Palette", fileName = "new Palette")]
    public class PaletteSO : ScriptableObject
    {
        [Tooltip("Three colours per tile colour. Tiles choose which one each layer uses (by default shape = 2, pattern = 1, icon = 3).")]
        [SerializeField] private List<ColourSwatch> swatches = new List<ColourSwatch>();

        [Header("Outline")]
        [Tooltip("Outline drawn around every tile shape.")]
        [SerializeField] private Color outlineColor = new Color(0.18f, 0.133f, 0.184f);

        [Tooltip("Outline thickness in sprite pixels. The shader supports up to 3.")]
        [Range(0, 3)]
        [SerializeField] private int outlineThickness = 2;

        [Header("Highlight")]
        [Tooltip("Shared highlight colour, the same for every tile colour. Any layer can use it via its Color dropdown.")]
        [SerializeField] private Color highlightColor = Color.white;

        public Color OutlineColor => outlineColor;
        public int OutlineThickness => outlineThickness;
        public Color HighlightColor => highlightColor;

        /// <summary>Colour 1 for a tile colour, or white if the palette has no entry.</summary>
        public Color GetPrimary(TileColour colour)
        {
            ColourSwatch swatch = Find(colour);
            return swatch != null ? swatch.Primary : Color.white;
        }

        /// <summary>Colour 2 for a tile colour, or white if the palette has no entry.</summary>
        public Color GetSecondary(TileColour colour)
        {
            ColourSwatch swatch = Find(colour);
            return swatch != null ? swatch.Secondary : Color.white;
        }

        /// <summary>Colour 3 for a tile colour, or white if the palette has no entry.</summary>
        public Color GetTertiary(TileColour colour)
        {
            ColourSwatch swatch = Find(colour);
            return swatch != null ? swatch.Tertiary : Color.white;
        }

        private ColourSwatch Find(TileColour colour)
        {
            foreach (ColourSwatch swatch in swatches)
            {
                if (swatch != null && swatch.Colour == colour) return swatch;
            }
            return null;
        }
    }
}
