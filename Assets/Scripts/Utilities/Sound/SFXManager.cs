using System.Collections.Generic;
using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Plays one-shot sound effects on a small set of reusable voices, so rapid sounds can overlap and each can have
    /// its own pitch. When every voice is busy, the one that started longest ago is cut off and reused.
    /// </summary>
    public class SFXManager : Singleton<SFXManager>
    {
        [Tooltip("Optional template. Each voice copies its output mixer group, priority and 3D distance settings. It is never played itself.")]
        [SerializeField] private AudioSource effectsSource;

        [Tooltip("How many sounds can play at once.")]
        [Min(1), SerializeField] private int voiceCount = 8;

        [Range(0f, 1f), SerializeField] private float volume = 1f;

        private AudioSource[] voices;
        private float[] voiceStartTimes;

        // When each profile last played, for SoundProfileSO.MinInterval.
        private readonly Dictionary<SoundProfileSO, float> lastPlayTimes = new Dictionary<SoundProfileSO, float>();
        // Profiles already warned about having no clips, so a rapid sound doesn't flood the console.
        private readonly HashSet<SoundProfileSO> warnedProfiles = new HashSet<SoundProfileSO>();
        private bool warnedNullProfile;

        protected override void Awake()
        {
            base.Awake();
            if (instance != this) return;

            voices = new AudioSource[voiceCount];
            voiceStartTimes = new float[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                voices[i] = CreateVoice(i);
                voiceStartTimes[i] = float.NegativeInfinity;
            }
        }

        /// <summary>Plays a sound with no position. <paramref name="pitch"/> multiplies the profile's random pitch.</summary>
        public void PlaySound(SoundProfileSO profile, float pitch = 1f)
        {
            Play(profile, pitch, null);
        }

        /// <summary>Plays a fully 3D sound at a world position. <paramref name="pitch"/> multiplies the profile's random pitch.</summary>
        public void PlaySoundAtPosition(SoundProfileSO profile, Vector3 position, float pitch = 1f)
        {
            Play(profile, pitch, position);
        }

        /// <summary>Master SFX volume. Applies to sounds started after the change.</summary>
        public void SetVolume(float volume)
        {
            this.volume = Mathf.Clamp01(volume);
        }

        private void Play(SoundProfileSO profile, float pitch, Vector3? position)
        {
            if (voices == null) return;
            if (!TryGetClip(profile, out AudioClip clip)) return;
            if (!ReadyToPlay(profile)) return;

            AudioSource voice = TakeVoice();
            if (position.HasValue)
            {
                voice.transform.position = position.Value;
                voice.spatialBlend = 1f;
            }
            else
            {
                voice.transform.localPosition = Vector3.zero;
                voice.spatialBlend = 0f;
            }

            voice.clip = clip;
            voice.volume = profile.Volume * volume;
            // A pitch at or below zero would play backwards or not at all.
            voice.pitch = Mathf.Max(0.01f, profile.RandomPitch() * pitch);
            voice.Play();
        }

        /// <summary>False if the profile played less than its minInterval ago; otherwise records this play.</summary>
        private bool ReadyToPlay(SoundProfileSO profile)
        {
            if (profile.MinInterval <= 0f) return true;

            float now = Time.unscaledTime;
            if (lastPlayTimes.TryGetValue(profile, out float last) && now - last < profile.MinInterval) return false;

            lastPlayTimes[profile] = now;
            return true;
        }

        /// <summary>A free voice, or the one that started longest ago if all are busy.</summary>
        private AudioSource TakeVoice()
        {
            int chosen = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying)
                {
                    chosen = i;
                    break;
                }
                if (voiceStartTimes[i] < voiceStartTimes[chosen]) chosen = i;
            }

            voiceStartTimes[chosen] = Time.unscaledTime;
            return voices[chosen];
        }

        private AudioSource CreateVoice(int index)
        {
            // A child object per voice, so positional sounds can move it.
            GameObject go = new GameObject($"Voice {index}");
            go.transform.SetParent(transform, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            if (effectsSource != null)
            {
                source.outputAudioMixerGroup = effectsSource.outputAudioMixerGroup;
                source.priority = effectsSource.priority;
                source.rolloffMode = effectsSource.rolloffMode;
                source.minDistance = effectsSource.minDistance;
                source.maxDistance = effectsSource.maxDistance;
                source.dopplerLevel = effectsSource.dopplerLevel;
            }
            return source;
        }

        private bool TryGetClip(SoundProfileSO profile, out AudioClip clip)
        {
            // Unity's == null, not ?., so a missing or destroyed asset counts as null.
            if (profile == null)
            {
                clip = null;
                if (!warnedNullProfile) Debug.LogWarning("SFXManager: asked to play a missing sound profile. (Warned once.)", this);
                warnedNullProfile = true;
                return false;
            }

            clip = profile.Clip();
            if (clip != null) return true;

            if (warnedProfiles.Add(profile))
            {
                Debug.LogWarning($"SFXManager: {profile.name} has no clips assigned, so it plays nothing. (Warned once per profile.)", profile);
            }
            return false;
        }
    }
}
