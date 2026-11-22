using UnityEngine;

namespace benjohnson
{
    /// <summary>Colours a spawned effect made from white or grey art.</summary>
    public static class RendererTint
    {
        /// <summary>
        /// Multiplies the RGB of every sprite, trail and single-colour particle system under <paramref name="root"/>
        /// by <paramref name="tint"/>. Their own alpha is kept, so fades survive. Call once, on a fresh instance.
        /// </summary>
        public static void Multiply(GameObject root, Color tint)
        {
            tint.a = 1f;

            foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>()) sprite.color *= tint;

            foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>())
            {
                // Every key, as startColor and endColor only cover the ends of the gradient.
                Gradient gradient = trail.colorGradient;
                GradientColorKey[] keys = gradient.colorKeys;
                for (int i = 0; i < keys.Length; i++) keys[i].color *= tint;
                gradient.colorKeys = keys;
                trail.colorGradient = gradient;
            }

            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = particles.main;
                if (main.startColor.mode == ParticleSystemGradientMode.Color) main.startColor = main.startColor.color * tint;
            }
        }
    }
}
