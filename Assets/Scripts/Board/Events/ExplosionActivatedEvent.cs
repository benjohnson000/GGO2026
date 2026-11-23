using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// An <see cref="ExplosionEffect"/> destroyed the square of cells within <see cref="Radius"/> of
    /// <see cref="TileEffectActivatedEvent.Cell"/>. <see cref="TileEffectActivatedEvent.Targets"/> run from the centre outward.
    /// </summary>
    public class ExplosionActivatedEvent : TileEffectActivatedEvent
    {
        public int Radius { get; }

        public ExplosionActivatedEvent(TileEffect effect, Tile tile, Vector2Int cell, IReadOnlyList<Vector2Int> targets, int radius)
            : base(effect, tile, cell, targets)
        {
            Radius = radius;
        }
    }
}
