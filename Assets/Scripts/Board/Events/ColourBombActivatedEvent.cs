using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>A <see cref="ColourBombEffect"/> fired on <see cref="Colour"/>.</summary>
    public class ColourBombActivatedEvent : TileEffectActivatedEvent
    {
        /// <summary>The colour destroyed. None when two bombs were swapped (whole board) or no colour was left.</summary>
        public TileColour Colour { get; }

        public ColourBombActivatedEvent(TileEffect effect, Tile tile, Vector2Int cell, IReadOnlyList<Vector2Int> targets, TileColour colour)
            : base(effect, tile, cell, targets)
        {
            Colour = colour;
        }
    }
}
