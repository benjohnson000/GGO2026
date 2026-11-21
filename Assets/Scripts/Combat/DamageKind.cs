namespace ggo2026
{
    /// <summary>Where damage came from, so modifiers can tell attacks from status ticks.</summary>
    public enum DamageKind
    {
        /// <summary>Tile effects and enemy attacks.</summary>
        Attack,

        /// <summary>Status effects such as poison.</summary>
        Status,
    }
}
