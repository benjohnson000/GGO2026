using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Connected set of matched tiles. Runs that share a cell (L and T shapes) are merged into one group,
    /// including runs of different colours that cross on a multi-colour tile.
    /// </summary>
    public class MatchGroup
    {
        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private readonly List<Tile> tiles = new List<Tile>();

        /// <summary>Every colour a run in this group matched on.</summary>
        public TileColour Colours { get; private set; }

        /// <summary>Matched cells, listed bottom row first, left to right. Parallel to <see cref="Tiles"/>.</summary>
        public IReadOnlyList<Vector2Int> Cells => cells;

        public IReadOnlyList<Tile> Tiles => tiles;

        internal void AddColour(TileColour colour)
        {
            Colours |= colour;
        }

        internal void AddCell(Vector2Int cell, Tile tile)
        {
            cells.Add(cell);
            tiles.Add(tile);
        }
    }
}
