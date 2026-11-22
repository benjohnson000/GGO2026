namespace ggo2026
{
    /// <summary>Which side a tile effect acts on. Serialized: append values, never renumber.</summary>
    public enum TileEffectTarget
    {
        /// <summary>The other side (the enemy, for player tiles).</summary>
        Opponent = 0,

        /// <summary>The side the tile belongs to (the player).</summary>
        Owner = 1,
    }
}
