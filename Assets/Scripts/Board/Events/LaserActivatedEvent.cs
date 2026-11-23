using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>A <see cref="LaserEffect"/> fired from <see cref="TileEffectActivatedEvent.Cell"/> at each of its <see cref="TileEffectActivatedEvent.Targets"/>, nearest first.</summary>
    public class LaserActivatedEvent : TileEffectActivatedEvent
    {
        public LaserActivatedEvent(TileEffect effect, Tile tile, Vector2Int cell, IReadOnlyList<Vector2Int> targets)
            : base(effect, tile, cell, targets)
        {
        }
    }
}
