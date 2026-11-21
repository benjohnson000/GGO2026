namespace ggo2026
{
    /// <summary>Health was restored. <see cref="Amount"/> is what was actually healed, after the max-health cap.</summary>
    public class HealedEvent : CombatEvent
    {
        public int Amount { get; }
        public int HealthAfter { get; }

        public HealedEvent(Combatant target, int amount, int healthAfter) : base(target)
        {
            Amount = amount;
            HealthAfter = healthAfter;
        }
    }
}
