using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Which combo tile a match shape makes, one tile definition per colour. What the combo does is the tile's own
    /// <see cref="TileEffect"/>, not part of the rule.
    /// </summary>
    [Serializable]
    public class ComboRule
    {
        [Tooltip("Match shape that makes this combo.")]
        [SerializeField] private MatchShape shape = MatchShape.Line4;

        [Tooltip("One tile per colour; the combo uses the tile matching the match's colour. A colourless (None) tile, such as " +
                 "a colour bomb, is used for any colour without its own tile. Colours with neither make no combo.")]
        [SerializeField] private List<TileDataSO> tiles = new List<TileDataSO>();

        public MatchShape Shape => shape;

        /// <summary>The first tile of <paramref name="colour"/>, else the first colourless tile, else null.</summary>
        public TileDataSO GetTile(TileColour colour)
        {
            TileDataSO colourless = null;
            foreach (TileDataSO data in tiles)
            {
                if (data == null) continue;
                if (data.Colour == colour) return data;
                if (colourless == null && data.Colour == TileColour.None) colourless = data;
            }
            return colourless;
        }
    }
}
