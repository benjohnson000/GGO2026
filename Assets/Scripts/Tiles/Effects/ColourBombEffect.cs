using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Colour bomb: destroys every tile of one colour. Meant for a colourless tile (<see cref="TileColour.None"/>),
    /// which never matches and is fired by swapping it with any neighbour.
    /// The colour is the swapped tile's; when not fired by a swap (e.g. hit by a line clear), the most common colour
    /// on the board. Swapped with another colour bomb, it destroys the whole board.
    /// </summary>
    [Serializable]
    public class ColourBombEffect : TileEffect
    {
        public override bool ActivatesOnSwap => true;

        public override TileEffectActivatedEvent Activate(TileEffectContext context)
        {
            Board board = context.Board;
            List<Vector2Int> targets = new List<Vector2Int>();

            if (context.SwappedWith != null && context.SwappedWith.HasEffect<ColourBombEffect>())
            {
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (board.GetTile(cell) != null) targets.Add(cell);
                    }
                }
                return new ColourBombActivatedEvent(this, context.Tile, context.Cell, targets, TileColour.None);
            }

            TileColour colour = context.SwappedWith != null && TileColourUtils.IsMatchable(context.SwappedWith.Colour)
                ? context.SwappedWith.Colour
                : MostCommonColour(board);

            if (colour != TileColour.None)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        Tile tile = board.GetTile(cell);
                        if (tile != null && tile.Colour == colour) targets.Add(cell);
                    }
                }
            }

            return new ColourBombActivatedEvent(this, context.Tile, context.Cell, targets, colour);
        }

        /// <summary>The matchable colour with the most tiles on the board; ties go to the earlier colour in <see cref="TileColourUtils.All"/>. None if there are none.</summary>
        private static TileColour MostCommonColour(Board board)
        {
            TileColour best = TileColour.None;
            int bestCount = 0;

            foreach (TileColour colour in TileColourUtils.All)
            {
                int count = 0;
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        Tile tile = board.GetTile(new Vector2Int(x, y));
                        if (tile != null && tile.Colour == colour) count++;
                    }
                }

                if (count > bestCount)
                {
                    best = colour;
                    bestCount = count;
                }
            }

            return best;
        }
    }
}
