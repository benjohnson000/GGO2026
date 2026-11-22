using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// The turn flow of one fight, with no presentation. The enemy telegraphs an intent with a countdown in swaps;
    /// each accepted swap ticks it down, and at 0 the enemy takes its turn:
    /// <code>
    /// StartTurn(enemy) → intent actions → StartTurn(player) → pick the next intent
    /// </code>
    /// Swaps that only tick the countdown are not turns. Like <see cref="Combat"/>, every call resolves instantly
    /// and returns the events it produced, in order.
    /// </summary>
    public class Encounter
    {
        private readonly IReadOnlyList<EnemyIntent> intents;
        private readonly System.Random rng;

        public Combat Combat { get; }
        public Combatant Player => Combat.Player;
        public Combatant Enemy => Combat.Enemy;

        /// <summary>The intent the enemy is telegraphing, or null before <see cref="Begin"/> or with no intents.</summary>
        public EnemyIntent CurrentIntent { get; private set; }

        public bool IsOver => Combat.IsOver;
        public bool PlayerWon => Enemy.IsDead && !Player.IsDead;

        public Encounter(Combat combat, IReadOnlyList<EnemyIntent> intents, System.Random rng)
        {
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.intents = intents ?? Array.Empty<EnemyIntent>();
            this.rng = rng ?? new System.Random();
        }

        /// <summary>Picks the enemy's first intent. Call once, before the first swap.</summary>
        public List<CombatEvent> Begin()
        {
            List<CombatEvent> events = new List<CombatEvent>();
            if (intents.Count == 0) Debug.LogWarning($"Encounter: {Enemy.DisplayName} has no intents, so it never acts.");
            ChooseIntent(events);
            return events;
        }

        /// <summary>
        /// Call after an accepted swap has fully resolved (matches, cascades and, later, passives).
        /// Ticks the enemy's countdown and, if it reaches 0, runs the enemy's turn and the start of the player's.
        /// </summary>
        public List<CombatEvent> AdvanceSwap()
        {
            List<CombatEvent> events = new List<CombatEvent>();
            if (IsOver || CurrentIntent == null) return events;

            events.AddRange(Combat.Execute(new ChangeCountdownAction(Enemy, -1)));
            // Checked after resolving, since a listener may have delayed or advanced the countdown.
            if (!IsOver && Enemy.IntentCountdown <= 0) RunEnemyTurn(events);
            return events;
        }

        private void RunEnemyTurn(List<CombatEvent> events)
        {
            EnemyIntent intent = CurrentIntent;

            events.AddRange(Combat.StartTurn(Enemy));
            if (IsOver) return;

            foreach (IntentAction intentAction in intent.Actions)
            {
                CombatAction action = intentAction?.CreateAction(Combat, Enemy);
                if (action != null) Combat.Enqueue(action);
            }
            events.AddRange(Combat.Resolve());
            if (IsOver) return;

            events.AddRange(Combat.StartTurn(Player));
            if (IsOver) return;

            ChooseIntent(events);
        }

        private void ChooseIntent(List<CombatEvent> events)
        {
            CurrentIntent = PickRandomIntent();
            events.Add(new IntentChangedEvent(Enemy, CurrentIntent));
            if (CurrentIntent != null) events.AddRange(Combat.Execute(new SetCountdownAction(Enemy, CurrentIntent.Countdown)));
        }

        private EnemyIntent PickRandomIntent()
        {
            int total = 0;
            foreach (EnemyIntent intent in intents)
            {
                if (intent != null) total += intent.Weight;
            }
            if (total == 0) return null;

            int roll = rng.Next(total);
            foreach (EnemyIntent intent in intents)
            {
                if (intent == null) continue;
                roll -= intent.Weight;
                if (roll < 0) return intent;
            }
            return null;
        }
    }
}
