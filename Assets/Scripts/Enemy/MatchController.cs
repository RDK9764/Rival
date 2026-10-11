using UnityEngine;
using Rival.Core;
using Rival.Combat;
using Rival.Utilities;

namespace Rival.Enemy
{
    /// <summary>
    /// Scene-level match orchestrator. Delegates round management to RoundManager
    /// (best-of-N rounds, health/stamina resets between rounds). When a death
    /// occurs, notifies RoundManager which decides whether to start the next
    /// round or end the match. Fires stats and triggers GameOver state.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        [Tooltip("Delay between death and round-end announcement.")]
        [SerializeField, Range(0.2f, 3f)] private float deathDelay = 1.0f;

        private HealthComponent playerHealth;
        private HealthComponent rivalHealth;
        private RoundManager roundManager;
        private GameManager gameManager;
        private RoundAnnouncementUI announcement;
        private IStatsTracker statsTracker;
        private float timer = -1f;
        private bool matchEnded;
        private bool lastRoundPlayerWon;

        private void Start()
        {
            gameManager = GameManager.Instance;
            announcement = FindObjectOfType<RoundAnnouncementUI>();
            ServiceLocator.TryGet(out statsTracker);

            // Endless mode owns the whole fight; the round-based match stays out.
            if (ServiceLocator.TryGet<IMatchConfig>(out var matchConfig)
                && matchConfig.GameMode == RivalGameMode.Endless)
            {
                return;
            }

            if (!ServiceLocator.TryGet<RivalMatchSetup>(out var matchSetup)) return;

            playerHealth = matchSetup.PlayerHealth;
            rivalHealth = matchSetup.RivalHealth;

            var playerGo = playerHealth != null ? playerHealth.gameObject : null;
            var rivalGo = rivalHealth != null ? rivalHealth.gameObject : null;

            var playerCombat = playerGo != null ? playerGo.GetComponent<CombatController>() : null;
            var rivalCombat = rivalGo != null ? rivalGo.GetComponent<CombatController>() : null;
            var playerStamina = playerGo != null ? playerGo.GetComponent<StaminaComponent>() : null;
            var rivalStamina = rivalGo != null ? rivalGo.GetComponent<StaminaComponent>() : null;

            var rivalController = rivalGo != null ? rivalGo.GetComponent<RivalController>() : null;

            roundManager = gameObject.GetComponent<RoundManager>();
            if (roundManager == null) roundManager = gameObject.AddComponent<RoundManager>();
            roundManager.Initialize(
                playerHealth, rivalHealth,
                playerCombat, rivalCombat,
                playerStamina, rivalStamina,
                playerGo != null ? playerGo.transform : null,
                rivalGo != null ? rivalGo.transform : null
            );

            roundManager.RoundStarted += OnRoundStarted;
            roundManager.RoundEnded += OnRoundEnded;
            roundManager.MatchFinished += OnMatchFinished;

            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            if (rivalHealth != null) rivalHealth.Died += OnRivalDied;

            roundManager.BeginMatch();
        }

        private void OnDestroy()
        {
            if (roundManager != null)
            {
                roundManager.RoundStarted -= OnRoundStarted;
                roundManager.RoundEnded -= OnRoundEnded;
                roundManager.MatchFinished -= OnMatchFinished;
            }
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            if (rivalHealth != null) rivalHealth.Died -= OnRivalDied;
        }

        private void Update()
        {
            if (timer < 0f) return;
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f) timer = -1f;
        }

        private void OnPlayerDied()
        {
            if (matchEnded || timer >= 0f) return;
            lastRoundPlayerWon = false;
            timer = deathDelay;
            roundManager.OnActorDied(false);
        }

        private void OnRivalDied()
        {
            if (matchEnded || timer >= 0f) return;
            lastRoundPlayerWon = true;
            timer = deathDelay;
            roundManager.OnActorDied(true);
        }

        private void OnRoundStarted(int round)
        {
            if (announcement != null)
            {
                announcement.ShowRoundStart(round, null);
            }
        }

        private void OnRoundEnded(bool playerWon)
        {
            if (announcement != null)
            {
                announcement.ShowRoundResult(playerWon, null);
            }
        }

        private void OnMatchFinished()
        {
            if (matchEnded) return;
            matchEnded = true;

            if (statsTracker != null)
            {
                if (lastRoundPlayerWon) statsTracker.RecordWin();
                else statsTracker.RecordLoss();
            }

            AwardMatchRewards();
        }

        /// <summary>
        /// Awards currency for a completed match: a fixed amount for finishing,
        /// plus a bonus for winning. Applied through the PlayerLoadoutManager.
        /// </summary>
        private void AwardMatchRewards()
        {
            if (!ServiceLocator.TryGet<PlayerLoadoutManager>(out var loadout)) return;
            loadout.AwardCurrency(lastRoundPlayerWon ? 25 : 10);
        }

        public int PlayerWins => roundManager != null ? roundManager.PlayerWins : 0;
        public int RivalWins => roundManager != null ? roundManager.RivalWins : 0;
        public int RoundsToWin => roundManager != null ? roundManager.RoundsToWin : 2;
        public bool PlayerWonMatch => lastRoundPlayerWon && matchEnded;
    }
}
