namespace ggo2026
{
    /// <summary>A combatant's health reached 0. The fight is over (<see cref="Combat.IsOver"/>).</summary>
    public class CombatantDiedEvent : CombatEvent
    {
        public CombatantDiedEvent(Combatant target) : base(target)
        {
        }
    }
}
