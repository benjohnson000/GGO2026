using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Adds stacks of a status to the player or to the enemy itself.</summary>
    [Serializable]
    public class ApplyStatusIntent : IntentAction
    {
        [Tooltip("The status asset to apply.")]
        [SerializeField] private StatusDataSO status;

        [Tooltip("Stacks to add. Negative removes stacks.")]
        [SerializeField] private int stacks = 3;

        [Tooltip("Opponent is the player; Owner is the enemy itself.")]
        [SerializeField] private TileEffectTarget target = TileEffectTarget.Opponent;

        public override CombatAction CreateAction(Combat combat, Combatant enemy)
        {
            if (status == null)
            {
                Debug.LogError("ApplyStatusIntent has no status assigned.");
                return null;
            }

            Combatant combatant = target == TileEffectTarget.Owner ? enemy : combat.GetOpponent(enemy);
            return new ApplyStatusAction(combatant, status, stacks);
        }

        public override string Describe()
        {
            string statusName = status != null ? status.DisplayName : "?";
            return target == TileEffectTarget.Owner ? $"Gain {statusName} {stacks}" : $"{statusName} {stacks}";
        }
    }
}
