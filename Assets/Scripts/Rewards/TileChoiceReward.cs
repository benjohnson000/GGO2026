using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Opens the tile choice screen with a few tiles; collected once the player picks one. Picking does not add the
    /// tile to the deck yet, because there is no run deck (each fight builds its pool from a StartingDeckSO).
    /// </summary>
    public class TileChoiceReward : Reward
    {
        private readonly List<TileDataSO> options;

        public IReadOnlyList<TileDataSO> Options => options;

        /// <summary>The tile the player chose, or null until then.</summary>
        public TileDataSO PickedTile { get; private set; }

        public override string Title => "Choose a Tile";

        public TileChoiceReward(List<TileDataSO> options, Sprite icon) : base(icon)
        {
            this.options = options ?? new List<TileDataSO>();
        }

        protected override void OnClaim(RewardClaimContext context)
        {
            if (context.TileChoice == null)
            {
                Debug.LogWarning("TileChoiceReward: no tile choice screen is assigned on the RewardScreen, so this reward can't be collected yet.");
                return;
            }

            context.TileChoice.Show(options, picked =>
            {
                if (picked == null) return;
                PickedTile = picked;
                MarkClaimed();
            });
        }
    }
}
