using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Bomb tile: destroys every tile in the square of cells within <see cref="Radius"/> of it (radius 1 is the 3x3
    /// around it, 2 is 5x5…), which fires their effects. Tiles are destroyed from the bomb outward, one ring at a time.
    /// By default it also fires when swapped with any neighbour, exploding from the cell it was swapped into.
    /// </summary>
    [Serializable]
    public class ExplosionEffect : TileEffect
    {
        [Tooltip("How many cells the square reaches out from the tile on each side. 1 = 3x3, 2 = 5x5.")]
        [Min(1)]
        [SerializeField] private int radius = 1;

        [Tooltip("On: swapping the tile with any neighbour fires it, even with no match. It explodes from the cell it was swapped into.")]
        [SerializeField] private bool activatesOnSwap = true;

        public int Radius => radius;

        // Queues no combat actions and sends no projectiles.
        public override bool UsesProjectile => false;

        public override bool ActivatesOnSwap => activatesOnSwap;

        public override TileEffectActivatedEvent Activate(TileEffectContext context)
        {
            Board board = context.Board;
            Vector2Int origin = context.Cell;
            List<Vector2Int> targets = new List<Vector2Int>();

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x == 0 && y == 0) continue;
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    if (board.IsInBounds(cell)) targets.Add(cell);
                }
            }

            // Ring by ring (square distance), then edges before corners within a ring. The list is built in a fixed
            // order and List.Sort is unstable, so break the remaining ties by position to keep it deterministic.
            targets.Sort((a, b) =>
            {
                Vector2Int da = a - origin;
                Vector2Int db = b - origin;
                int compare = Ring(da).CompareTo(Ring(db));
                if (compare != 0) return compare;
                compare = da.sqrMagnitude.CompareTo(db.sqrMagnitude);
                if (compare != 0) return compare;
                compare = a.x.CompareTo(b.x);
                return compare != 0 ? compare : a.y.CompareTo(b.y);
            });

            return new ExplosionActivatedEvent(this, context.Tile, origin, targets, radius);
        }

        private static int Ring(Vector2Int offset)
        {
            return Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
        }
    }
}
