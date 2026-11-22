using benjohnson;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// One arm of a striped tile's blast: put this on a prefab drawn pointing along +X and assign it to
    /// <see cref="BoardViewSettingsSO"/>. The board spawns one per direction. The head travels to the board's edge
    /// while the beam grows behind it, then everything fades and the object destroys itself.
    /// It is only a visual: <see cref="BoardView"/> times the pops.
    /// </summary>
    public class LineBlast : MonoBehaviour
    {
        [Tooltip("Optional. Child moved along the line, e.g. an arrow or flare with a trail or particles.")]
        [SerializeField] private Transform head;

        [Tooltip("Optional. Child stretched from the tile to the head. Needs a left-pivot sprite with Draw Mode Sliced or Tiled.")]
        [SerializeField] private SpriteRenderer beam;

        [Tooltip("Multiply sprites, trails and single-colour particles by the striped tile's colour. Use white or grey art.")]
        [SerializeField] private bool tintWithTileColour = true;

        private SpriteRenderer[] sprites;
        private Color[] spriteColours;
        private TrailRenderer[] trails;
        private ParticleSystem[] particleSystems;

        private Vector3 from;
        private Vector3 to;
        private float travelTime;
        private float fadeTime;
        private float elapsed;
        private bool launched;
        private bool arrived;

        /// <summary>
        /// Spawns an arm under <paramref name="parent"/>, whose local space <paramref name="origin"/>,
        /// <paramref name="direction"/>, <paramref name="distance"/> and <paramref name="speed"/> are in.
        /// <paramref name="age"/> is how far into its flight it starts, to stay in step with the pops it passes.
        /// </summary>
        public static LineBlast Create(LineBlast prefab, Transform parent, Vector3 origin, Vector2 direction,
            float distance, float speed, float fadeTime, Color colour, float age)
        {
            LineBlast blast = Instantiate(prefab, parent);
            blast.transform.localPosition = origin;
            blast.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            blast.from = parent.TransformPoint(origin);
            blast.to = parent.TransformPoint(origin + (Vector3)(direction * distance));
            blast.travelTime = distance / speed;
            blast.fadeTime = fadeTime;
            blast.Launch(colour, age);
            return blast;
        }

        private void Launch(Color colour, float age)
        {
            sprites = GetComponentsInChildren<SpriteRenderer>();
            trails = GetComponentsInChildren<TrailRenderer>();
            particleSystems = GetComponentsInChildren<ParticleSystem>();

            if (tintWithTileColour) RendererTint.Multiply(gameObject, colour);

            spriteColours = new Color[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) spriteColours[i] = sprites[i].color;

            if (beam != null && beam.drawMode == SpriteDrawMode.Simple)
            {
                Debug.LogWarning($"{name}: the beam's Draw Mode must be Sliced or Tiled, or it can't grow.", this);
            }

            elapsed = age;
            launched = true;
            Show();
            // Placing the head must not leave a streak from wherever it sat in the prefab.
            foreach (TrailRenderer trail in trails) trail.Clear();
        }

        private void Update()
        {
            if (!launched) return;

            elapsed += Time.deltaTime;
            Show();
        }

        private void Show()
        {
            float travel = Mathf.Clamp01(elapsed / travelTime);
            Vector3 front = Vector3.Lerp(from, to, travel);
            if (head != null) head.position = front;
            if (beam != null)
            {
                float scale = Mathf.Abs(beam.transform.lossyScale.x);
                if (scale > 0f) beam.size = new Vector2(Vector3.Distance(from, front) / scale, beam.size.y);
            }

            if (travel < 1f) return;

            if (!arrived) Arrive();

            float fade = fadeTime > 0f ? Mathf.Clamp01((elapsed - travelTime) / fadeTime) : 1f;
            for (int i = 0; i < sprites.Length; i++)
            {
                Color colour = spriteColours[i];
                colour.a *= 1f - fade;
                sprites[i].color = colour;
            }

            if (fade >= 1f)
            {
                launched = false;
                Destroy(gameObject);
            }
        }

        private void Arrive()
        {
            arrived = true;
            foreach (TrailRenderer trail in trails) trail.emitting = false;
            foreach (ParticleSystem particles in particleSystems) particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
