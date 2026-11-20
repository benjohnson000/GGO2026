using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>
    /// The deck as a pool. Tiles leave it when drawn onto the board and come back only when destroyed.
    /// Drawing from an empty pool produces a filler of a random colour, so the deck has no minimum size.
    /// </summary>
    public class TilePool
    {
        private readonly List<Tile> tiles = new List<Tile>();
        private readonly TileColour fillerColours;
        private readonly System.Random rng;

        /// <summary>Tiles currently in the pool (not on the board).</summary>
        public int Count => tiles.Count;

        public TilePool(TileColour fillerColours, System.Random rng)
        {
            this.rng = rng ?? new System.Random();

            // An empty filler mask would make unmatchable fillers, which the design rejects.
            fillerColours = TileColourUtils.Sanitize(fillerColours);
            this.fillerColours = fillerColours != TileColour.None ? fillerColours : TileColourUtils.All;
        }

        /// <summary>Adds a tile to the deck.</summary>
        public void Add(Tile tile)
        {
            if (tile == null || tile.IsFiller) return;
            tiles.Add(tile);
        }

        /// <summary>Removes a random tile from the pool, or creates a filler if the pool is empty.</summary>
        public Tile Draw()
        {
            if (tiles.Count == 0) return Tile.CreateFiller(TileColourUtils.PickRandom(fillerColours, rng));

            int index = rng.Next(tiles.Count);
            int last = tiles.Count - 1;
            Tile tile = tiles[index];
            tiles[index] = tiles[last];
            tiles.RemoveAt(last);
            return tile;
        }

        /// <summary>Puts a destroyed tile back. Fillers are discarded because they are not part of the deck.</summary>
        public void Return(Tile tile)
        {
            if (tile == null || tile.IsFiller) return;
            tiles.Add(tile);
        }
    }
}
