using UnityEngine;

namespace ggo2026
{
    /// <summary>Colours and timings for a <see cref="HealthBarView"/>. Status colours come from each status's asset.</summary>
    [CreateAssetMenu(menuName = "GGO2026/Health Bar Settings", fileName = "Health Bar Settings")]
    public class HealthBarSettingsSO : ScriptableObject
    {
        [Header("Colours")]
        [Tooltip("The underfill: health lost since effects last finished resolving. Keep it duller than status colours, so it doesn't read as one.")]
        [SerializeField] private Color damageColour = new Color(0.64f, 0.52f, 0.49f);

        [Tooltip("Replaces the health fill's own colour while there is block.")]
        [SerializeField] private Color blockedHealthColour = new Color(0.36f, 0.63f, 0.87f);

        [Header("Underfill")]
        [Min(0f)]
        [Tooltip("Seconds the underfill waits once effects have resolved.")]
        [SerializeField] private float holdDuration = 0.3f;

        [Min(0f)]
        [Tooltip("Seconds the underfill then takes to reach the health fill.")]
        [SerializeField] private float drainDuration = 0.4f;

        [Header("Block Appear")]
        [Min(0f)]
        [Tooltip("Seconds the block icon takes to float down and fade in when block is gained from 0. The number shows after. 0 shows it straight away.")]
        [SerializeField] private float blockAppearDuration = 0.25f;

        [Tooltip("How far above its place the block icon starts, in canvas units (the icon is 2 tall).")]
        [SerializeField] private float blockAppearHeight = 0.5f;

        [Header("Block Break")]
        [Min(0f)]
        [Tooltip("Seconds the broken block icon's halves take to slide apart and fade out. 0 hides the block straight away.")]
        [SerializeField] private float blockBreakDuration = 0.35f;

        [Tooltip("How far each half slides sideways, in canvas units (the block icon is 2 wide).")]
        [SerializeField] private float blockBreakDistance = 0.75f;

        public Color DamageColour => damageColour;
        public Color BlockedHealthColour => blockedHealthColour;
        public float HoldDuration => holdDuration;
        public float DrainDuration => drainDuration;
        public float BlockAppearDuration => blockAppearDuration;
        public float BlockAppearHeight => blockAppearHeight;
        public float BlockBreakDuration => blockBreakDuration;
        public float BlockBreakDistance => blockBreakDistance;
    }
}
