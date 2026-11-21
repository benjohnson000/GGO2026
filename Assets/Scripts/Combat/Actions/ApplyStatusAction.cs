namespace ggo2026
{
    /// <summary>
    /// Adds stacks of a status, creating it if needed. Negative stacks remove stacks; a status at 0 stacks is removed.
    /// </summary>
    public class ApplyStatusAction : CombatAction
    {
        public Combatant Target { get; }
        public StatusDataSO Status { get; }
        public int Stacks { get; }

        public ApplyStatusAction(Combatant target, StatusDataSO status, int stacks)
        {
            Target = target;
            Status = status;
            Stacks = stacks;
        }

        internal override void Execute(Combat combat) => combat.ResolveStatus(Target, Status, Stacks);
    }
}
