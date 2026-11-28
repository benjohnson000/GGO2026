using System;
using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>A choice of <see cref="choices"/> different tiles, picked at random from <see cref="options"/>.</summary>
    [Serializable]
    public class TileChoiceRewardSource : RewardSource
    {
        [Min(1)]
        [Tooltip("How many tiles the choice screen offers.")]
        [SerializeField] private int choices = 3;

        [Tooltip("Tiles that can be offered. Each appears at most once per choice.")]
        [SerializeField] private List<TileDataSO> options = new List<TileDataSO>();

        [Tooltip("Optional icon for the reward row.")]
        [SerializeField] private Sprite icon;

        public override Reward Create(System.Random rng)
        {
            List<TileDataSO> pool = new List<TileDataSO>();
            foreach (TileDataSO tile in options)
            {
                if (tile != null && !pool.Contains(tile)) pool.Add(tile);
            }
            if (pool.Count == 0)
            {
                Debug.LogWarning("TileChoiceRewardSource: no tiles in Options, so no tile choice is offered.");
                return null;
            }

            // Partial Fisher-Yates: the first `count` entries end up a random selection.
            int count = Mathf.Min(choices, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int j = rng.Next(i, pool.Count);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            pool.RemoveRange(count, pool.Count - count);
            return new TileChoiceReward(pool, icon);
        }
    }
}
