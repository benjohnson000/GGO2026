namespace ggo2026
{
    /// <summary>
    /// The colour a tile matches as. Every tile has exactly one.
    /// Serialized in assets: append new values, never renumber existing ones.
    /// The values are not contiguous because they were bit flags when tiles could have several colours.
    /// </summary>
    public enum TileColour
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 4,
        Yellow = 8,
        Purple = 16,
    }
}
