using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Striped tile: destroys its whole row or column. The line is picked when it fires: perpendicular to a line clear
    /// that hit it (so chains reach new tiles), else from the swap that matched it, else at random.
    /// If that line has no tiles left, it takes the other one. Tiles are destroyed from the striped tile outward.
    /// </summary>
    [Serializable]
    public class LineClearEffect : TileEffect
    {
        [Tooltip("On: a sideways swap clears the column and an up/down swap clears the row, as in Candy Crush. Off: clears along the swap.")]
        [SerializeField] private bool clearAcrossSwap = true;

        public override TileEffectActivatedEvent Activate(TileEffectContext context)
        {
            Board board = context.Board;
            Vector2Int cell = context.Cell;
            LineAxis axis = PickAxis(context);
            List<Vector2Int> targets = GetLine(board, cell, axis);

            // The match, or a striped tile that fired earlier this step, may have emptied the line already.
            if (!HasTile(board, targets))
            {
                axis = Perpendicular(axis);
                targets = GetLine(board, cell, axis);
            }

            return new LineClearActivatedEvent(this, context.Tile, cell, targets, axis);
        }

        /// <summary>
        /// Every other cell of the row or column through <paramref name="cell"/>, nearest first. At equal distance
        /// left comes before right and below before above. The board destroys them, and so fires their effects, in this order.
        /// </summary>
        private static List<Vector2Int> GetLine(Board board, Vector2Int cell, LineAxis axis)
        {
            List<Vector2Int> line = new List<Vector2Int>();
            Vector2Int step = axis == LineAxis.Row ? Vector2Int.right : Vector2Int.up;
            int length = axis == LineAxis.Row ? board.Width : board.Height;

            for (int distance = 1; distance < length; distance++)
            {
                Vector2Int before = cell - step * distance;
                Vector2Int after = cell + step * distance;
                if (board.IsInBounds(before)) line.Add(before);
                if (board.IsInBounds(after)) line.Add(after);
            }
            return line;
        }

        private static bool HasTile(Board board, List<Vector2Int> line)
        {
            foreach (Vector2Int cell in line)
            {
                if (board.GetTile(cell) != null) return true;
            }
            return false;
        }

        private static LineAxis Perpendicular(LineAxis axis)
        {
            return axis == LineAxis.Row ? LineAxis.Column : LineAxis.Row;
        }

        private LineAxis PickAxis(TileEffectContext context)
        {
            // Clearing along the line that hit it would only find tiles already destroyed.
            if (context.Cause is LineClearActivatedEvent line) return Perpendicular(line.Axis);

            if (context.SwapDirection.HasValue)
            {
                bool sideways = context.SwapDirection.Value.x != 0;
                bool clearRow = sideways != clearAcrossSwap;
                return clearRow ? LineAxis.Row : LineAxis.Column;
            }

            return context.Random.Next(2) == 0 ? LineAxis.Row : LineAxis.Column;
        }
    }
}
