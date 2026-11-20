using System.Collections.Generic;
using UnityEngine;

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
        [Tooltip("Sprite for filler tiles, and for deck tiles with no shape assigned.")]
        [SerializeField] private Sprite fillerSprite;

        [Tooltip("Optional sprite material. Leave empty to use the renderer's default.")]
        [SerializeField] private Material tileMaterial;

        [Tooltip("Display colour per tile colour. Multi-colour tiles currently show their lowest colour.")]
        [SerializeField] private List<ColourSwatch> palette = new List<ColourSwatch>();

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
        public Sprite FillerSprite => fillerSprite;
        public Material TileMaterial => tileMaterial;
        public float SwapDuration => swapDuration;
        public float PopDuration => popDuration;
        public float FallSpeed => fallSpeed;
        public float ShuffleDuration => shuffleDuration;
        public float DragThreshold => dragThreshold;

        /// <summary>Display colour for a tile's colours (its lowest colour), or white if the palette has no entry.</summary>
        public Color GetColor(TileColour colours)
        {
            TileColour colour = TileColourUtils.Lowest(colours);
            foreach (ColourSwatch swatch in palette)
            {
                if (swatch != null && swatch.Colour == colour) return swatch.Color;
            }
            return Color.white;
        }
    }
}
