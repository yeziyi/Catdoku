using System;
using UnityEngine;

namespace ColorCubeShooter
{
    /// <summary>
    /// Manages background music and sound effect playback based on <see cref="SoundConfig"/>.
    /// Also saves/updates Sound and Music toggle states from settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoundManager : MonoBehaviour
    {
        public const string PrefsSoundEnabledKey = "settings_sound_enabled";
        public const string PrefsMusicEnabledKey = "settings_music_enabled";

        public static SoundManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] SoundConfig soundConfig;

        [Header("Audio Sources (optional)")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;

        public bool IsSoundEnabled { get; private set; } = true;
        public bool IsMusicEnabled { get; private set; } = true;

        public event Action<bool> OnSoundEnabledChanged;
        public event Action<bool> OnMusicEnabledChanged;

        AudioClip currentMusicClip;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSources();
            LoadSettingsFromPrefs();
            ApplyAudioSettings();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void EnsureAudioSources()
        {
            if (musicSource == null)
                musicSource = CreateOrGetChildAudioSource("MusicSource", loop: true, playOnAwake: false);
            else
            {
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
                sfxSource = CreateOrGetChildAudioSource("SfxSource", loop: false, playOnAwake: false);
            else
            {
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        AudioSource CreateOrGetChildAudioSource(string childName, bool loop, bool playOnAwake)
        {
            Transform child = transform.Find(childName);
            GameObject go = child != null ? child.gameObject : new GameObject(childName);
            if (go.transform.parent != transform)
                go.transform.SetParent(transform, false);

            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null)
                source = go.AddComponent<AudioSource>();

            source.loop = loop;
            source.playOnAwake = playOnAwake;
            return source;
        }

        void LoadSettingsFromPrefs()
        {
            IsSoundEnabled = !PlayerPrefs.HasKey(PrefsSoundEnabledKey) || PlayerPrefs.GetInt(PrefsSoundEnabledKey, 1) != 0;
            IsMusicEnabled = !PlayerPrefs.HasKey(PrefsMusicEnabledKey) || PlayerPrefs.GetInt(PrefsMusicEnabledKey, 1) != 0;
        }

        void ApplyAudioSettings()
        {
            if (sfxSource != null)
                sfxSource.mute = !IsSoundEnabled;

            if (musicSource != null)
            {
                musicSource.mute = !IsMusicEnabled;
                if (!IsMusicEnabled && musicSource.isPlaying)
                    musicSource.Stop();
                else if (IsMusicEnabled && currentMusicClip != null && !musicSource.isPlaying)
                    musicSource.Play();
            }
        }

        public void SetSoundEnabled(bool enabled, bool saveToPrefs = true)
        {
            if (IsSoundEnabled == enabled)
                return;

            IsSoundEnabled = enabled;
            if (saveToPrefs)
            {
                PlayerPrefs.SetInt(PrefsSoundEnabledKey, enabled ? 1 : 0);
                PlayerPrefs.Save();
            }

            if (sfxSource != null)
                sfxSource.mute = !enabled;

            OnSoundEnabledChanged?.Invoke(enabled);
        }

        public void SetMusicEnabled(bool enabled, bool saveToPrefs = true)
        {
            if (IsMusicEnabled == enabled)
                return;

            IsMusicEnabled = enabled;
            if (saveToPrefs)
            {
                PlayerPrefs.SetInt(PrefsMusicEnabledKey, enabled ? 1 : 0);
                PlayerPrefs.Save();
            }

            if (musicSource != null)
            {
                musicSource.mute = !enabled;
                if (!enabled)
                {
                    if (musicSource.isPlaying)
                        musicSource.Stop();
                }
                else if (currentMusicClip != null)
                {
                    musicSource.clip = currentMusicClip;
                    if (!musicSource.isPlaying)
                        musicSource.Play();
                }
            }

            OnMusicEnabledChanged?.Invoke(enabled);
        }

        public void ToggleSound()
        {
            SetSoundEnabled(!IsSoundEnabled);
        }

        public void ToggleMusic()
        {
            SetMusicEnabled(!IsMusicEnabled);
        }

        public void PlayHomePanelMusic()
        {
            PlayMusic(soundConfig != null ? soundConfig.homePanelBackgroundMusic : null);
        }

        public void PlayGameMusic()
        {
            PlayMusic(soundConfig != null ? soundConfig.gameBackgroundMusic : null);
        }

        public void StopMusic()
        {
            currentMusicClip = null;
            if (musicSource != null && musicSource.isPlaying)
                musicSource.Stop();
        }

        public void PlayButtonClickSound()
        {
            PlaySfx(soundConfig != null ? soundConfig.buttonClickSound : null);
        }

        public void PlayClickXSound()
        {
            PlaySfx(soundConfig != null ? soundConfig.clickXSound : null);
        }

        public void PlayUnClickXSound()
        {
            PlaySfx(soundConfig != null ? soundConfig.unClickXSound : null);
        }

        public void PlayDoubleClickCorrectSound()
        {
            PlaySfx(soundConfig != null ? soundConfig.doubleClickXCorrectSound : null);
        }

        public void PlayDoubleClickIncorrectSound()
        {
            PlaySfx(soundConfig != null ? soundConfig.doubleClickXIncorrectSound : null);
        }

        public void PlayLevelCompleteSound()
        {
            PlaySfx(soundConfig != null ? soundConfig.levelCompleteSound : null);
        }

        public void PlayOutOfSpaceSound()
        {
            PlaySfx(soundConfig != null ? soundConfig.outOfSpaceSound : null);
        }

        public void PlayMusic(AudioClip clip)
        {
            currentMusicClip = clip;
            if (musicSource == null || clip == null || !IsMusicEnabled)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource == null || clip == null || !IsSoundEnabled)
                return;

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }
    }
}
