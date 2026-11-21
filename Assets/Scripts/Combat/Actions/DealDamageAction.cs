namespace ggo2026
{
    /// <summary>Deals damage: outgoing and incoming modifiers, then block, then health.</summary>
    public class DealDamageAction : CombatAction
    {
        public DamageInfo Damage { get; }

        public DealDamageAction(DamageInfo damage)
        {
            Damage = damage ?? throw new System.ArgumentNullException(nameof(damage));
        }

        public DealDamageAction(Combatant source, Combatant target, int amount, TileColour colour = TileColour.None)
            : this(new DamageInfo(source, target, amount, DamageKind.Attack, colour))
        {
        }

        internal override void Execute(Combat combat) => combat.ResolveDamage(Damage);
    }
}
