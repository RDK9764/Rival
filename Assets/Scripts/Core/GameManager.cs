using System;
using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Root orchestrator and pure game-state machine. Broadcasts state
    /// transitions; side effects (pause, time scale) are owned by dedicated
    /// services so this class stays small and predictable. Lives in the Boot
    /// scene and survives scene loads.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameState, GameState> GameStateChanged;
        public GameState CurrentState { get; private set; } = GameState.Boot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ServiceLocator.Unregister(this);
        }

        public void SetState(GameState newState)
        {
            if (newState == CurrentState) return;

            var previous = CurrentState;
            CurrentState = newState;
            GameStateChanged?.Invoke(previous, newState);
        }

        public void TogglePause()
        {
            SetState(CurrentState == GameState.Paused ? GameState.Playing : GameState.Paused);
        }
    }
}
