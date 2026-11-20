using UnityEngine;

namespace ggo2026
{
    /// <summary>A tile fell to a lower cell after the tiles below it were destroyed.</summary>
    public class TileMovedEvent : BoardEvent
    {
        public Tile Tile { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }

        public TileMovedEvent(Tile tile, Vector2Int from, Vector2Int to)
        {
            Tile = tile;
            From = from;
            To = to;
        }
    }
}
