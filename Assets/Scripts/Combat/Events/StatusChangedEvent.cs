namespace ggo2026
{
    /// <summary>A status was applied, gained or lost stacks, or was removed (<see cref="NewStacks"/> is 0).</summary>
    public class StatusChangedEvent : CombatEvent
    {
        public StatusDataSO Status { get; }
        public int OldStacks { get; }
        public int NewStacks { get; }

        public bool WasApplied => OldStacks == 0;
        public bool WasRemoved => NewStacks == 0;

        public StatusChangedEvent(Combatant target, StatusDataSO status, int oldStacks, int newStacks) : base(target)
        {
            Status = status;
            OldStacks = oldStacks;
            NewStacks = newStacks;
        }
    }
}
