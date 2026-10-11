using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Central game settings backed by PlayerPrefs. Reads on Awake, writes
    /// immediately on change. Any system can resolve IGameSettings to read
    /// current values without knowing about PlayerPrefs.
    /// </summary>
    public interface IGameSettings
    {
        float MusicVolume { get; }
        float SfxVolume { get; }
        float CameraSensitivity { get; }
        bool InvertCameraY { get; }
        int QualityLevel { get; }
        bool Fullscreen { get; }

        void SetMusicVolume(float value);
        void SetSfxVolume(float value);
        void SetCameraSensitivity(float value);
        void SetInvertCameraY(bool value);
        void SetQualityLevel(int level);
        void SetFullscreen(bool value);
        void ApplyAll();
    }

    public class GameSettings : MonoBehaviour, IGameSettings
    {
        private const string MusicVolKey = "Rival_MusicVolume";
        private const string SfxVolKey = "Rival_SfxVolume";
        private const string SensitivityKey = "Rival_CameraSensitivity";
        private const string InvertYKey = "Rival_InvertCameraY";
        private const string QualityKey = "Rival_QualityLevel";
        private const string FullscreenKey = "Rival_Fullscreen";

        public float MusicVolume { get; private set; } = 0.5f;
        public float SfxVolume { get; private set; } = 0.8f;
        public float CameraSensitivity { get; private set; } = 3f;
        public bool InvertCameraY { get; private set; }
        public int QualityLevel { get; private set; } = 2;
        public bool Fullscreen { get; private set; } = true;

        private IAudioService audioService;

        private void Awake()
        {
            ServiceLocator.Register<IGameSettings>(this);
            Load();
            ApplyAll();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IGameSettings>(this);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolKey, MusicVolume);
            ServiceLocator.TryGet(out audioService);
            audioService?.SetMusicVolume(MusicVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolKey, SfxVolume);
            ServiceLocator.TryGet(out audioService);
            audioService?.SetSfxVolume(SfxVolume);
        }

        public void SetCameraSensitivity(float value)
        {
            CameraSensitivity = Mathf.Clamp(value, 0.5f, 10f);
            PlayerPrefs.SetFloat(SensitivityKey, CameraSensitivity);
        }

        public void SetInvertCameraY(bool value)
        {
            InvertCameraY = value;
            PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
        }

        public void SetQualityLevel(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(QualityKey, QualityLevel);
            QualitySettings.SetQualityLevel(QualityLevel);
        }

        public void SetFullscreen(bool value)
        {
            Fullscreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Screen.fullScreen = value;
        }

        public void ApplyAll()
        {
            ServiceLocator.TryGet(out audioService);
            audioService?.SetMusicVolume(MusicVolume);
            audioService?.SetSfxVolume(SfxVolume);
            QualitySettings.SetQualityLevel(QualityLevel);
            Screen.fullScreen = Fullscreen;
        }

        private void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat(MusicVolKey, 0.5f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolKey, 0.8f);
            CameraSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 3f);
            InvertCameraY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            QualityLevel = PlayerPrefs.GetInt(QualityKey, 2);
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        }
    }
}
