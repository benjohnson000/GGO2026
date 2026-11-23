namespace ggo2026
{
    /// <summary>
    /// Marks the end of an accepted swap, after the board has resolved and before the enemy countdown ticks.
    /// Fires <see cref="CombatListener.OnSwapEnded"/> for every listener. Queued by <see cref="Encounter.AdvanceSwap"/>.
    /// </summary>
    public class EndSwapAction : CombatAction
    {
        internal override void Execute(Combat combat) => combat.ResolveEndSwap();
    }
}
