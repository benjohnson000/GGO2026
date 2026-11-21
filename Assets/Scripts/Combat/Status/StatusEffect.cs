namespace ggo2026
{
    /// <summary>
    /// A status on one combatant: a stack count plus behaviour through the <see cref="CombatListener"/> hooks.
    /// Created by <see cref="StatusDataSO.CreateEffect"/> the first time the status is applied; stacks are changed
    /// only through <see cref="ApplyStatusAction"/>. This base class has no behaviour, so it suits plain counters.
    /// </summary>
    public class StatusEffect : CombatListener
    {
        public StatusDataSO Data { get; internal set; }
        public int Stacks { get; internal set; }
    }
}
