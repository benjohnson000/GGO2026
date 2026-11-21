namespace ggo2026
{
    /// <summary>
    /// What a combo tile does to the board when it is destroyed. Serialized in <see cref="ComboRule"/>,
    /// so append new values; never reorder.
    /// </summary>
    public enum ComboEffect
    {
        /// <summary>No board effect yet. The tile is still a temporary combo tile.</summary>
        None = 0,

        /// <summary>Destroys every other tile in its row.</summary>
        ClearRow = 1,
    }
}
