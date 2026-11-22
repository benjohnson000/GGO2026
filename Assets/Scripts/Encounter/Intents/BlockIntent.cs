using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>The enemy gains block. It lasts until the start of the enemy's next turn.</summary>
    [Serializable]
    public class BlockIntent : IntentAction
    {
        [Min(0)]
        [SerializeField] private int amount = 5;

        public override CombatAction CreateAction(Combat combat, Combatant enemy)
        {
            return new GainBlockAction(enemy, amount);
        }

        public override string Describe() => $"Block {amount}";
    }
}
