using System;
using UnityEngine;
using Rival.Core;
using Rival.Combat;
using Rival.Actors;
using Rival.Utilities;

namespace Rival.Enemy
{
    /// <summary>
    /// Manages best-of-N rounds. Resets health, stamina, combat state, and
    /// positions between rounds. Tracks wins per side and fires events so UI
    /// can show round announcements and score. Works with MatchController which
    /// detects death; RoundManager decides whether to start the next round or
    /// end the match.
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        [SerializeField, Range(1, 5)] private int roundsToWin = 2;
        [SerializeField, Range(0.5f, 3f)] private float roundDelay = 1.5f;
        [SerializeField, Range(0.5f, 5f)] private float respawnDelay = 2.0f;

        public int PlayerWins { get; private set; }
        public int RivalWins { get; private set; }
        public int CurrentRound { get; private set; } = 1;
        public int RoundsToWin => roundsToWin;
        public bool MatchComplete => PlayerWins >= roundsToWin || RivalWins >= roundsToWin;

        public event Action<int> RoundStarted;
        public event Action<bool> RoundEnded;
        public event Action MatchFinished;

        private HealthComponent playerHealth;
        private HealthComponent rivalHealth;
        private CombatController playerCombat;
        private CombatController rivalCombat;
        private StaminaComponent playerStamina;
        private StaminaComponent rivalStamina;
        private Transform playerTransform;
        private Transform rivalTransform;
        private GameManager gameManager;

        private Vector3 playerSpawnPos;
        private Vector3 rivalSpawnPos;
        private float respawnTimer = -1f;
        private bool waitingForRespawn;
        private bool lastRoundPlayerWon;

        public void Initialize(HealthComponent pHealth, HealthComponent rHealth,
            CombatController pCombat, CombatController rCombat,
            StaminaComponent pStamina, StaminaComponent rStamina,
            Transform player, Transform rival)
        {
            playerHealth = pHealth;
            rivalHealth = rHealth;
            playerCombat = pCombat;
            rivalCombat = rCombat;
            playerStamina = pStamina;
            rivalStamina = rStamina;
            playerTransform = player;
            rivalTransform = rival;
            playerSpawnPos = player != null ? player.position : Vector3.zero;
            rivalSpawnPos = rival != null ? rival.position : Vector3.zero;
            gameManager = GameManager.Instance;
        }

        private void Update()
        {
            if (!waitingForRespawn) return;

            respawnTimer -= Time.unscaledDeltaTime;
            if (respawnTimer <= 0f)
            {
                waitingForRespawn = false;
                if (MatchComplete)
                {
                    MatchFinished?.Invoke();
                    if (gameManager != null) gameManager.SetState(GameState.GameOver);
                }
                else
                {
                    StartRound();
                }
            }
        }

        public void OnActorDied(bool playerWon)
        {
            lastRoundPlayerWon = playerWon;
            if (playerWon) PlayerWins++;
            else RivalWins++;

            RoundEnded?.Invoke(playerWon);
            respawnTimer = respawnDelay;
            waitingForRespawn = true;
        }

        public void BeginMatch()
        {
            PlayerWins = 0;
            RivalWins = 0;
            CurrentRound = 0;
            StartRound();
        }

        private void StartRound()
        {
            CurrentRound++;
            ResetActors();
            RoundStarted?.Invoke(CurrentRound);
        }

        private void ResetActors()
        {
            if (playerHealth != null) playerHealth.ResetFull();
            if (rivalHealth != null) rivalHealth.ResetFull();
            if (playerCombat != null) playerCombat.ResetState();
            if (rivalCombat != null) rivalCombat.ResetState();
            if (playerStamina != null) playerStamina.ResetFull();
            if (rivalStamina != null) rivalStamina.ResetFull();

            if (playerTransform != null) playerTransform.position = playerSpawnPos;
            if (rivalTransform != null) rivalTransform.position = rivalSpawnPos;
        }
    }
}
