using benjohnson;
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

        [Tooltip("Seconds between one tile starting to pop and the next, so cleared tiles trigger one at a time. " +
                 "A tile's combat effects play as it disappears. Tiles hit by a line clear follow its blast instead. 0 pops everything at once.")]
        [Min(0f)]
        [SerializeField] private float popInterval = 0.05f;

        [Tooltip("Fall speed in cells per second.")]
        [Min(0.1f)]
        [SerializeField] private float fallSpeed = 12f;

        [Min(0.01f)]
        [SerializeField] private float shuffleDuration = 0.4f;

        [Header("Line Blast")]
        [Tooltip("Optional. One arm of a striped tile's blast, drawn pointing along +X. Without it, pops still follow the blast's timing.")]
        [SerializeField] private LineBlast lineBlastPrefab;

        [Tooltip("Blast speed in cells per second. A tile starts to pop when the blast reaches its centre.")]
        [Min(0.1f)]
        [SerializeField] private float lineBlastSpeed = 20f;

        [Tooltip("Seconds the blast takes to fade after reaching the board's edge.")]
        [Min(0f)]
        [SerializeField] private float lineBlastFadeTime = 0.15f;

        [Header("Explosion")]
        [Tooltip("Optional. Circle drawn when a bomb explodes, tinted with the bomb's colour. It grows to cover the blast square " +
                 "and fades out. Use white art. Without it, bombs have no visual of their own.")]
        [SerializeField] private Sprite explosionSprite;

        [Tooltip("Seconds the explosion circle takes to grow to full size and fade out.")]
        [Min(0.01f)]
        [SerializeField] private float explosionDuration = 0.25f;

        [Header("Laser")]
        [Tooltip("Optional. Projectile flown from a laser tile to each target when the laser effect has no Projectile of its own; " +
                 "the target starts to pop when it lands. Without either, targets pop as soon as the laser fires.")]
        [SerializeField] private ProjectileStyleSO defaultLaserProjectile;

        [Header("Audio")]
        [Tooltip("Optional. Played once per tile as it starts to pop. Use its Min Interval to thin out very fast clears.")]
        [SerializeField] private SoundProfileSO popSound;

        [Tooltip("Pitch added for each successive pop in one clear, so a chain rises. 0 = flat.")]
        [Min(0f)]
        [SerializeField] private float popPitchStep = 0.04f;

        [Tooltip("Highest pitch multiplier the rising pops reach.")]
        [Min(1f)]
        [SerializeField] private float popPitchMax = 1.6f;

        [Tooltip("Optional. Played once per line blast, as it launches.")]
        [SerializeField] private SoundProfileSO lineBlastSound;

        [Tooltip("Optional. Played once per bomb explosion, as its circle bursts out.")]
        [SerializeField] private SoundProfileSO explosionSound;

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
        public float PopInterval => popInterval;
        public float FallSpeed => fallSpeed;
        public float ShuffleDuration => shuffleDuration;
        public float DragThreshold => dragThreshold;
        public SoundProfileSO PopSound => popSound;
        public float PopPitchStep => popPitchStep;
        public float PopPitchMax => popPitchMax;
        public LineBlast LineBlastPrefab => lineBlastPrefab;
        public float LineBlastSpeed => lineBlastSpeed;
        public float LineBlastFadeTime => lineBlastFadeTime;
        public SoundProfileSO LineBlastSound => lineBlastSound;
        public ProjectileStyleSO DefaultLaserProjectile => defaultLaserProjectile;
        public Sprite ExplosionSprite => explosionSprite;
        public float ExplosionDuration => explosionDuration;
        public SoundProfileSO ExplosionSound => explosionSound;
    }
}
