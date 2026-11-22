namespace ggo2026
{
    /// <summary>
    /// Adds swaps to a combatant's intent countdown (positive delays it, as stun does; negative brings it closer).
    /// The countdown never goes below 0.
    /// </summary>
    public class ChangeCountdownAction : CombatAction
    {
        public Combatant Target { get; }
        public int Swaps { get; }

        public ChangeCountdownAction(Combatant target, int swaps)
        {
            Target = target;
            Swaps = swaps;
        }

        internal override void Execute(Combat combat)
        {
            if (Target == null) return;
            combat.ResolveCountdown(Target, Target.IntentCountdown + Swaps);
        }
    }
}
