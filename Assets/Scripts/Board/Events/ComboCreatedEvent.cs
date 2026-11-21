using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// A match made a combo tile, placed in one of the group's now-empty cells. Comes after the match's
    /// <see cref="TileDestroyedEvent"/>s and before gravity. Combo tiles are temporary and never join the pool.
    /// </summary>
    public class ComboCreatedEvent : BoardEvent
    {
        public Tile Tile { get; }
        public Vector2Int Cell { get; }

        /// <summary>The match that made the combo.</summary>
        public MatchGroup Group { get; }

        public ComboCreatedEvent(Tile tile, Vector2Int cell, MatchGroup group)
        {
            Tile = tile;
            Cell = cell;
            Group = group;
        }
    }
}
