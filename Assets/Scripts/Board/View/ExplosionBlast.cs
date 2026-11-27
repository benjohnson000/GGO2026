using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// A bomb's explosion: a circle sprite that grows quickly from the bomb's cell to cover its blast square, fading
    /// out as it goes, then destroys itself. Built in code, no prefab. It is only a visual: <see cref="BoardView"/>
    /// times the pops.
    /// </summary>
    public class ExplosionBlast : MonoBehaviour
    {
        // Above tiles, which use sorting orders 0 to 2.
        private const int SortingOrder = 3;

        private SpriteRenderer sprite;
        private Color colour;
        private float fullScale;
        private float duration;
        private float elapsed;

        /// <summary>
        /// Spawns an explosion under <paramref name="parent"/> at <paramref name="localPosition"/> that grows to
        /// <paramref name="diameter"/> (in the parent's local units) over <paramref name="duration"/> seconds.
        /// <paramref name="age"/> is how far into it to start, to stay in step with the pops.
        /// </summary>
        public static ExplosionBlast Create(Sprite circle, Transform parent, Vector3 localPosition, float diameter,
            float duration, Color colour, float age)
        {
            GameObject go = new GameObject("Explosion");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            ExplosionBlast blast = go.AddComponent<ExplosionBlast>();
            blast.sprite = go.AddComponent<SpriteRenderer>();
            blast.sprite.sprite = circle;
            blast.sprite.sortingOrder = SortingOrder;

            float spriteSize = Mathf.Max(circle.bounds.size.x, circle.bounds.size.y);
            blast.fullScale = spriteSize > 0f ? diameter / spriteSize : 0f;
            blast.duration = duration;
            blast.colour = colour;
            blast.elapsed = age;
            blast.Show();
            return blast;
        }

        /// <summary>Its size, as a fraction of full size, <paramref name="t"/> of the way through (0 to 1).</summary>
        public static float GetGrowth(float t)
        {
            // Ease-out cubic: bursts out fast, then settles at full size as it fades.
            float rest = 1f - Mathf.Clamp01(t);
            return 1f - rest * rest * rest;
        }

        /// <summary>The inverse of <see cref="GetGrowth"/>: how far through (0 to 1) it reaches <paramref name="fraction"/> of full size.</summary>
        public static float GetTimeToReach(float fraction)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(fraction), 1f / 3f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            Show();
        }

        private void Show()
        {
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            transform.localScale = Vector3.one * (fullScale * GetGrowth(t));

            Color faded = colour;
            faded.a *= 1f - t;
            sprite.color = faded;

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
