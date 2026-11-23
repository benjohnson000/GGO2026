using System;
using benjohnson;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// A tile's effect in flight (damage, block, heal, status, a laser shot…): put this on a prefab (sprite, particle
    /// system, trail…) and reference it from a <see cref="ProjectileStyleSO"/>, which sets how it flies, its impact
    /// and sounds. It flies along an arc from the tile that fired to its target, then calls back so the change is
    /// shown, stops emitting and destroys itself once its trails and particles have faded.
    /// </summary>
    public class DamageProjectile : MonoBehaviour
    {
        [Tooltip("Seconds kept alive after impact so this prefab's trails and particles fade out instead of vanishing.")]
        [Min(0f)]
        [SerializeField] private float lingerTime = 0.5f;

        private ProjectileStyleSO style;
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
        /// flown as <paramref name="flightStyle"/> describes, in <paramref name="colour"/>, the colour of the tile that fired.
        /// <paramref name="landedCallback"/> runs once, on impact, or if this object is destroyed first.
        /// </summary>
        public void Launch(ProjectileStyleSO flightStyle, Vector3 start, Transform targetPoint, Color colour, Action landedCallback)
        {
            Launch(flightStyle, start, targetPoint, targetPoint != null ? targetPoint.position : start, colour, landedCallback);
        }

        /// <summary>Starts the flight from <paramref name="start"/> to a fixed world point, e.g. a board cell. <paramref name="landedCallback"/> is optional.</summary>
        public void Launch(ProjectileStyleSO flightStyle, Vector3 start, Vector3 end, Color colour, Action landedCallback = null)
        {
            Launch(flightStyle, start, null, end, colour, landedCallback);
        }

        private void Launch(ProjectileStyleSO flightStyle, Vector3 start, Transform targetPoint, Vector3 end, Color colour, Action landedCallback)
        {
            style = flightStyle;
            tileColour = colour;
            if (style.TintWithTileColour) RendererTint.Multiply(gameObject, tileColour);

            from = start;
            target = targetPoint;
            lastTargetPosition = end;
            onLanded = landedCallback;
            landed = false;
            elapsed = 0f;
            transform.position = start;

            // Bow the path up and a random amount to one side, so a run of hits fans out.
            Vector3 direction = end - start;
            Vector3 side = Vector3.Cross(direction, Vector3.forward).normalized;
            control = (start + end) * 0.5f + Vector3.up * style.ArcHeight + side * UnityEngine.Random.Range(-style.ArcSpread, style.ArcSpread);

            if (style.FaceDirection) Face(Bezier(from, control, lastTargetPosition, 0.01f) - start);
            PlaySound(style.LaunchSound);
        }

        private void Update()
        {
            if (landed) return;

            if (target != null) lastTargetPosition = target.position;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / style.Duration);
            Vector3 previous = transform.position;
            transform.position = Bezier(from, control, lastTargetPosition, Mathf.Pow(t, style.EasePower));
            if (style.FaceDirection) Face(transform.position - previous);

            if (t >= 1f) Land();
        }

        /// <summary>Turns +X toward <paramref name="direction"/> in the XY plane. Ignores zero moves.</summary>
        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < 1e-8f) return;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void Land()
        {
            landed = true;
            if (style.ImpactPrefab != null)
            {
                GameObject impact = Instantiate(style.ImpactPrefab, transform.position, Quaternion.identity);
                if (style.TintWithTileColour) RendererTint.Multiply(impact, tileColour);
            }
            PlaySound(style.ImpactSound);
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

        private static void PlaySound(SoundProfileSO sound)
        {
            if (sound == null || !sound.HasClips) return;

            SFXManager sfx = SFXManager.instance;
            if (sfx != null) sfx.PlaySound(sound);
        }

        private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
