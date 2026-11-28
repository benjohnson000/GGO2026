using System;
using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>
    /// A screen that offers several tiles and lets the player pick one (the pick-3 screen). Implement this on its
    /// MonoBehaviour and assign it to the <see cref="RewardScreen"/>'s Tile Choice Screen field.
    /// </summary>
    public interface ITileChoicePresenter
    {
        /// <summary>
        /// Shows <paramref name="options"/>. Calls <paramref name="onPicked"/> once with the chosen tile, or with null
        /// if the player closes the screen without picking (the reward then stays in the list).
        /// </summary>
        void Show(IReadOnlyList<TileDataSO> options, Action<TileDataSO> onPicked);
    }
}
