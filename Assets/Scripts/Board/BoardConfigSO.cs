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

        [Tooltip("Colours a filler can roll when the pool is empty. Nothing selected falls back to every colour.")]
        [SerializeField] private TileColour fillerColours = TileColourUtils.All;

        public int Width => width;
        public int Height => height;
        public TileColour FillerColours => TileColourUtils.Sanitize(fillerColours);
    }
}
