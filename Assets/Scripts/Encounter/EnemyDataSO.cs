using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// An enemy: name and health (from <see cref="CombatantDataSO"/>), the intents it picks from at random, and the
    /// rewards offered when it is defeated.
    /// </summary>
    [CreateAssetMenu(menuName = "GGO2026/Enemy", fileName = "new Enemy")]
    public class EnemyDataSO : CombatantDataSO
    {
        [Tooltip("Picked at random, by weight, each time the previous intent fires. With none, the enemy never acts.")]
        [SerializeField] private List<EnemyIntent> intents = new List<EnemyIntent>();

        [Tooltip("Shown on the reward screen after this enemy is defeated, in this order.")]
        [SerializeReference, Polymorphic] private List<RewardSource> rewards = new List<RewardSource> { new CoinRewardSource(10) };

        public IReadOnlyList<EnemyIntent> Intents => intents;

        public IReadOnlyList<RewardSource> Rewards => rewards;

        /// <summary>Rolls a fresh set of rewards for one win. Sources that offer nothing are left out.</summary>
        public List<Reward> CreateRewards(System.Random rng)
        {
            List<Reward> result = new List<Reward>();
            foreach (RewardSource source in rewards)
            {
                Reward reward = source?.Create(rng);
                if (reward != null) result.Add(reward);
            }
            return result;
        }
    }
}
