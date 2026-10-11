using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// ISceneLoader implementation. Registers itself as a service so callers
    /// depend on the interface, not this type. Lives in the Boot scene and
    /// survives scene loads.
    /// </summary>
    public class SceneLoader : MonoBehaviour, ISceneLoader
    {
        public string CurrentScene => SceneManager.GetActiveScene().name;

        private void Awake()
        {
            if (ServiceLocator.TryGet<ISceneLoader>(out _))
            {
                Debug.LogWarning("[SceneLoader] Duplicate SceneLoader detected; destroying the new instance.");
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<ISceneLoader>(this);
        }

        public void Load(string sceneName, Action onComplete = null)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            onComplete?.Invoke();
        }

        public void LoadAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' was not found. Is it added to Build Settings?");
                return;
            }

            operation.completed += _ => onComplete?.Invoke();
            StartCoroutine(ReportProgress(operation, onProgress));
        }

        private IEnumerator ReportProgress(AsyncOperation operation, Action<float> onProgress)
        {
            while (!operation.isDone)
            {
                onProgress?.Invoke(operation.progress);
                yield return null;
            }
        }
    }
}
