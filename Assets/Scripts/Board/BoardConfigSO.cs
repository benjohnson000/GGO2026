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

        [Tooltip("Combo tiles made by special match shapes. Each match makes at most one combo, from its highest-priority shape " +
                 "(5-line, cross, 4-line, square). Shapes with no rule make no combo.")]
        [SerializeField] private List<ComboRule> comboRules = new List<ComboRule>();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<TileDataSO> FillerTiles => fillerTiles;
        public IReadOnlyList<ComboRule> ComboRules => comboRules;

        private void OnValidate()
        {
            // Names each list element after its combo. Renaming a tile asset updates it the next time this config is edited.
            foreach (ComboRule rule in comboRules)
            {
                if (rule != null) rule.RefreshLabel();
            }
        }
    }
}
