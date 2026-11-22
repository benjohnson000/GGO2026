using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// One sound effect, played through <see cref="SFXManager"/>: a set of clips (one picked at random per play),
    /// volume, a random pitch range and a minimum gap between plays. Shared as an asset, so several things can use
    /// the same sound. <see cref="MinInterval"/> is tracked per asset, so it applies across everything using it.
    /// </summary>
    [CreateAssetMenu(menuName = "Sound Profile", fileName = "new SoundProfile")]
    public class SoundProfileSO : ScriptableObject
    {
        [Tooltip("One is picked at random per play.")]
        [SerializeField] private AudioClip[] clips;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.75f;

        [Tooltip("Each play picks a random pitch between min and max. 1 and 1 = no variation.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float minPitch = 1f;

        [Range(0.1f, 3f)]
        [SerializeField] private float maxPitch = 1f;

        [Tooltip("Seconds before this sound can play again; plays sooner than that are skipped. 0 = no limit.")]
        [Min(0f)]
        [SerializeField] private float minInterval;

        public float Volume => volume;
        public float MinInterval => minInterval;

        /// <summary>True if at least one clip is assigned.</summary>
        public bool HasClips
        {
            get
            {
                if (clips == null) return false;
                foreach (AudioClip clip in clips)
                {
                    if (clip != null) return true;
                }
                return false;
            }
        }

        /// <summary>Returns a random clip, or null if none are assigned.</summary>
        public AudioClip Clip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>A random pitch between minPitch and maxPitch, in either order.</summary>
        public float RandomPitch()
        {
            return Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
        }
    }
}
