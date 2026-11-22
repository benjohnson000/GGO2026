using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>The tile's owner heals.</summary>
    [Serializable]
    public class HealEffect : CombatTileEffect
    {
        [Min(0)]
        [SerializeField] private int amount = 2;

        protected override CombatAction CreateAction(TileEffectContext context)
        {
            return new HealAction(context.Owner, amount);
        }
    }
}
