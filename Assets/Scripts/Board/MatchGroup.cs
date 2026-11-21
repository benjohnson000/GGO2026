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

        /// <summary>The highest-priority shape in the group. Decides which combo tile, if any, it makes.</summary>
        public MatchShape Shape { get; private set; }

        /// <summary>
        /// Where a combo goes when no swapped tile is in the group (cascades): the middle of the longest run for
        /// lines, the shared tile for crosses, the bottom-left of the first square for squares.
        /// </summary>
        public Vector2Int ComboCell { get; private set; }

        internal MatchGroup(TileColour colour)
        {
            Colour = colour;
        }

        public bool Contains(Vector2Int cell)
        {
            return cells.Contains(cell);
        }

        internal void SetShape(MatchShape shape, Vector2Int comboCell)
        {
            Shape = shape;
            ComboCell = comboCell;
        }

        internal void AddCell(Vector2Int cell, Tile tile)
        {
            cells.Add(cell);
            tiles.Add(tile);
        }
    }
}
