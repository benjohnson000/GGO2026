namespace ggo2026
{
    /// <summary>Adds block to a combatant, after the owner's block-gain modifiers.</summary>
    public class GainBlockAction : CombatAction
    {
        public Combatant Target { get; }
        public int Amount { get; }

        public GainBlockAction(Combatant target, int amount)
        {
            Target = target;
            Amount = amount;
        }

        internal override void Execute(Combat combat) => combat.ResolveGainBlock(Target, Amount);
    }
}
