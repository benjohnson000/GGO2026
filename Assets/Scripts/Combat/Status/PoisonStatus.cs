namespace ggo2026
{
    /// <summary>Runtime behaviour for <see cref="PoisonStatusSO"/>.</summary>
    public class PoisonStatus : StatusEffect
    {
        private readonly PoisonStatusSO settings;

        public PoisonStatus(PoisonStatusSO settings)
        {
            this.settings = settings;
        }

        public override void OnTurnStarted(Combat combat, Combatant combatant)
        {
            if (combatant != Owner || Stacks <= 0) return;

            // Nobody deals poison damage, so it gets no outgoing modifiers.
            DamageInfo damage = new DamageInfo(null, Owner, Stacks, DamageKind.Status) { IgnoresBlock = settings.IgnoresBlock };
            combat.Enqueue(new DealDamageAction(damage));
            combat.Enqueue(new ApplyStatusAction(Owner, Data, -settings.StacksLostPerTick));
        }
    }
}
