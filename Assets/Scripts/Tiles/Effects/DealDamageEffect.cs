using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Deals damage from the tile's owner to the opponent, tagged with the tile's colour (for weaknesses and resistances).</summary>
    [Serializable]
    public class DealDamageEffect : CombatTileEffect
    {
        [Min(0)]
        [SerializeField] private int amount = 3;

        protected override CombatAction CreateAction(TileEffectContext context)
        {
            return new DealDamageAction(context.Owner, context.Opponent, amount, context.Tile.Colour);
        }
    }
}
