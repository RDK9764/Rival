using UnityEngine;
using Rival.Combat;
using Rival.Actors;

namespace Rival.Combat
{
    /// <summary>
    /// Bridges the CombatController state machine to an Animator. Writes
    /// parameters each frame so an AnimatorController can drive animations
    /// without knowing about combat states directly. Supports locomotion
    /// blend (Speed, SpeedX, SpeedZ), attack layer fade, dodge state,
    /// and reactive triggers (Hit, Died). Attach to the same GameObject
    /// as the CombatController and Animator.
    /// </summary>
    [RequireComponent(typeof(CombatController))]
    public class AnimatorAdapter : MonoBehaviour
    {
        private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int IsBlocking = Animator.StringToHash("IsBlocking");
        private static readonly int IsStunned = Animator.StringToHash("IsStunned");
        private static readonly int IsDodging = Animator.StringToHash("IsDodging");
        private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int DiedTrigger = Animator.StringToHash("Died");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int SpeedX = Animator.StringToHash("SpeedX");
        private static readonly int SpeedZ = Animator.StringToHash("SpeedZ");

        [SerializeField] private CombatController combat;
        [SerializeField] private Animator animator;
        [SerializeField] private HealthComponent health;
        [SerializeField] private CharacterMotor motor;

        [Header("Blending")]
        [SerializeField, Range(0.05f, 0.5f)] private float speedDampTime = 0.15f;
        [SerializeField, Range(5f, 30f)] private float layerFadeSpeed = 15f;

        private float attackLayerWeight;
        private bool attackActive;

        private void Awake()
        {
            if (combat == null) combat = GetComponent<CombatController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (health == null) health = GetComponent<HealthComponent>();
            if (motor == null) motor = GetComponent<CharacterMotor>();
        }

        private void OnEnable()
        {
            if (combat != null) combat.AttackStarted += OnAttackStarted;
            if (combat != null) combat.AttackResolved += OnAttackResolved;
            if (combat != null) combat.StateChanged += OnStateChanged;
            if (health != null) health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (combat != null) combat.AttackStarted -= OnAttackStarted;
            if (combat != null) combat.AttackResolved -= OnAttackResolved;
            if (combat != null) combat.StateChanged -= OnStateChanged;
            if (health != null) health.Died -= OnDied;
        }

        private void Update()
        {
            if (animator == null || combat == null) return;

            animator.SetBool(IsAttacking, combat.State == CombatState.Attacking);
            animator.SetBool(IsBlocking, combat.State == CombatState.Blocking);
            animator.SetBool(IsStunned, combat.State == CombatState.Stunned);
            animator.SetBool(IsDodging, motor != null && motor.IsDodging);

            if (motor != null && motor.Rigidbody != null)
            {
                var velocity = motor.Rigidbody.velocity;
                velocity.y = 0f;
                var targetSpeed = velocity.magnitude;
                animator.SetFloat(Speed, targetSpeed, speedDampTime, Time.deltaTime);

                var localDir = transform.InverseTransformDirection(velocity);
                animator.SetFloat(SpeedX, localDir.x, speedDampTime, Time.deltaTime);
                animator.SetFloat(SpeedZ, localDir.z, speedDampTime, Time.deltaTime);
            }

            // Fade attack layer weight
            var targetWeight = attackActive ? 1f : 0f;
            attackLayerWeight = Mathf.MoveTowards(attackLayerWeight, targetWeight, layerFadeSpeed * Time.deltaTime);
            if (animator.layerCount > 1)
                animator.SetLayerWeight(1, attackLayerWeight);
        }

        private void OnAttackStarted(AttackDefinition def, int comboIndex)
        {
            if (animator == null) return;
            animator.SetInteger(AttackIndex, comboIndex);
            animator.SetTrigger(IsAttacking);
            attackActive = true;
        }

        private void OnAttackResolved(AttackDefinition def, int comboIndex, HitOutcome outcome)
        {
            attackActive = false;
        }

        private void OnStateChanged(CombatState newState)
        {
            if (animator == null || health == null || health.IsDead) return;
            if (newState == CombatState.Stunned) animator.SetTrigger(HitTrigger);
        }

        private void OnDied()
        {
            if (animator != null) animator.SetTrigger(DiedTrigger);
        }
    }
}
