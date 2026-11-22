using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>The tile's owner gains block.</summary>
    [Serializable]
    public class GainBlockEffect : CombatTileEffect
    {
        [Min(0)]
        [SerializeField] private int amount = 3;

        protected override CombatAction CreateAction(TileEffectContext context)
        {
            return new GainBlockAction(context.Owner, amount);
        }
    }
}
