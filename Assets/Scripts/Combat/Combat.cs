using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Rules of one fight between the player and an enemy, with no presentation. Like <see cref="Board"/>,
    /// every operation resolves instantly and returns the <see cref="CombatEvent"/>s it produced, in order.
    /// <para>
    /// All changes are <see cref="CombatAction"/>s run through a queue. Listeners (statuses, charms) never change
    /// state directly: they call <see cref="Enqueue"/>, and the queue runs until it is empty. This keeps chains
    /// of triggers in a predictable order and stops two triggers from calling each other forever.
    /// </para>
    /// </summary>
    public class Combat
    {
        // Safety limit so a loop of triggers can never hang the game.
        private const int MaxActionsPerResolve = 1000;

        private readonly Queue<CombatAction> queue = new Queue<CombatAction>();
        private readonly List<CombatListener> listeners = new List<CombatListener>();
        private readonly List<CombatListener> buffer = new List<CombatListener>();
        private List<CombatEvent> events;

        public Combatant Player { get; }
        public Combatant Enemy { get; }

        /// <summary>True once either side has died.</summary>
        public bool IsOver => Player.IsDead || Enemy.IsDead;

        /// <summary>True while the queue is running. Actions enqueued now run in the same resolve.</summary>
        public bool IsResolving => events != null;

        /// <summary>Listeners not tied to a status, such as charms and enemy affixes, in the order they were added.</summary>
        public IReadOnlyList<CombatListener> Listeners => listeners;

        public Combat(Combatant player, Combatant enemy)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            if (player == enemy) throw new ArgumentException("Player and enemy must be different combatants.", nameof(enemy));
        }

        /// <summary>The other side of the fight.</summary>
        public Combatant GetOpponent(Combatant combatant)
        {
            return combatant == Player ? Enemy : Player;
        }

        /// <summary>Adds a listener that is not a status (a charm or an affix), owned by <paramref name="owner"/>.</summary>
        public void AddListener(CombatListener listener, Combatant owner)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (owner != Player && owner != Enemy) throw new ArgumentException("The owner must be in this fight.", nameof(owner));

            listener.Owner = owner;
            listeners.Add(listener);
        }

        public void RemoveListener(CombatListener listener)
        {
            listeners.Remove(listener);
        }

        /// <summary>
        /// Queues an action. Outside a resolve, it waits for the next <see cref="Resolve"/> or <see cref="Execute"/>;
        /// during one (from a listener), it runs after the actions already queued.
        /// </summary>
        public void Enqueue(CombatAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            queue.Enqueue(action);
        }

        /// <summary>Queues an action and resolves the queue.</summary>
        public List<CombatEvent> Execute(CombatAction action)
        {
            Enqueue(action);
            return Resolve();
        }

        /// <summary>Starts a combatant's turn: logs the turn, removes their block, then fires turn-start triggers.</summary>
        public List<CombatEvent> StartTurn(Combatant combatant)
        {
            return Execute(new StartTurnAction(combatant));
        }

        /// <summary>
        /// Runs queued actions until none are left, including any that listeners queue along the way.
        /// Called during a resolve (by a listener), it returns an empty list and the outer resolve carries on.
        /// </summary>
        public List<CombatEvent> Resolve()
        {
            if (IsResolving) return new List<CombatEvent>();

            List<CombatEvent> resolved = new List<CombatEvent>();
            events = resolved;
            try
            {
                int count = 0;
                while (queue.Count > 0)
                {
                    if (count++ >= MaxActionsPerResolve)
                    {
                        Debug.LogWarning($"Combat: stopped after {MaxActionsPerResolve} actions in one resolve. Two triggers may be feeding each other.");
                        queue.Clear();
                        break;
                    }
                    queue.Dequeue().Execute(this);
                }
            }
            finally
            {
                events = null;
            }
            return resolved;
        }

        internal void ResolveDamage(DamageInfo damage)
        {
            Combatant target = damage.Target;
            if (!IsInFight(target) || target.IsDead) return;

            if (damage.Source != null)
            {
                foreach (CombatListener listener in GetListenersOwnedBy(damage.Source)) listener.ModifyOutgoingDamage(this, damage);
            }
            foreach (CombatListener listener in GetListenersOwnedBy(target)) listener.ModifyIncomingDamage(this, damage);

            damage.Amount = Mathf.Max(0, damage.Amount);
            int blocked = damage.IgnoresBlock ? 0 : Mathf.Min(target.Block, damage.Amount);
            int healthLost = Mathf.Min(target.Health, damage.Amount - blocked);
            target.Block -= blocked;
            target.Health -= healthLost;

            DamageEvent damageEvent = new DamageEvent(damage, blocked, healthLost);
            Log(damageEvent);
            foreach (CombatListener listener in GetListeners(target)) listener.OnDamageDealt(this, damageEvent);

            if (target.Health == 0) Kill(target);
        }

        internal void ResolveGainBlock(Combatant target, int amount)
        {
            if (!IsInFight(target) || target.IsDead) return;

            foreach (CombatListener listener in GetListenersOwnedBy(target)) amount = listener.ModifyBlockGained(this, amount);
            if (amount <= 0) return;

            ChangeBlock(target, target.Block + amount);
        }

        internal void ResolveHeal(Combatant target, int amount)
        {
            if (!IsInFight(target) || target.IsDead) return;

            int healed = Mathf.Clamp(amount, 0, target.MaxHealth - target.Health);
            if (healed == 0) return;

            target.Health += healed;
            HealedEvent healedEvent = new HealedEvent(target, healed, target.Health);
            Log(healedEvent);
            foreach (CombatListener listener in GetListeners(target)) listener.OnHealed(this, healedEvent);
        }

        internal void ResolveStatus(Combatant target, StatusDataSO data, int stacks)
        {
            if (!IsInFight(target) || target.IsDead || data == null || stacks == 0) return;

            StatusEffect status = target.GetStatus(data);
            int oldStacks = status != null ? status.Stacks : 0;
            int newStacks = Mathf.Max(0, oldStacks + stacks);
            if (newStacks == oldStacks) return;

            if (status == null)
            {
                status = data.CreateEffect();
                if (status == null)
                {
                    Debug.LogError($"Combat: {data.name} created no status effect.", data);
                    return;
                }
                status.Data = data;
                status.Owner = target;
                target.AddStatus(status);
            }

            status.Stacks = newStacks;
            if (newStacks == 0) target.RemoveStatus(status);

            StatusChangedEvent statusEvent = new StatusChangedEvent(target, data, oldStacks, newStacks);
            Log(statusEvent);
            foreach (CombatListener listener in GetListeners(target)) listener.OnStatusChanged(this, statusEvent);
        }

        internal void ResolveStartTurn(Combatant combatant)
        {
            if (!IsInFight(combatant) || combatant.IsDead) return;

            Log(new TurnStartedEvent(combatant));

            int lost = combatant.Block;
            foreach (CombatListener listener in GetListenersOwnedBy(combatant)) lost = listener.ModifyBlockLost(this, lost);
            lost = Mathf.Clamp(lost, 0, combatant.Block);
            if (lost > 0) ChangeBlock(combatant, combatant.Block - lost);

            foreach (CombatListener listener in GetListeners(combatant)) listener.OnTurnStarted(this, combatant);
        }

        private void ChangeBlock(Combatant target, int newBlock)
        {
            int oldBlock = target.Block;
            target.Block = newBlock;

            BlockChangedEvent blockEvent = new BlockChangedEvent(target, oldBlock, newBlock);
            Log(blockEvent);
            foreach (CombatListener listener in GetListeners(target)) listener.OnBlockChanged(this, blockEvent);
        }

        private void Kill(Combatant target)
        {
            target.IsDead = true;

            CombatantDiedEvent diedEvent = new CombatantDiedEvent(target);
            Log(diedEvent);
            foreach (CombatListener listener in GetListeners(target)) listener.OnCombatantDied(this, diedEvent);
        }

        private bool IsInFight(Combatant combatant)
        {
            return combatant == Player || combatant == Enemy;
        }

        private void Log(CombatEvent combatEvent)
        {
            events?.Add(combatEvent);
        }

        /// <summary>
        /// Every listener, in trigger order: those owned by <paramref name="first"/> (statuses, then other listeners),
        /// then those owned by the opponent. Snapshot into a reused buffer, so listeners may queue actions freely.
        /// </summary>
        private List<CombatListener> GetListeners(Combatant first)
        {
            buffer.Clear();
            AddOwnedBy(first);
            AddOwnedBy(GetOpponent(first));
            return buffer;
        }

        /// <summary>Only the listeners owned by <paramref name="owner"/>, for modifiers that act on their own side.</summary>
        private List<CombatListener> GetListenersOwnedBy(Combatant owner)
        {
            buffer.Clear();
            AddOwnedBy(owner);
            return buffer;
        }

        private void AddOwnedBy(Combatant owner)
        {
            foreach (StatusEffect status in owner.Statuses) buffer.Add(status);
            foreach (CombatListener listener in listeners)
            {
                if (listener.Owner == owner) buffer.Add(listener);
            }
        }
    }
}
