using UnityEngine;

namespace ggo2026
{
    /// <summary>The player swapped two tiles. A rejected swap made no match and was undone without costing a turn.</summary>
    public class SwapEvent : BoardEvent
    {
        /// <summary>Tile that started at <see cref="CellA"/>.</summary>
        public Tile TileA { get; }
        /// <summary>Tile that started at <see cref="CellB"/>.</summary>
        public Tile TileB { get; }
        public Vector2Int CellA { get; }
        public Vector2Int CellB { get; }
        public bool Accepted { get; }

        public SwapEvent(Tile tileA, Tile tileB, Vector2Int cellA, Vector2Int cellB, bool accepted)
        {
            TileA = tileA;
            TileB = tileB;
            CellA = cellA;
            CellB = cellB;
            Accepted = accepted;
        }
    }
}
