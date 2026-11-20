using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Display colour for one <see cref="TileColour"/>.</summary>
    [Serializable]
    public class ColourSwatch
    {
        [SerializeField] private TileColour colour;
        [SerializeField] private Color color = Color.white;

        public TileColour Colour => colour;
        public Color Color => color;
    }
}
