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

        // Previewed for both tick types: per swap, it is the hit the next swap will deal.
        public override DamageInfo CreateTurnStartDamage()
        {
            // Nobody deals poison damage, so it gets no outgoing modifiers.
            return new DamageInfo(null, Owner, Stacks, DamageKind.Status) { IgnoresBlock = settings.IgnoresBlock };
        }

        public override void OnTurnStarted(Combat combat, Combatant combatant)
        {
            if (settings.TickType != StatusTickType.PerTurn || combatant != Owner) return;
            Tick(combat);
        }

        public override void OnSwapEnded(Combat combat)
        {
            if (settings.TickType != StatusTickType.PerSwap) return;
            Tick(combat);
        }

        private void Tick(Combat combat)
        {
            if (Stacks <= 0) return;

            combat.Enqueue(new DealDamageAction(CreateTurnStartDamage()));
            combat.Enqueue(new ApplyStatusAction(Owner, Data, -settings.StacksLostPerTick));
        }
    }
}
