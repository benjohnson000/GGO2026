using UnityEngine;

namespace benjohnson
{
    public class MusicManager : Singleton<MusicManager>
    {
        [SerializeField] private AudioSource musicSource;
        [Range(0f, 1f)][SerializeField] private float volume = 1f;

        // Per-track volume of the current clip, so master volume changes keep the relative level
        private float trackVolume = 1f;

        public void PlayMusic(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("Music clip is null.");
                return;
            }

            trackVolume = Mathf.Clamp01(volume);
            musicSource.clip = clip;
            musicSource.volume = trackVolume * this.volume;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void SetMusicVolume(float volume)
        {
            this.volume = Mathf.Clamp01(volume);
            if (musicSource.isPlaying)
            {
                musicSource.volume = trackVolume * this.volume;
            }
        }
    }
}
