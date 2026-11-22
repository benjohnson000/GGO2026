namespace ggo2026
{
    /// <summary>A combatant's intent countdown changed. At 0 the intent fires (see <see cref="Encounter"/>).</summary>
    public class CountdownChangedEvent : CombatEvent
    {
        public int OldCountdown { get; }
        public int NewCountdown { get; }

        public CountdownChangedEvent(Combatant target, int oldCountdown, int newCountdown) : base(target)
        {
            OldCountdown = oldCountdown;
            NewCountdown = newCountdown;
        }
    }
}
