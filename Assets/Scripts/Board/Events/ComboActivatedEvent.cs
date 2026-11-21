using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// A destroyed combo tile fired its <see cref="ComboEffect"/>. Comes after the combo's own
    /// <see cref="TileDestroyedEvent"/> and is followed by one for each tile the effect destroys.
    /// </summary>
    public class ComboActivatedEvent : BoardEvent
    {
        public Tile Tile { get; }

        /// <summary>The cell the combo tile was in when it fired.</summary>
        public Vector2Int Cell { get; }

        public ComboActivatedEvent(Tile tile, Vector2Int cell)
        {
            Tile = tile;
            Cell = cell;
        }
    }
}
