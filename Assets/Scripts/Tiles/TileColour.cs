using System;

namespace ggo2026
{
    /// <summary>
    /// Colours a tile can match as. Flags, so a multi-colour tile is several bits set at once.
    /// Serialized in assets: append new values, never reorder or renumber existing ones.
    /// </summary>
    [Flags]
    public enum TileColour
    {
        None = 0,
        Red = 1 << 0,
        Blue = 1 << 1,
        Green = 1 << 2,
        Yellow = 1 << 3,
        Purple = 1 << 4,
    }
}
