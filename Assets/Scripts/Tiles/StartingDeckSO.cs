using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// The tiles a run starts with. Stands in for the run's deck until run state exists.
    /// </summary>
    [CreateAssetMenu(menuName = "GGO2026/Starting Deck", fileName = "new StartingDeck")]
    public class StartingDeckSO : ScriptableObject
    {
        [SerializeField] private List<DeckEntry> entries = new List<DeckEntry>();

        /// <summary>Adds a fresh <see cref="Tile"/> to the pool for every copy listed in this deck.</summary>
        public void AddTo(TilePool pool)
        {
            if (pool == null) return;

            foreach (DeckEntry entry in entries)
            {
                if (entry == null || entry.TileData == null) continue;

                // Colourless tiles are allowed (e.g. colour bombs); any other colour must be a real one.
                TileColour colour = entry.TileData.Colour;
                if (colour != TileColour.None && !TileColourUtils.IsMatchable(colour))
                {
                    Debug.LogError($"{name}: tile '{entry.TileData.name}' has invalid colour '{colour}'. Skipping it.", this);
                    continue;
                }

                for (int i = 0; i < entry.Count; i++) pool.Add(new Tile(entry.TileData));
            }
        }
    }
}
