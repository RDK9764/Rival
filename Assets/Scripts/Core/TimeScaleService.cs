using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Owns Time.timeScale. Maps the Paused game state to scale 0 and restores
    /// the gameplay scale otherwise, by subscribing to GameManager's state
    /// transitions. Keeps GameManager a pure state machine and gives future
    /// systems (slow-mo, hit-stop) a single place to control time.
    /// </summary>
    public class TimeScaleService : MonoBehaviour, ITimeService
    {
        public float Scale => gameplayScale;

        private GameManager gameManager;
        private bool subscribed;
        private float gameplayScale = 1f;
        private float frozenScale = 1f;

        private void Awake()
        {
            ServiceLocator.Register<ITimeService>(this);
        }

        private void OnDestroy()
        {
            if (gameManager != null) gameManager.GameStateChanged -= OnGameStateChanged;
            ServiceLocator.Unregister<ITimeService>(this);
            Time.timeScale = 1f;
        }

        public void SetScale(float scale)
        {
            gameplayScale = Mathf.Max(0f, scale);
            Apply();
        }

        private void Update()
        {
            if (subscribed) return;

            // Subscribed lazily so registration order within the Boot scene
            // never matters.
            if (gameManager == null) gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.GameStateChanged += OnGameStateChanged;
            subscribed = true;
            Apply();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.GameOver) frozenScale = gameplayScale;
            Apply();
        }

        private void Apply()
        {
            var paused = gameManager != null && gameManager.CurrentState == GameState.Paused;
            var gameOver = gameManager != null && gameManager.CurrentState == GameState.GameOver;
            Time.timeScale = (paused || gameOver) ? 0f : gameplayScale;
        }
    }
}
