using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Fires a laser at random tiles still on the board and destroys them, which fires their effects.
    /// Each target is a different tile, and tiles with effects are picked first by default. Targets are listed nearest first, which is the order the board destroys them
    /// and fires their effects.
    /// </summary>
    [Serializable]
    public class LaserEffect : TileEffect
    {
        [Tooltip("How many lasers fire, each at a different random tile. Fewer fire if the board has fewer tiles left.")]
        [Min(1)]
        [SerializeField] private int count = 1;

        [Tooltip("On: aims at tiles that have effects first, so lasers set off chains; plain tiles are only hit once none are left. Off: any tile.")]
        [SerializeField] private bool preferTilesWithEffects = true;

        public override TileEffectActivatedEvent Activate(TileEffectContext context)
        {
            Board board = context.Board;
            List<Vector2Int> withEffects = new List<Vector2Int>();
            List<Vector2Int> others = new List<Vector2Int>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    // The firing tile has already left its cell, so it is never a candidate.
                    Tile tile = board.GetTile(cell);
                    if (tile == null) continue;

                    if (preferTilesWithEffects && tile.HasEffects) withEffects.Add(cell);
                    else others.Add(cell);
                }
            }

            List<Vector2Int> targets = new List<Vector2Int>();
            TakeRandom(withEffects, targets, count, context.Random);
            TakeRandom(others, targets, count - targets.Count, context.Random);
            if (targets.Count == 0) return null;

            Vector2Int origin = context.Cell;
            targets.Sort((a, b) => (a - origin).sqrMagnitude.CompareTo((b - origin).sqrMagnitude));

            return new LaserActivatedEvent(this, context.Tile, origin, targets);
        }

        /// <summary>Moves up to <paramref name="amount"/> random cells from <paramref name="candidates"/> into <paramref name="targets"/>.</summary>
        private static void TakeRandom(List<Vector2Int> candidates, List<Vector2Int> targets, int amount, System.Random random)
        {
            for (int i = 0; i < amount && candidates.Count > 0; i++)
            {
                int pick = random.Next(candidates.Count);
                targets.Add(candidates[pick]);
                candidates[pick] = candidates[candidates.Count - 1];
                candidates.RemoveAt(candidates.Count - 1);
            }
        }
    }
}
