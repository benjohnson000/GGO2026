using UnityEngine;
using UnityEngine.Serialization;

namespace ggo2026
{
    /// <summary>Presentation and feel tuning for the board: layout, palette, animation timing and drag input.</summary>
    [CreateAssetMenu(menuName = "GGO2026/Board View Settings", fileName = "new BoardViewSettings")]
    public class BoardViewSettingsSO : ScriptableObject
    {
        [Header("Layout")]
        [Tooltip("World units between cell centres.")]
        [Min(0.01f)]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Tile sprite scale relative to the cell, to leave a gap between tiles.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float tileScale = 0.85f;

        [Header("Visuals")]
        [Tooltip("Sprite for tiles with no shape assigned, including plain fillers when the board config has no filler tiles.")]
        [FormerlySerializedAs("fillerSprite")]
        [SerializeField] private Sprite fallbackSprite;

        [Tooltip("Sprite material for tiles. Use a material with the GGO2026/Tile Outline shader, or the outline is not drawn.")]
        [SerializeField] private Material tileMaterial;

        [Tooltip("Tile colours and outline.")]
        [SerializeField] private PaletteSO tilePalette;

        [Header("Timing")]
        [Min(0.01f)]
        [SerializeField] private float swapDuration = 0.15f;

        [Min(0.01f)]
        [SerializeField] private float popDuration = 0.15f;

        [Tooltip("Fall speed in cells per second.")]
        [Min(0.1f)]
        [SerializeField] private float fallSpeed = 12f;

        [Min(0.01f)]
        [SerializeField] private float shuffleDuration = 0.4f;

        [Header("Input")]
        [Tooltip("How far the pointer must drag, as a fraction of a cell, before a swap triggers.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float dragThreshold = 0.35f;

        public float CellSize => cellSize;
        public float TileScale => tileScale;
        public Sprite FallbackSprite => fallbackSprite;
        public Material TileMaterial => tileMaterial;
        public PaletteSO Palette => tilePalette;
        public float SwapDuration => swapDuration;
        public float PopDuration => popDuration;
        public float FallSpeed => fallSpeed;
        public float ShuffleDuration => shuffleDuration;
        public float DragThreshold => dragThreshold;
    }
}
