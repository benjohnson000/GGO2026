using UnityEngine;

namespace ggo2026
{
    /// <summary>A <see cref="GainCoinsEffect"/> fired. The board adds <see cref="Amount"/> to <see cref="Board.Coins"/> as it logs this.</summary>
    public class CoinsGainedEvent : TileEffectActivatedEvent
    {
        public int Amount { get; }

        public CoinsGainedEvent(TileEffect effect, Tile tile, Vector2Int cell, int amount)
            : base(effect, tile, cell)
        {
            Amount = amount;
        }
    }
}
