using System;
using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Central audio manager. Plays SFX (one-shot) and music (looping) through
    /// pooled AudioSources. Resolves an SfxLibrary for categorized sound lookup.
    /// Registered as IAudioService so gameplay code never touches AudioSource
    /// directly.
    /// </summary>
    public interface IAudioService
    {
        void PlaySfx(SfxCategory category);
        void PlaySfxClip(AudioClip clip);
        void PlayMusic(AudioClip clip);
        void StopMusic();
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
    }

    public enum SfxCategory
    {
        LightAttack,
        HeavyAttack,
        Hit,
        Block,
        Parry,
        Dodge,
        Death,
        GuardBreak,
        MenuConfirm
    }

    public class AudioService : MonoBehaviour, IAudioService
    {
        [SerializeField] private SfxLibrary sfxLibrary;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField] private int sfxPoolSize = 8;

        private AudioSource musicSource;
        private AudioSource[] sfxPool;
        private int sfxIndex;

        private void Awake()
        {
            ServiceLocator.Register<IAudioService>(this);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;

            sfxPool = new AudioSource[sfxPoolSize];
            for (var i = 0; i < sfxPoolSize; i++)
            {
                sfxPool[i] = gameObject.AddComponent<AudioSource>();
                sfxPool[i].playOnAwake = false;
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IAudioService>(this);
        }

        public void PlaySfx(SfxCategory category)
        {
            AudioClip clip = null;
            if (sfxLibrary != null) clip = sfxLibrary.GetClip(category);
            if (clip == null && ServiceLocator.TryGet(out IProceduralAudio proc))
                clip = proc.GetClip(category);
            if (clip != null) PlaySfxClip(clip);
        }

        public void PlaySfxClip(AudioClip clip)
        {
            if (clip == null) return;
            var source = sfxPool[sfxIndex];
            sfxIndex = (sfxIndex + 1) % sfxPool.Length;
            source.clip = clip;
            source.volume = sfxVolume;
            source.Play();
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic() => musicSource.Stop();

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume;
        }

        public void SetSfxVolume(float volume) => sfxVolume = Mathf.Clamp01(volume);
    }
}
