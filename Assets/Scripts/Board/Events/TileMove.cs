using UnityEngine;

namespace ggo2026
{
    /// <summary>One tile's move during a shuffle.</summary>
    public readonly struct TileMove
    {
        public Tile Tile { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }

        public TileMove(Tile tile, Vector2Int from, Vector2Int to)
        {
            Tile = tile;
            From = from;
            To = to;
        }
    }
}
