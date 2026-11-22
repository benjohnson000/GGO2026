namespace ggo2026
{
    /// <summary>Sets a combatant's intent countdown, for example when a new intent is chosen.</summary>
    public class SetCountdownAction : CombatAction
    {
        public Combatant Target { get; }
        public int Swaps { get; }

        public SetCountdownAction(Combatant target, int swaps)
        {
            Target = target;
            Swaps = swaps;
        }

        internal override void Execute(Combat combat) => combat.ResolveCountdown(Target, Swaps);
    }
}
