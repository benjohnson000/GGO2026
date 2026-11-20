namespace ggo2026
{
    /// <summary>Bit helpers for <see cref="TileColour"/> masks.</summary>
    public static class TileColourUtils
    {
        /// <summary>Every defined colour.</summary>
        public const TileColour All = TileColour.Red | TileColour.Blue | TileColour.Green | TileColour.Yellow | TileColour.Purple;

        /// <summary>
        /// Strips bits that are not defined colours. The inspector's "Everything" option can set every bit,
        /// so masks read from assets should go through this.
        /// </summary>
        public static TileColour Sanitize(TileColour colours)
        {
            return colours & All;
        }

        /// <summary>Number of colours set in the mask.</summary>
        public static int Count(TileColour colours)
        {
            int count = 0;
            for (int bit = 1; bit <= (int)All; bit <<= 1)
            {
                if (((int)colours & bit) != 0) count++;
            }
            return count;
        }

        /// <summary>Lowest colour set in the mask, or None if the mask is empty.</summary>
        public static TileColour Lowest(TileColour colours)
        {
            int value = (int)Sanitize(colours);
            return (TileColour)(value & -value);
        }

        /// <summary>One colour picked at random from the mask, or None if the mask is empty.</summary>
        public static TileColour PickRandom(TileColour colours, System.Random rng)
        {
            colours = Sanitize(colours);
            int count = Count(colours);
            if (count == 0) return TileColour.None;

            int pick = rng.Next(count);
            for (int bit = 1; bit <= (int)All; bit <<= 1)
            {
                if (((int)colours & bit) == 0) continue;
                if (pick == 0) return (TileColour)bit;
                pick--;
            }
            return TileColour.None;
        }
    }
}
