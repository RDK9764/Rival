using System;
using UnityEngine;
using Rival.Core;
using Rival.Utilities;

namespace Rival.Enemy
{
    /// <summary>
    /// Manages endless mode: escalating waves of rival archetypes with
    /// increasing difficulty. Each wave picks a harder archetype, scales
    /// its stats, and tracks the player's streak. The match continues
    /// until the player loses. Fires events so UI can display wave info.
    /// </summary>
    public class EndlessModeManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RivalRoster roster;
        [SerializeField] private RivalMatchSetup matchSetup;

        [Header("Difficulty")]
        [SerializeField, Range(1f, 2f)] private float speedScalePerWave = 1.04f;
        [SerializeField, Range(0.8f, 1f)] private float reactionScalePerWave = 0.96f;
        [SerializeField, Range(0.9f, 1f)] private float cooldownScalePerWave = 0.95f;
        [SerializeField, Range(1f, 1.15f)] private float damageScalePerWave = 1.05f;
        [SerializeField, Range(0f, 0.3f)] private float aggressionBoostPerWave = 0.02f;
        [SerializeField, Range(0f, 0.05f)] private float parryBoostPerWave = 0.01f;

        [Header("Pacing")]
        [SerializeField, Range(1f, 5f)] private float waveDelay = 2f;
        [SerializeField, Range(0, 5)] private int easyWaveCount = 3;

        public int CurrentWave { get; private set; }
        public int BestWave { get; private set; }
        public bool IsActive { get; private set; }
        public float CurrentDamageScale => Mathf.Pow(damageScalePerWave, CurrentWave - 1);
        public float CurrentSpeedScale => Mathf.Pow(speedScalePerWave, CurrentWave - 1);

        public event Action<int> WaveStarted;
        public event Action<int, bool> WaveEnded;
        public event Action<int> MatchComplete;

        private float waveTimer = -1f;
        private bool waitingForNextWave;
        private HealthComponent playerHealth;
        private HealthComponent rivalHealth;
        private GameManager gameManager;
        private IStatsTracker statsTracker;

        private void Start()
        {
            gameManager = GameManager.Instance;
            ServiceLocator.TryGet(out statsTracker);
            BestWave = PlayerPrefs.GetInt("Rival_Endless_BestWave", 0);

            if (!ServiceLocator.TryGet<IMatchConfig>(out var matchConfig)) return;

            // Difficulty scaling assigns over the shared archetype assets; keep
            // pristine bases so waves never compound and MATCH mode is untouched.
            if (matchConfig.GameMode == RivalGameMode.Endless && roster != null)
            {
                foreach (var archetype in roster.Archetypes)
                {
                    if (archetype != null) CaptureBase(archetype);
                }
            }

            if (matchConfig.GameMode != RivalGameMode.Endless) return;

            if (matchSetup == null) ServiceLocator.TryGet(out matchSetup);
            if (matchSetup == null) return;

            playerHealth = matchSetup.PlayerHealth;
            rivalHealth = matchSetup.RivalHealth;
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            if (rivalHealth != null) rivalHealth.Died += OnRivalDied;

            StartEndlessMode(playerHealth, rivalHealth);
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            if (rivalHealth != null) rivalHealth.Died -= OnRivalDied;
            RestoreArchetypes();
        }

        private void Update()
        {
            if (!waitingForNextWave) return;
            waveTimer -= Time.unscaledDeltaTime;
            if (waveTimer <= 0f)
            {
                waitingForNextWave = false;
                SpawnNextWave();
            }
        }

        public void StartEndlessMode(HealthComponent player, HealthComponent rival)
        {
            playerHealth = player;
            rivalHealth = rival;
            CurrentWave = 0;
            IsActive = true;
            SpawnNextWave();
        }

        public void StopEndlessMode()
        {
            IsActive = false;
            waitingForNextWave = false;
            RestoreArchetypes();
        }

        public void OnRivalDied()
        {
            if (!IsActive) return;
            WaveEnded?.Invoke(CurrentWave, true);
            waitingForNextWave = true;
            waveTimer = waveDelay;
        }

        public void OnPlayerDied()
        {
            if (!IsActive) return;
            IsActive = false;
            WaveEnded?.Invoke(CurrentWave, false);
            RestoreArchetypes();

            if (CurrentWave > BestWave)
            {
                BestWave = CurrentWave;
                PlayerPrefs.SetInt("Rival_Endless_BestWave", BestWave);
                PlayerPrefs.Save();
            }

            statsTracker?.RecordLoss();
            AwardWaveRewards();

            MatchComplete?.Invoke(CurrentWave);
            if (gameManager != null) gameManager.SetState(GameState.GameOver);
        }

        /// <summary>Awards currency for every wave survived (10/wave) plus a 25 bonus if 10+ waves.</summary>
        private void AwardWaveRewards()
        {
            if (!ServiceLocator.TryGet<PlayerLoadoutManager>(out var loadout)) return;
            var amount = CurrentWave * 10 + (CurrentWave >= 10 ? 25 : 0);
            loadout.AwardCurrency(amount);
        }

        private void SpawnNextWave()
        {
            CurrentWave++;
            WaveStarted?.Invoke(CurrentWave);

            var archetype = PickArchetype();
            if (archetype != null && matchSetup != null)
            {
                matchSetup.SetArchetypeByAsset(archetype);
                ApplyDifficultyScaling(archetype);
            }

            if (playerHealth != null) playerHealth.ResetFull();
            if (rivalHealth != null) rivalHealth.ResetFull();

            var playerCombat = playerHealth != null ? playerHealth.GetComponent<CombatController>() : null;
            var rivalCombat = rivalHealth != null ? rivalHealth.GetComponent<CombatController>() : null;
            var playerStamina = playerHealth != null ? playerHealth.GetComponent<StaminaComponent>() : null;
            var rivalStamina = rivalHealth != null ? rivalHealth.GetComponent<StaminaComponent>() : null;

            if (playerCombat != null) playerCombat.ResetState();
            if (rivalCombat != null) rivalCombat.ResetState();
            if (playerStamina != null) playerStamina.ResetFull();
            if (rivalStamina != null) rivalStamina.ResetFull();
        }

        private RivalArchetype PickArchetype()
        {
            if (roster == null || roster.Archetypes.Count == 0) return null;

            // Early waves: pick from easier archetypes
            // Later waves: pick from all, biased toward harder ones
            int index;
            if (CurrentWave <= easyWaveCount)
            {
                index = Mathf.Min(CurrentWave - 1, roster.Archetypes.Count - 1);
            }
            else
            {
                // Weighted random: later index = harder
                var maxIndex = roster.Archetypes.Count - 1;
                var bias = Mathf.Clamp01((float)(CurrentWave - easyWaveCount) / 10f);
                index = Mathf.Clamp(Mathf.RoundToInt(UnityEngine.Random.Range(0f, maxIndex) + bias * 2f), 0, maxIndex);
            }

            return roster.Archetypes[index];
        }

        private void ApplyDifficultyScaling(RivalArchetype archetype)
        {
            var wave = CurrentWave - 1;
            if (!bases.TryGetValue(archetype, out var b)) CaptureBase(archetype);
            b = bases[archetype];

            archetype.MovementSpeedMultiplier = Mathf.Min(1.2f, b.MovementSpeedMultiplier * Mathf.Pow(speedScalePerWave, wave));
            archetype.MaxReactionDelay = b.MaxReactionDelay * Mathf.Pow(reactionScalePerWave, wave);
            archetype.AttackCooldown = b.AttackCooldown * Mathf.Pow(cooldownScalePerWave, wave);

            if (archetype.BaseStyle != null)
            {
                archetype.BaseStyle.Aggression = Mathf.Min(1f, b.Aggression + aggressionBoostPerWave * wave);
                archetype.BaseStyle.ParryFrequency = Mathf.Min(0.8f, b.ParryFrequency + parryBoostPerWave * wave);
            }
        }

        private struct ArchetypeBase
        {
            public float MovementSpeedMultiplier, MaxReactionDelay, AttackCooldown, Aggression, ParryFrequency;
        }

        private readonly System.Collections.Generic.Dictionary<RivalArchetype, ArchetypeBase> bases =
            new System.Collections.Generic.Dictionary<RivalArchetype, ArchetypeBase>();

        private void CaptureBase(RivalArchetype archetype)
        {
            if (archetype == null || bases.ContainsKey(archetype)) return;
            var hasStyle = archetype.BaseStyle != null;
            bases[archetype] = new ArchetypeBase
            {
                MovementSpeedMultiplier = archetype.MovementSpeedMultiplier,
                MaxReactionDelay = archetype.MaxReactionDelay,
                AttackCooldown = archetype.AttackCooldown,
                Aggression = hasStyle ? archetype.BaseStyle.Aggression : 0f,
                ParryFrequency = hasStyle ? archetype.BaseStyle.ParryFrequency : 0f
            };
        }

        private void RestoreArchetypes()
        {
            foreach (var pair in bases)
            {
                var archetype = pair.Key;
                if (archetype == null) continue;
                var b = pair.Value;
                archetype.MovementSpeedMultiplier = b.MovementSpeedMultiplier;
                archetype.MaxReactionDelay = b.MaxReactionDelay;
                archetype.AttackCooldown = b.AttackCooldown;
                if (archetype.BaseStyle != null)
                {
                    archetype.BaseStyle.Aggression = Mathf.Clamp01(b.Aggression);
                    archetype.BaseStyle.ParryFrequency = Mathf.Clamp01(b.ParryFrequency);
                }
            }
            bases.Clear();
        }
    }
}
