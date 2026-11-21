namespace ggo2026
{
    /// <summary>
    /// One hit on its way through the damage pipeline. Listeners may change <see cref="Amount"/> while it is
    /// being modified; once the <see cref="DamageEvent"/> is logged it holds the final amount.
    /// </summary>
    public class DamageInfo
    {
        /// <summary>Who dealt the damage, or null when nobody did (poison, the environment).</summary>
        public Combatant Source { get; }
        public Combatant Target { get; }
        public DamageKind Kind { get; }

        /// <summary>Colour of the tile that caused the damage, or <see cref="TileColour.None"/>. For weaknesses, resistances and charms.</summary>
        public TileColour Colour { get; }

        /// <summary>Damage before block. Modifiers change this; it is clamped to 0 or more before block applies.</summary>
        public int Amount { get; set; }

        /// <summary>True if block does not absorb this hit.</summary>
        public bool IgnoresBlock { get; set; }

        public DamageInfo(Combatant source, Combatant target, int amount, DamageKind kind = DamageKind.Attack, TileColour colour = TileColour.None)
        {
            Source = source;
            Target = target;
            Amount = amount;
            Kind = kind;
            Colour = colour;
        }
    }
}
