using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// One thing the player can collect from the <see cref="RewardScreen"/> after a win: coins, a tile choice, and later
    /// charms. Made fresh for each win by a <see cref="RewardSource"/>, so it may hold rolled values (e.g. the coin
    /// amount or the tiles on offer). A reward is collected once; some (a tile choice) open another screen first and
    /// are only collected when that choice is made.
    /// </summary>
    public abstract class Reward
    {
        /// <summary>Raised once, when the reward has been given.</summary>
        public event Action<Reward> Claimed;

        /// <summary>Optional icon for the list row.</summary>
        public Sprite Icon { get; }

        public bool IsClaimed { get; private set; }

        /// <summary>Text for the list row, e.g. "10 Coins".</summary>
        public abstract string Title { get; }

        protected Reward(Sprite icon)
        {
            Icon = icon;
        }

        /// <summary>
        /// Gives the reward, or starts giving it (e.g. opens a choice). Implementations call <see cref="MarkClaimed"/>
        /// once it has actually been given, which may be later, or never if the player backs out of a choice.
        /// </summary>
        public void Claim(RewardClaimContext context)
        {
            if (IsClaimed) return;
            OnClaim(context);
        }

        protected abstract void OnClaim(RewardClaimContext context);

        protected void MarkClaimed()
        {
            if (IsClaimed) return;
            IsClaimed = true;
            Claimed?.Invoke(this);
        }
    }
}
