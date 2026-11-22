namespace ggo2026
{
    /// <summary>
    /// The enemy chose a new intent. Logged by <see cref="Encounter"/>, not <see cref="Combat"/>, so listeners don't
    /// receive it. A <see cref="CountdownChangedEvent"/> with the intent's countdown follows.
    /// </summary>
    public class IntentChangedEvent : CombatEvent
    {
        /// <summary>The new intent, or null when the enemy has none.</summary>
        public EnemyIntent Intent { get; }

        public IntentChangedEvent(Combatant target, EnemyIntent intent) : base(target)
        {
            Intent = intent;
        }
    }
}
