using System;
using System.Collections.Generic;
using System.Text;
using Animancer;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// One move an enemy can telegraph: what it does when it fires, how many swaps until it fires, and how likely
    /// it is to be picked. Listed on an <see cref="EnemyDataSO"/>.
    /// </summary>
    [Serializable]
    public class EnemyIntent
    {
        [Tooltip("Optional name shown instead of the generated description, e.g. \"Crushing Blow\".")]
        [SerializeField] private string label;

        [Min(1)]
        [Tooltip("Swaps from when this intent is chosen until it fires.")]
        [SerializeField] private int countdown = 3;

        [Min(1)]
        [Tooltip("Relative chance of being picked. An intent with weight 2 is picked twice as often as one with weight 1.")]
        [SerializeField] private int weight = 1;

        [SerializeReference, Polymorphic] private List<IntentAction> actions = new List<IntentAction>();

        public int Countdown => Mathf.Max(1, countdown);
        public int Weight => Mathf.Max(1, weight);
        public IReadOnlyList<IntentAction> Actions => actions;

        /// <summary>The label, or the actions' descriptions joined, e.g. "Attack 6, Block 4".</summary>
        public string Describe()
        {
            if (!string.IsNullOrEmpty(label)) return label;

            StringBuilder builder = new StringBuilder();
            foreach (IntentAction action in actions)
            {
                if (action == null) continue;
                if (builder.Length > 0) builder.Append(", ");
                builder.Append(action.Describe());
            }
            return builder.Length > 0 ? builder.ToString() : "Wait";
        }
    }
}
