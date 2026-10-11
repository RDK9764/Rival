using UnityEngine;
using Rival.BehaviorAnalysis;
using Rival.Core;
using Rival.Utilities;

namespace Rival.SaveSystem
{
    /// <summary>
    /// Checkpoints the BehaviorProfiler's current profiles to disk on a cadence,
    /// on GameOver, and on disable/quit, so learned behavior survives into the
    /// next session.
    /// </summary>
    public class ProfilePersister : MonoBehaviour
    {
        [SerializeField] private BehaviorProfiler profiler;
        [Tooltip("Seconds between profile checkpoints.")]
        [SerializeField] private float saveInterval = 5f;

        private IProfileStore store;
        private float saveTimer;

        private void Awake()
        {
            if (profiler == null) profiler = GetComponent<BehaviorProfiler>();
            ServiceLocator.TryGet(out store);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameStateChanged -= OnGameStateChanged;
            SaveAll();
        }

        private void OnApplicationQuit() => SaveAll();

        private void Update()
        {
            saveTimer -= Time.deltaTime;
            if (saveTimer > 0f) return;
            saveTimer = saveInterval;
            SaveAll();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.GameOver) SaveAll();
        }

        private void SaveAll()
        {
            if (profiler == null || store == null) return;

            foreach (var actor in profiler.ActorNames)
            {
                var profile = profiler.GetProfile(actor);
                if (profile.SampleCount == 0) continue;
                store.Save(actor, profile);
            }
        }
    }
}
