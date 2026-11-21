namespace ggo2026
{
    /// <summary>
    /// One physical tile. It lives either in the <see cref="TilePool"/> or on the <see cref="Board"/>.
    /// Compared by reference, so two copies of the same <see cref="TileDataSO"/> are different tiles.
    /// </summary>
    public class Tile
    {
        /// <summary>Tile definition. Null only for a fallback filler created without data.</summary>
        public TileDataSO Data { get; }

        /// <summary>Colour this tile matches as.</summary>
        public TileColour Colour { get; }

        /// <summary>Fillers stand in when the pool is empty. They are not part of the deck.</summary>
        public bool IsFiller { get; }

        public Tile(TileDataSO data) : this(data, data != null ? data.Colour : TileColour.None, false)
        {
        }

        private Tile(TileDataSO data, TileColour colour, bool isFiller)
        {
            Data = data;
            Colour = colour;
            IsFiller = isFiller;
        }

        /// <summary>Creates a filler from a filler tile definition.</summary>
        public static Tile CreateFiller(TileDataSO data)
        {
            return new Tile(data, data != null ? data.Colour : TileColour.None, true);
        }

        /// <summary>Creates a plain filler of the given colour, used when no filler tile definitions are available.</summary>
        public static Tile CreateFiller(TileColour colour)
        {
            return new Tile(null, colour, true);
        }
    }
}
