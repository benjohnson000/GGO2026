using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>A <see cref="LineClearEffect"/> fired along <see cref="Axis"/>.</summary>
    public class LineClearActivatedEvent : TileEffectActivatedEvent
    {
        public LineAxis Axis { get; }

        public LineClearActivatedEvent(TileEffect effect, Tile tile, Vector2Int cell, IReadOnlyList<Vector2Int> targets, LineAxis axis)
            : base(effect, tile, cell, targets)
        {
            Axis = axis;
        }
    }
}
