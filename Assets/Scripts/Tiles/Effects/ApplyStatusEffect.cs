using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Adds stacks of a status (e.g. poison) to the opponent or to the tile's owner.</summary>
    [Serializable]
    public class ApplyStatusEffect : CombatTileEffect
    {
        [Tooltip("The status asset to apply.")]
        [SerializeField] private StatusDataSO status;

        [Tooltip("Stacks to add. Negative removes stacks.")]
        [SerializeField] private int stacks = 3;

        [SerializeField] private TileEffectTarget target = TileEffectTarget.Opponent;

        protected override CombatAction CreateAction(TileEffectContext context)
        {
            if (status == null)
            {
                // Data is set: only tiles with data have effects.
                Debug.LogError($"{context.Tile.Data.name}: ApplyStatusEffect has no status assigned.", context.Tile.Data);
                return null;
            }

            Combatant combatant = target == TileEffectTarget.Owner ? context.Owner : context.Opponent;
            return new ApplyStatusAction(combatant, status, stacks);
        }
    }
}
