namespace ggo2026
{
    /// <summary>
    /// Where a tile layer (shape, pattern, icon) takes its tint from: one of the palette colours for the tile's
    /// <see cref="TileColour"/>, the palette's shared highlight colour, or a colour set on the tile itself.
    /// Serialized in assets: append new values, never renumber existing ones.
    /// </summary>
    public enum TileColourSource
    {
        Primary = 0,
        Secondary = 1,
        Tertiary = 2,
        Override = 3,
        Highlight = 4,
    }
}
