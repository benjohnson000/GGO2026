using UnityEngine;

namespace ggo2026
{
    /// <summary>Adds coins to the run's purse and flies them from the reward row to the coin counter.</summary>
    public class CoinReward : Reward
    {
        public int Amount { get; }

        public override string Title => Amount == 1 ? "1 Coin" : $"{Amount} Coins";

        public CoinReward(int amount, Sprite icon) : base(icon)
        {
            Amount = Mathf.Max(0, amount);
        }

        protected override void OnClaim(RewardClaimContext context)
        {
            CoinsView coins = context.Coins;
            if (coins == null || coins.Purse == null)
            {
                Debug.LogWarning("CoinReward: no CoinsView found (is the UI scene loaded?), so the coins can't be given.");
                return;
            }

            coins.Purse.Gain(Amount);
            coins.ShowGain(Amount, context.Origin, Color.white);
            MarkClaimed();
        }
    }
}
