using UnityEngine;
using Rival.Combat;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Bridges combat events to game-feel systems. Subscribes to HitTaken,
    /// AttackResolved, and Died on both actors' combat components, then triggers
    /// HitStop, camera shake, and audio. Attach to a scene-level GameObject.
    /// </summary>
    public class CombatFeelConnector : MonoBehaviour
    {
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private HealthComponent rivalHealth;
        [SerializeField] private CombatController playerCombat;
        [SerializeField] private CombatController rivalCombat;
        [SerializeField] private DamageReceiver playerDamageReceiver;
        [SerializeField] private DamageReceiver rivalDamageReceiver;
        [SerializeField] private HitStop hitStop;
        [SerializeField] private GameFeelService gameFeel;

        private IAudioService audioService;

        private void Start()
        {
            ServiceLocator.TryGet(out audioService);

            if (playerCombat != null) playerCombat.AttackResolved += OnAttackResolved;
            if (rivalCombat != null) rivalCombat.AttackResolved += OnAttackResolved;
            if (playerDamageReceiver != null) playerDamageReceiver.HitTaken += OnHitTaken;
            if (rivalDamageReceiver != null) rivalDamageReceiver.HitTaken += OnHitTaken;
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            if (rivalHealth != null) rivalHealth.Died += OnRivalDied;
        }

        private void OnDestroy()
        {
            if (playerCombat != null) playerCombat.AttackResolved -= OnAttackResolved;
            if (rivalCombat != null) rivalCombat.AttackResolved -= OnAttackResolved;
            if (playerDamageReceiver != null) playerDamageReceiver.HitTaken -= OnHitTaken;
            if (rivalDamageReceiver != null) rivalDamageReceiver.HitTaken -= OnHitTaken;
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            if (rivalHealth != null) rivalHealth.Died -= OnRivalDied;
        }

        private void OnAttackResolved(AttackDefinition attack, int comboIndex, HitOutcome outcome)
        {
            if (attack == null) return;

            switch (outcome)
            {
                case HitOutcome.Normal:
                    hitStop?.TriggerHitStop();
                    gameFeel?.ShakeCamera(0.08f, 0.1f);
                    audioService?.PlaySfx(attack.AttackType == AttackType.Heavy
                        ? SfxCategory.HeavyAttack : SfxCategory.LightAttack);
                    break;
                case HitOutcome.Blocked:
                    audioService?.PlaySfx(SfxCategory.Block);
                    gameFeel?.ShakeCamera(0.04f, 0.06f);
                    break;
                case HitOutcome.BlockBroken:
                    hitStop?.TriggerHitStop();
                    gameFeel?.ShakeCamera(0.15f, 0.15f);
                    audioService?.PlaySfx(SfxCategory.GuardBreak);
                    break;
                case HitOutcome.Parried:
                    hitStop?.TriggerHitStop();
                    gameFeel?.ShakeCamera(0.12f, 0.12f);
                    audioService?.PlaySfx(SfxCategory.Parry);
                    break;
                case HitOutcome.Evaded:
                    audioService?.PlaySfx(SfxCategory.Dodge);
                    break;
            }
        }

        private void OnHitTaken(DamageContext context, DamageResult result)
        {
            if (result.Outcome == HitOutcome.Normal || result.Outcome == HitOutcome.BlockBroken)
            {
                audioService?.PlaySfx(SfxCategory.Hit);
            }
        }

        private void OnPlayerDied() => HandleDeath(false);
        private void OnRivalDied() => HandleDeath(true);

        private void HandleDeath(bool playerWon)
        {
            hitStop?.TriggerKillSlowMotion();
            gameFeel?.ShakeCamera(0.25f, 0.3f);
            audioService?.PlaySfx(SfxCategory.Death);
        }
    }
}
