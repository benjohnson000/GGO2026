using UnityEngine;

namespace ggo2026
{
    /// <summary>A tile was drawn from the pool (or created as a filler) into an empty cell.</summary>
    public class TileSpawnedEvent : BoardEvent
    {
        public Tile Tile { get; }
        public Vector2Int Cell { get; }

        /// <summary>Virtual cell above the board that the tile falls in from. Its y is at least the board height.</summary>
        public Vector2Int SpawnCell { get; }

        public TileSpawnedEvent(Tile tile, Vector2Int cell, Vector2Int spawnCell)
        {
            Tile = tile;
            Cell = cell;
            SpawnCell = spawnCell;
        }
    }
}
