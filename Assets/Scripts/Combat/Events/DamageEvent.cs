namespace ggo2026
{
    /// <summary>
    /// A hit landed, including one fully absorbed by block. Check <see cref="HealthLost"/> for "whenever you take damage".
    /// </summary>
    public class DamageEvent : CombatEvent
    {
        /// <summary>The hit, with its final amount after modifiers.</summary>
        public DamageInfo Damage { get; }
        public int Blocked { get; }
        public int HealthLost { get; }
        public int BlockAfter { get; }
        public int HealthAfter { get; }

        public Combatant Source => Damage.Source;

        public DamageEvent(DamageInfo damage, int blocked, int healthLost) : base(damage.Target)
        {
            Damage = damage;
            Blocked = blocked;
            HealthLost = healthLost;
            BlockAfter = damage.Target.Block;
            HealthAfter = damage.Target.Health;
        }
    }
}
