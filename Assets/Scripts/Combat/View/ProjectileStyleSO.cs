using benjohnson;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// How one kind of projectile looks and flies: which <see cref="DamageProjectile"/> prefab, its flight, impact and
    /// sounds. A <see cref="TileEffect"/> picks one with <see cref="TileEffect.Projectile"/>, so effects can share a
    /// prefab (e.g. one laser) with different tuning, or use different prefabs. Effects without one use the default
    /// on <see cref="EncounterView"/> (combat hits) or <see cref="BoardViewSettingsSO"/> (laser shots).
    /// </summary>
    [CreateAssetMenu(menuName = "GGO2026/Projectile Style", fileName = "new ProjectileStyle")]
    public class ProjectileStyleSO : ScriptableObject
    {
        [Tooltip("The projectile spawned. Without it nothing flies and hits land at once.")]
        [SerializeField] private DamageProjectile prefab;

        [Header("Flight")]
        [Tooltip("Seconds from launch to impact.")]
        [Min(0.01f)]
        [SerializeField] private float duration = 0.35f;

        [Tooltip("How far the path bows upward at its middle, in world units. 0 = straight line.")]
        [SerializeField] private float arcHeight = 1f;

        [Tooltip("Random sideways bow per projectile, in world units, so several in a row don't overlap.")]
        [Min(0f)]
        [SerializeField] private float arcSpread = 0.75f;

        [Tooltip("Speed-up toward the target. 1 = constant speed, 2 = eases in (accelerates into the hit).")]
        [Min(1f)]
        [SerializeField] private float easePower = 2f;

        [Tooltip("Turn the projectile to face where it is heading. Draw the prefab pointing along +X. Use for lasers, arrows and the like.")]
        [SerializeField] private bool faceDirection;

        [Header("Visuals")]
        [Tooltip("Optional. Spawned at the impact point, e.g. a burst particle prefab. Destroy it from its own settings (e.g. Stop Action: Destroy).")]
        [SerializeField] private GameObject impactPrefab;

        [Tooltip("Multiply sprites, trails and single-colour particles, and the impact's, by the colour of the tile that fired. Use white or grey art.")]
        [SerializeField] private bool tintWithTileColour = true;

        [Header("Audio")]
        [Tooltip("Optional. Played as each projectile launches.")]
        [SerializeField] private SoundProfileSO launchSound;

        [Tooltip("Optional. Played as each projectile lands.")]
        [SerializeField] private SoundProfileSO impactSound;

        public DamageProjectile Prefab => prefab;
        public float Duration => duration;
        public float ArcHeight => arcHeight;
        public float ArcSpread => arcSpread;
        public float EasePower => easePower;
        public bool FaceDirection => faceDirection;
        public GameObject ImpactPrefab => impactPrefab;
        public bool TintWithTileColour => tintWithTileColour;
        public SoundProfileSO LaunchSound => launchSound;
        public SoundProfileSO ImpactSound => impactSound;

        /// <summary>Seconds from launch to impact, or 0 when there is no prefab (hits land at once).</summary>
        public float FlightTime => prefab != null ? duration : 0f;

        /// <summary>
        /// Spawns the prefab at <paramref name="start"/> under <paramref name="parent"/> (or the active scene) and does
        /// not launch it yet. Returns null without a prefab.
        /// </summary>
        public DamageProjectile Spawn(Vector3 start, Transform parent = null)
        {
            if (prefab == null) return null;
            return parent != null ? Instantiate(prefab, start, Quaternion.identity, parent) : Instantiate(prefab, start, Quaternion.identity);
        }
    }
}
