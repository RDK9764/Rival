using UnityEngine;

namespace Rival.Combat
{
    /// <summary>
    /// Resolves an incoming hit against the defender's current state:
    /// dodge i-frames evade, a parry window negates and punishes, blocking
    /// mitigates (and breaks when stamina runs out), otherwise full damage
    /// with a crit roll from the attacker's weapon. Emits HitTaken for UI,
    /// audio and telemetry.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class DamageReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private float blockDamageMultiplier = 0.3f;

        public IDefenseState DefenseState { get; private set; }

        public event System.Action<DamageContext, DamageResult> HitTaken;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<HealthComponent>();
            if (health == null) Debug.LogWarning("[DamageReceiver] No HealthComponent found on owner.", this);

            DefenseState = GetComponentInParent<IDefenseState>();
        }

        public DamageResult ApplyDamage(DamageContext context)
        {
            var result = new DamageResult();

            if (DefenseState != null && DefenseState.IsDodgeInvulnerable)
            {
                result.Outcome = HitOutcome.Evaded;
                result.FinalDamage = 0f;
                HitTaken?.Invoke(context, result);
                return result;
            }

            if (DefenseState != null && DefenseState.IsParryWindowActive)
            {
                DefenseState.OnSuccessfulParry(context);
                result.Outcome = HitOutcome.Parried;
                result.FinalDamage = 0f;
                HitTaken?.Invoke(context, result);
                return result;
            }

            var damage = context.BaseDamage;

            if (DefenseState != null && DefenseState.IsBlocking)
            {
                result.Outcome = DefenseState.OnBlocked(context) ? HitOutcome.Blocked : HitOutcome.BlockBroken;
                if (result.Outcome == HitOutcome.Blocked) damage *= blockDamageMultiplier;
            }
            else
            {
                result.Outcome = HitOutcome.Normal;
            }

            // Blocked hits are mitigated and cannot crit; a broken guard or a
            // clean hit can.
            var canCrit = result.Outcome == HitOutcome.Normal || result.Outcome == HitOutcome.BlockBroken;
            if (canCrit && context.Weapon != null && Random.value < context.Weapon.CritChance)
            {
                result.WasCrit = true;
                damage *= context.Weapon.CritMultiplier;
            }

            result.FinalDamage = damage;
            health?.ApplyDamage(damage);
            HitTaken?.Invoke(context, result);
            return result;
        }
    }
}
