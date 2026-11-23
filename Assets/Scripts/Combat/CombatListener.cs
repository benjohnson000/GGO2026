namespace ggo2026
{
    /// <summary>
    /// Base for anything that changes or reacts to a fight: statuses, charms, enemy affixes. Every hook does
    /// nothing by default; override only what you need.
    /// <para>
    /// <b>Modifiers</b> (<c>Modify*</c>) run only for listeners owned by the side they name: outgoing damage for the
    /// attacker's, incoming damage and block for the target's. They may only change the value passed in.
    /// <see cref="Combat.PreviewDamage"/> also runs them, for hits that haven't happened, so never use one to count
    /// or use something up: do that in a trigger.
    /// </para>
    /// <para>
    /// <b>Triggers</b> (<c>On*</c>) run for every listener, those owned by the event's target first. Check
    /// <see cref="Owner"/> to tell "you" from "the enemy". To change the fight, call <see cref="Combat.Enqueue"/>;
    /// never change a <see cref="Combatant"/> directly.
    /// </para>
    /// </summary>
    public abstract class CombatListener
    {
        /// <summary>The side this listener belongs to. Set when it is added to a fight or applied as a status.</summary>
        public Combatant Owner { get; internal set; }

        /// <summary>The owner is dealing <paramref name="damage"/>. Change its amount or flags.</summary>
        public virtual void ModifyOutgoingDamage(Combat combat, DamageInfo damage) { }

        /// <summary>The owner is about to take <paramref name="damage"/>, before block. Change its amount or flags.</summary>
        public virtual void ModifyIncomingDamage(Combat combat, DamageInfo damage) { }

        /// <summary>The owner is gaining <paramref name="amount"/> block. Returns the amount to gain.</summary>
        public virtual int ModifyBlockGained(Combat combat, int amount) => amount;

        /// <summary>The owner's turn is starting and they will lose <paramref name="amount"/> block (all of it by default). Returns the amount to lose.</summary>
        public virtual int ModifyBlockLost(Combat combat, int amount) => amount;

        public virtual void OnDamageDealt(Combat combat, DamageEvent damageEvent) { }

        public virtual void OnBlockChanged(Combat combat, BlockChangedEvent blockEvent) { }

        public virtual void OnHealed(Combat combat, HealedEvent healedEvent) { }

        public virtual void OnStatusChanged(Combat combat, StatusChangedEvent statusEvent) { }

        /// <summary>A turn started, after that combatant's block was removed.</summary>
        public virtual void OnTurnStarted(Combat combat, Combatant combatant) { }

        /// <summary>An accepted swap has fully resolved. Runs before the enemy countdown ticks, so before any turn it causes.</summary>
        public virtual void OnSwapEnded(Combat combat) { }

        /// <summary>An intent countdown changed: ticked down by a swap, delayed (e.g. by stun) or set for a new intent.</summary>
        public virtual void OnCountdownChanged(Combat combat, CountdownChangedEvent countdownEvent) { }

        public virtual void OnCombatantDied(Combat combat, CombatantDiedEvent diedEvent) { }
    }
}
