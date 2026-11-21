namespace ggo2026
{
    /// <summary>Starts a combatant's turn: their block is removed (after block-loss modifiers), then turn-start triggers fire.</summary>
    public class StartTurnAction : CombatAction
    {
        public Combatant Combatant { get; }

        public StartTurnAction(Combatant combatant)
        {
            Combatant = combatant;
        }

        internal override void Execute(Combat combat) => combat.ResolveStartTurn(Combatant);
    }
}
