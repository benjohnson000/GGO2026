using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>
    /// The deck as a pool. Tiles leave it when drawn onto the board and come back only when destroyed.
    /// Drawing from an empty pool produces a filler, so the deck has no minimum size.
    /// </summary>
    public class TilePool
    {
        private readonly List<Tile> tiles = new List<Tile>();
        private readonly List<TileDataSO> fillerTiles = new List<TileDataSO>();
        private readonly System.Random rng;

        /// <summary>Tiles currently in the pool (not on the board).</summary>
        public int Count => tiles.Count;

        /// <summary>True when fillers come from tile definitions rather than the plain-colour fallback.</summary>
        public bool HasFillerTiles => fillerTiles.Count > 0;

        public TilePool(IReadOnlyList<TileDataSO> fillerTiles, System.Random rng)
        {
            this.rng = rng ?? new System.Random();

            if (fillerTiles == null) return;
            foreach (TileDataSO data in fillerTiles)
            {
                // Fillers with no colour could never match, which the design rejects.
                if (data != null && TileColourUtils.IsMatchable(data.Colour) && !this.fillerTiles.Contains(data)) this.fillerTiles.Add(data);
            }
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
            if (tiles.Count == 0) return CreateFiller();

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

        private Tile CreateFiller()
        {
            if (fillerTiles.Count > 0) return Tile.CreateFiller(fillerTiles[rng.Next(fillerTiles.Count)]);
            return Tile.CreateFiller(TileColourUtils.PickRandom(TileColourUtils.All, rng));
        }
    }
}
