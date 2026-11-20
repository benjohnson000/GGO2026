namespace ggo2026
{
    /// <summary>
    /// One physical tile. It lives either in the <see cref="TilePool"/> or on the <see cref="Board"/>.
    /// Compared by reference, so two copies of the same <see cref="TileDataSO"/> are different tiles.
    /// </summary>
    public class Tile
    {
        /// <summary>Deck definition, or null for a filler.</summary>
        public TileDataSO Data { get; }

        /// <summary>Colours this tile matches as.</summary>
        public TileColour Colours { get; }

        /// <summary>Fillers stand in when the pool is empty. They are not part of the deck.</summary>
        public bool IsFiller => Data == null;

        public Tile(TileDataSO data)
        {
            Data = data;
            Colours = data != null ? data.Colours : TileColour.None;
        }

        private Tile(TileColour colour)
        {
            Colours = colour;
        }

        /// <summary>Creates a filler tile of the given colour with no effect.</summary>
        public static Tile CreateFiller(TileColour colour)
        {
            return new Tile(TileColourUtils.Sanitize(colour));
        }
    }
}
