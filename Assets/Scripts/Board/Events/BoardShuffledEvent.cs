using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>No valid move was left, so the tiles already on the board were rearranged. Nothing is drawn.</summary>
    public class BoardShuffledEvent : BoardEvent
    {
        /// <summary>Tiles that changed cell. Tiles that stayed put are not listed.</summary>
        public IReadOnlyList<TileMove> Moves { get; }

        public BoardShuffledEvent(IReadOnlyList<TileMove> moves)
        {
            Moves = moves;
        }
    }
}
