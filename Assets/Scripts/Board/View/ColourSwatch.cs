using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ggo2026
{
    /// <summary>
    /// Display colours for one <see cref="TileColour"/>. Each tile layer picks one of them via its <see cref="TileColourSource"/>;
    /// by default the shape uses secondary, the pattern primary and the icon tertiary.
    /// </summary>
    [Serializable]
    public class ColourSwatch
    {
        [SerializeField] private TileColour colour;

        [Tooltip("Colour 1. Default for patterns.")]
        [FormerlySerializedAs("color")]
        [SerializeField] private Color primary = Color.white;

        [Tooltip("Colour 2. Default for shapes.")]
        [SerializeField] private Color secondary = Color.white;

        [Tooltip("Colour 3. Default for icons.")]
        [SerializeField] private Color tertiary = Color.white;

        public TileColour Colour => colour;
        public Color Primary => primary;
        public Color Secondary => secondary;
        public Color Tertiary => tertiary;
    }
}
