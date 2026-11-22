using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Deals damage to the player.</summary>
    [Serializable]
    public class AttackIntent : IntentAction
    {
        [Min(0)]
        [SerializeField] private int amount = 6;

        public override CombatAction CreateAction(Combat combat, Combatant enemy)
        {
            return new DealDamageAction(enemy, combat.GetOpponent(enemy), amount);
        }

        public override string Describe() => $"Attack {amount}";
    }
}
