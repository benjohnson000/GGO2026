using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Gives the player coins. The board adds them to its <see cref="Board.Coins"/>; the board view flies them from the
    /// tile to the coin counter in this effect's <see cref="TileEffect.Projectile"/> style (or the counter's default).
    /// Does nothing when the board has no purse.
    /// </summary>
    [Serializable]
    public class GainCoinsEffect : TileEffect
    {
        [Min(1)]
        [SerializeField] private int amount = 1;

        public override TileEffectActivatedEvent Activate(TileEffectContext context)
        {
            if (context.Coins == null || amount <= 0) return null;
            return new CoinsGainedEvent(this, context.Tile, context.Cell, amount);
        }
    }
}
