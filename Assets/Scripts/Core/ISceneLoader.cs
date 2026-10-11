using System;

namespace Rival.Core
{
    /// <summary>
    /// Contract for scene transitions. Gameplay depends on this interface so a
    /// loading screen / fade-out can be added without touching any caller.
    /// </summary>
    public interface ISceneLoader
    {
        string CurrentScene { get; }

        /// <summary>Synchronous load; the game must tolerate a hitch.</summary>
        void Load(string sceneName, Action onComplete = null);

        /// <summary>Asynchronous load with optional progress and completion callbacks.</summary>
        void LoadAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null);
    }
}
