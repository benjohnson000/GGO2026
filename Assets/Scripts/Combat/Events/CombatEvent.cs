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

        private DamagePreview targetPreview;
        private DamagePreview opponentPreview;

        protected CombatEvent(Combatant target)
        {
            Target = target;
        }

        /// <summary>
        /// The damage preview for <paramref name="combatant"/> as it stood just after this event, or null if the
        /// event was not logged by <see cref="Combat"/>. Either side's, since one side's change can affect the other's.
        /// </summary>
        public DamagePreview GetPreview(Combatant combatant)
        {
            return combatant == Target ? targetPreview : opponentPreview;
        }

        internal void SetPreviews(DamagePreview target, DamagePreview opponent)
        {
            targetPreview = target;
            opponentPreview = opponent;
        }
    }
}
