using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>A number of copies of one tile in a deck asset.</summary>
    [Serializable]
    public class DeckEntry
    {
        [SerializeField] private TileDataSO tile;
        [Min(0)]
        [SerializeField] private int count = 1;

        public TileDataSO TileData => tile;
        public int Count => count;
    }
}
