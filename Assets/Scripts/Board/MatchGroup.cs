using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Connected set of matched tiles of one colour. Runs and 2x2 squares that share a cell (L, T and block shapes) are merged into one group.
    /// </summary>
    public class MatchGroup
    {
        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private readonly List<Tile> tiles = new List<Tile>();

        public TileColour Colour { get; }

        /// <summary>Matched cells, listed bottom row first, left to right. Parallel to <see cref="Tiles"/>.</summary>
        public IReadOnlyList<Vector2Int> Cells => cells;

        public IReadOnlyList<Tile> Tiles => tiles;

        internal MatchGroup(TileColour colour)
        {
            Colour = colour;
        }

        internal void AddCell(Vector2Int cell, Tile tile)
        {
            cells.Add(cell);
            tiles.Add(tile);
        }
    }
}
