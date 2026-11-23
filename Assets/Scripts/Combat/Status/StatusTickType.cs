namespace ggo2026
{
    /// <summary>When a status that acts repeatedly (such as poison) ticks.</summary>
    public enum StatusTickType
    {
        /// <summary>At the start of its owner's turn, which comes when the enemy's intent fires.</summary>
        PerTurn,

        /// <summary>After every accepted swap, before the enemy countdown ticks.</summary>
        PerSwap,
    }
}
