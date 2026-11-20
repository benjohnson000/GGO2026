using UnityEngine;

namespace ggo2026
{
    /// <summary>A tile left the board and went back to the pool (fillers are discarded instead).</summary>
    public class TileDestroyedEvent : BoardEvent
    {
        public Tile Tile { get; }
        public Vector2Int Cell { get; }

        public TileDestroyedEvent(Tile tile, Vector2Int cell)
        {
            Tile = tile;
            Cell = cell;
        }
    }
}
