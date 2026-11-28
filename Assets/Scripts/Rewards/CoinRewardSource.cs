using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>A pile of coins, rolled between <see cref="min"/> and <see cref="max"/> (inclusive).</summary>
    [Serializable]
    public class CoinRewardSource : RewardSource
    {
        [Min(0)]
        [SerializeField] private int min = 10;

        [Min(0)]
        [Tooltip("Set equal to Min for a fixed amount.")]
        [SerializeField] private int max = 10;

        [Tooltip("Optional icon for the reward row.")]
        [SerializeField] private Sprite icon;

        public CoinRewardSource() { }

        public CoinRewardSource(int amount)
        {
            min = amount;
            max = amount;
        }

        public override Reward Create(System.Random rng)
        {
            int low = Mathf.Min(min, max);
            int high = Mathf.Max(min, max);
            int amount = rng.Next(low, high + 1);
            return amount > 0 ? new CoinReward(amount, icon) : null;
        }
    }
}
