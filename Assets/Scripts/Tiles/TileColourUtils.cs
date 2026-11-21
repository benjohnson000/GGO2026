using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>Helpers for <see cref="TileColour"/>.</summary>
    public static class TileColourUtils
    {
        /// <summary>Every matchable colour, in declaration order.</summary>
        public static readonly IReadOnlyList<TileColour> All = new[]
        {
            TileColour.Red,
            TileColour.Blue,
            TileColour.Green,
            TileColour.Yellow,
            TileColour.Purple,
        };

        /// <summary>True for a defined colour other than None.</summary>
        public static bool IsMatchable(TileColour colour)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == colour) return true;
            }
            return false;
        }

        /// <summary>One colour picked at random from the list, or None if the list is empty.</summary>
        public static TileColour PickRandom(IReadOnlyList<TileColour> colours, System.Random rng)
        {
            if (colours == null || colours.Count == 0) return TileColour.None;
            return colours[rng.Next(colours.Count)];
        }
    }
}
