using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Rules data for one board. Grid size is encounter data, so encounters will supply their own config later.
    /// </summary>
    [CreateAssetMenu(menuName = "GGO2026/Board Config", fileName = "new BoardConfig")]
    public class BoardConfigSO : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int width = 6;

        [Min(1)]
        [SerializeField] private int height = 6;

        [Tooltip("Tiles a filler can be when the pool is empty, picked at random. Use tiles with no effect, ideally one per colour. " +
                 "If empty, fillers fall back to plain tiles of a random colour.")]
        [SerializeField] private List<TileDataSO> fillerTiles = new List<TileDataSO>();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<TileDataSO> FillerTiles => fillerTiles;
    }
}
