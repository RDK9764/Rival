using UnityEngine;
using Rival.Actors;
using Rival.Combat;

namespace Rival.Enemy
{
    /// <summary>
    /// Executes a rival's decisions on the shared CharacterMotor. Mirrors
    /// PlayerController's execution contract but is driven by RivalBrain
    /// instead of IPlayerInput. Facing is handled by LockOnController, because
    /// the brain holds its target lock permanently.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(StaminaComponent))]
    [RequireComponent(typeof(CombatController))]
    [RequireComponent(typeof(DodgeInvulnerability))]
    public class RivalController : MonoBehaviour
    {
        [SerializeField] private CharacterMovementConfig movementConfig;
        [Tooltip("Rival moves slightly slower than the player so a mirror match stays readable.")]
        [SerializeField, Range(0.5f, 1f)] private float movementSpeedMultiplier = 0.85f;
        [SerializeField] private RivalBrain brain;
        [SerializeField] private CombatController combat;
        [SerializeField] private LockOnController lockOn;

        public CharacterMotor Motor { get; private set; }

        /// <summary>Runtime override for roster archetypes (RivalMatchSetup). Clamped to the same range as the inspector field.</summary>
        public void SetMovementSpeedMultiplier(float value)
            => movementSpeedMultiplier = Mathf.Clamp(value, 0.5f, 1f);

        private void Awake()
        {
            Motor = GetComponent<CharacterMotor>();
            Motor.Initialize(movementConfig);

            if (brain == null) brain = GetComponent<RivalBrain>();
            if (combat == null) combat = GetComponent<CombatController>();
            if (lockOn == null) lockOn = GetComponent<LockOnController>();

            if (brain != null) brain.DodgeRequested += OnDodgeRequested;
        }

        private void OnDestroy()
        {
            if (brain != null) brain.DodgeRequested -= OnDodgeRequested;
        }

        private void OnDodgeRequested()
        {
            if (brain != null) Motor.TryDodge(brain.DodgeDirection);
        }

        private void FixedUpdate()
        {
            if (movementConfig == null || Motor == null) return;

            var direction = combat != null && combat.IsMovementLocked
                ? Vector3.zero
                : (brain != null ? brain.MovementDirection : Vector3.zero);

            Motor.Move(direction, movementConfig.moveSpeed * movementSpeedMultiplier, Time.fixedDeltaTime);
        }
    }
}
