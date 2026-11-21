namespace ggo2026
{
    /// <summary>
    /// Something that happened in a fight. <see cref="Combat"/> resolves instantly and records these in order;
    /// the view plays them back. Events carry the values after the change, so a view that lags behind the rules
    /// can still show the right numbers at each step.
    /// </summary>
    public abstract class CombatEvent
    {
        /// <summary>The combatant this happened to.</summary>
        public Combatant Target { get; }

        protected CombatEvent(Combatant target)
        {
            Target = target;
        }
    }
}
