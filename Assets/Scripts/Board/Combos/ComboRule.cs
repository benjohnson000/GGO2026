using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Which combo tile a match shape makes, one tile definition per colour.</summary>
    [Serializable]
    public class ComboRule
    {
        [Tooltip("Match shape that makes this combo.")]
        [SerializeField] private MatchShape shape = MatchShape.Line4;

        [Tooltip("What the combo tile does when it is destroyed.")]
        [SerializeField] private ComboEffect effect = ComboEffect.ClearRow;

        [Tooltip("One tile per colour. The combo takes the colour of the match that made it; colours with no tile make no combo.")]
        [SerializeField] private List<TileDataSO> tiles = new List<TileDataSO>();

        public MatchShape Shape => shape;
        public ComboEffect Effect => effect;

        /// <summary>The first tile of <paramref name="colour"/>, or null if this rule has none.</summary>
        public TileDataSO GetTile(TileColour colour)
        {
            foreach (TileDataSO data in tiles)
            {
                if (data != null && data.Colour == colour) return data;
            }
            return null;
        }
    }
}
