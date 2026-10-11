using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    public enum MusicState
    {
        None,
        MainMenu,
        Combat,
        Endless,
        Victory,
        Defeat
    }

    /// <summary>
    /// Manages background music with crossfading between game states.
    /// Resolves IAudioService for playback and accepts AudioClip references
    /// per state. Registered as IMusicService on the ServiceLocator.
    /// </summary>
    public interface IMusicService
    {
        void SetState(MusicState state);
        void SetVolume(float volume);
    }

    public class MusicService : MonoBehaviour, IMusicService
    {
        [System.Serializable]
        private struct MusicEntry
        {
            public MusicState state;
            public AudioClip clip;
        }

        [SerializeField] private MusicEntry[] entries;
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.35f;
        [SerializeField, Range(0.1f, 3f)] private float crossfadeDuration = 1f;

        private IAudioService audioService;
        private MusicState currentState;
        private float crossfadeTimer;
        private bool isCrossfading;
        private AudioSource sourceA;
        private AudioSource sourceB;
        private bool activeSourceIsA;

        private void Awake()
        {
            ServiceLocator.Register<IMusicService>(this);
            sourceA = gameObject.AddComponent<AudioSource>();
            sourceA.loop = true;
            sourceA.playOnAwake = false;
            sourceB = gameObject.AddComponent<AudioSource>();
            sourceB.loop = true;
            sourceB.playOnAwake = false;
        }

        private void Start()
        {
            ServiceLocator.TryGet(out audioService);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IMusicService>(this);
        }

        private void Update()
        {
            if (!isCrossfading) return;

            crossfadeTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(crossfadeTimer / crossfadeDuration);

            var from = activeSourceIsA ? sourceA : sourceB;
            var to = activeSourceIsA ? sourceB : sourceA;

            from.volume = Mathf.Lerp(defaultVolume, 0f, t);
            to.volume = Mathf.Lerp(0f, defaultVolume, t);

            if (t >= 1f)
            {
                from.Stop();
                isCrossfading = false;
            }
        }

        public void SetState(MusicState state)
        {
            if (state == currentState) return;
            currentState = state;

            var clip = GetClip(state);
            if (clip == null) return;

            CrossfadeTo(clip);
        }

        public void SetVolume(float volume)
        {
            defaultVolume = Mathf.Clamp01(volume);
            var active = activeSourceIsA ? sourceA : sourceB;
            if (active.isPlaying) active.volume = defaultVolume;
        }

        private void CrossfadeTo(AudioClip clip)
        {
            var next = activeSourceIsA ? sourceB : sourceA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();

            crossfadeTimer = 0f;
            isCrossfading = true;
            activeSourceIsA = !activeSourceIsA;
        }

        private AudioClip GetClip(MusicState state)
        {
            if (entries == null) return null;
            foreach (var entry in entries)
            {
                if (entry.state == state && entry.clip != null) return entry.clip;
            }
            return null;
        }
    }
}
