namespace ggo2026
{
    /// <summary>Restores health, up to max health.</summary>
    public class HealAction : CombatAction
    {
        public Combatant Target { get; }
        public int Amount { get; }

        public HealAction(Combatant target, int amount)
        {
            Target = target;
            Amount = amount;
        }

        internal override void Execute(Combat combat) => combat.ResolveHeal(Target, Amount);
    }
}
