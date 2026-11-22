using System;
using benjohnson;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// A tile's effect in flight (damage, block, heal, status…): put this on a prefab (sprite, particle system,
    /// trail…) and assign it to <see cref="EncounterView"/>. It flies along an arc from the tile that fired to the
    /// target's <see cref="CombatantView.HitPoint"/>, then calls back so the change is shown, stops emitting and
    /// destroys itself once its trails and particles have faded.
    /// </summary>
    public class DamageProjectile : MonoBehaviour
    {
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

        [Tooltip("Seconds kept alive after impact so trails and particles fade out instead of vanishing.")]
        [Min(0f)]
        [SerializeField] private float lingerTime = 0.5f;

        [Tooltip("Optional. Spawned at the impact point, e.g. a burst particle prefab. Destroy it from its own settings (e.g. Stop Action: Destroy).")]
        [SerializeField] private GameObject impactPrefab;

        [Tooltip("Multiply sprites, trails and single-colour particles, and the impact's, by the colour of the tile that fired. Use white or grey art.")]
        [SerializeField] private bool tintWithTileColour = true;

        private Color tileColour;
        private Vector3 from;
        private Vector3 control;
        private Transform target;
        private Vector3 lastTargetPosition;
        private float elapsed;
        private Action onLanded;
        private bool landed = true;

        /// <summary>
        /// Starts the flight from <paramref name="start"/> to <paramref name="targetPoint"/> (followed if it moves),
        /// in <paramref name="colour"/>, the colour of the tile that fired.
        /// <paramref name="landedCallback"/> runs once, on impact, or if this object is destroyed first.
        /// </summary>
        public void Launch(Vector3 start, Transform targetPoint, Color colour, Action landedCallback)
        {
            tileColour = colour;
            if (tintWithTileColour) RendererTint.Multiply(gameObject, tileColour);

            from = start;
            target = targetPoint;
            lastTargetPosition = targetPoint != null ? targetPoint.position : start;
            onLanded = landedCallback;
            landed = false;
            elapsed = 0f;
            transform.position = start;

            // Bow the path up and a random amount to one side, so a run of hits fans out.
            Vector3 to = lastTargetPosition;
            Vector3 direction = to - start;
            Vector3 side = Vector3.Cross(direction, Vector3.forward).normalized;
            control = (start + to) * 0.5f + Vector3.up * arcHeight + side * UnityEngine.Random.Range(-arcSpread, arcSpread);
        }

        private void Update()
        {
            if (landed) return;

            if (target != null) lastTargetPosition = target.position;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Bezier(from, control, lastTargetPosition, Mathf.Pow(t, easePower));

            if (t >= 1f) Land();
        }

        private void Land()
        {
            landed = true;
            if (impactPrefab != null)
            {
                GameObject impact = Instantiate(impactPrefab, transform.position, Quaternion.identity);
                if (tintWithTileColour) RendererTint.Multiply(impact, tileColour);
            }
            StopVisuals();
            Notify();
            Destroy(gameObject, lingerTime);
        }

        private void OnDestroy()
        {
            // A projectile removed mid-flight still calls back, so playback never stalls.
            Notify();
        }

        private void Notify()
        {
            Action callback = onLanded;
            onLanded = null;
            callback?.Invoke();
        }

        /// <summary>Hides sprites and stops emitting, leaving existing trails and particles to fade.</summary>
        private void StopVisuals()
        {
            foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.enabled = false;
            foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>()) trail.emitting = false;
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>()) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
