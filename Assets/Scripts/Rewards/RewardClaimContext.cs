using UnityEngine;

namespace ggo2026
{
    /// <summary>What a <see cref="Reward"/> may need when it is claimed. Any of the references may be null.</summary>
    public class RewardClaimContext
    {
        /// <summary>The run's coin counter (and purse).</summary>
        public CoinsView Coins { get; }

        /// <summary>The screen that lets the player pick one tile of several. Not built yet, so usually null.</summary>
        public ITileChoicePresenter TileChoice { get; }

        /// <summary>World position of the claimed row, e.g. where coins fly from.</summary>
        public Vector3 Origin { get; }

        public RewardClaimContext(CoinsView coins, ITileChoicePresenter tileChoice, Vector3 origin)
        {
            Coins = coins;
            TileChoice = tileChoice;
            Origin = origin;
        }
    }
}
