namespace ggo2026
{
    /// <summary>A combatant's turn started. Logged before their block is removed and before turn-start triggers.</summary>
    public class TurnStartedEvent : CombatEvent
    {
        public TurnStartedEvent(Combatant target) : base(target)
        {
        }
    }
}
