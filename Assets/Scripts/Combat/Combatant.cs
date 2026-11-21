using System;
using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>
    /// One side of a fight: health, block and status effects. Holds state only; every change goes through
    /// <see cref="Combat"/> so modifiers, triggers and the event log are never skipped.
    /// </summary>
    public class Combatant
    {
        private readonly List<StatusEffect> statuses = new List<StatusEffect>();

        public string DisplayName { get; }
        public int MaxHealth { get; internal set; }
        public int Health { get; internal set; }
        public int Block { get; internal set; }
        public bool IsDead { get; internal set; }

        /// <summary>Active statuses in the order they were first applied. Every entry has at least 1 stack.</summary>
        public IReadOnlyList<StatusEffect> Statuses => statuses;

        /// <param name="health">Starting health, for health carried over from an earlier fight. Negative means full health.</param>
        public Combatant(string displayName, int maxHealth, int health = -1)
        {
            if (maxHealth < 1) throw new ArgumentOutOfRangeException(nameof(maxHealth), $"Max health must be at least 1, got {maxHealth}.");

            DisplayName = displayName;
            MaxHealth = maxHealth;
            Health = health < 0 ? maxHealth : Math.Min(health, maxHealth);
            IsDead = Health == 0;
        }

        /// <summary>The active status made from <paramref name="data"/>, or null.</summary>
        public StatusEffect GetStatus(StatusDataSO data)
        {
            foreach (StatusEffect status in statuses)
            {
                if (status.Data == data) return status;
            }
            return null;
        }

        /// <summary>Stacks of the status made from <paramref name="data"/>, or 0 if it is not active.</summary>
        public int GetStacks(StatusDataSO data)
        {
            StatusEffect status = GetStatus(data);
            return status != null ? status.Stacks : 0;
        }

        internal void AddStatus(StatusEffect status) => statuses.Add(status);

        internal void RemoveStatus(StatusEffect status) => statuses.Remove(status);
    }
}
