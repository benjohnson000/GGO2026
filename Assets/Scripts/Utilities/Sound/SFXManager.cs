using UnityEngine;

namespace benjohnson
{
    public class SFXManager : Singleton<SFXManager>
    {
        [SerializeField] private AudioSource effectsSource;
        [Range(0f, 1f), SerializeField] private float volume = 1f;

        public void PlaySound(SoundProfile profile)
        {
            if (!TryGetClip(profile, out AudioClip clip)) return;

            effectsSource.PlayOneShot(clip, profile.volume * volume);
        }

        public void PlaySoundAtPosition(SoundProfile profile, Vector3 position)
        {
            if (!TryGetClip(profile, out AudioClip clip)) return;

            AudioSource.PlayClipAtPoint(clip, position, profile.volume * volume);
        }

        public void SetVolume(float volume)
        {
            this.volume = Mathf.Clamp01(volume);
        }

        private static bool TryGetClip(SoundProfile profile, out AudioClip clip)
        {
            clip = profile?.Clip();
            if (clip == null)
            {
                Debug.LogWarning("No sound clips assigned.");
                return false;
            }
            return true;
        }
    }

    [System.Serializable]
    public class SoundProfile
    {
        public AudioClip[] clips;
        [Range(0, 1)] public float volume = 0.75f;

        /// <summary>Returns a random clip, or null if none are assigned.</summary>
        public AudioClip Clip()
        {
            if (clips == null || clips.Length == 0)
                return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}
