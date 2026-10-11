using System;
using UnityEngine;
using Rival.Actors;
using Rival.Camera;
using Rival.Combat;
using Rival.Core;
using Rival.Input;
using Rival.Utilities;

namespace Rival.Player
{
    /// <summary>
    /// Gameplay orchestrator for the player. Turns IPlayerInput into motor
    /// intents, manages stamina costs and facing. No physics math, no input
    /// system code, no movement math — each concern lives in its own type,
    /// so a bot brain can later drive this same actor via the shared motor.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(StaminaComponent))]
    [RequireComponent(typeof(DodgeInvulnerability))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterMovementConfig movementConfig;
        [SerializeField] private CombatController combat;
        [SerializeField] private LockOnController lockOn;

        public CharacterMotor Motor { get; private set; }
        public StaminaComponent StaminaComponent { get; private set; }
        public bool IsSprinting { get; private set; }

        /// <summary>World-space move direction, emitted for animation and telemetry.</summary>
        public event Action<Vector3> Moved;

        private IPlayerInput input;
        private ICameraRig cameraRig;
        private bool inputSubscribed;
        private bool matchOver;

        private void Awake()
        {
            Motor = GetComponent<CharacterMotor>();
            Motor.Initialize(movementConfig);
            StaminaComponent = GetComponent<StaminaComponent>();

            if (combat == null) combat = GetComponent<CombatController>();
            if (lockOn == null) lockOn = GetComponent<LockOnController>();
        }

        private void OnEnable()
        {
            ResolveInput();
            SubscribeInput();
            SubscribeGameManager();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            UnsubscribeGameManager();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out cameraRig);
        }

        private void Update()
        {
            // Input may register after this object's OnEnable (Awake order across
            // scene objects is not guaranteed), so retry until subscribed.
            if (!inputSubscribed || input == null)
            {
                ResolveInput();
                SubscribeInput();
            }
        }

        private void FixedUpdate()
        {
            if (movementConfig == null || StaminaComponent == null || matchOver) return;

            var moveInput = input != null ? input.Move : Vector2.zero;
            var movementLocked = combat != null && combat.IsMovementLocked;
            var blocking = combat != null && combat.IsBlocking;

            var direction = GetMoveDirection(movementLocked ? Vector2.zero : moveInput);
            IsSprinting = !movementLocked && !blocking
                         && input != null && input.IsSprinting
                         && StaminaComponent.Current > 0f
                         && moveInput.sqrMagnitude > 0.01f;

            var targetSpeed = movementConfig.moveSpeed;
            if (IsSprinting) targetSpeed *= movementConfig.sprintMultiplier;
            if (blocking) targetSpeed *= CombatController.BlockSlowMultiplier;

            Motor.Move(direction, targetSpeed, Time.fixedDeltaTime);

            if (IsSprinting) StaminaComponent.TrySpend(movementConfig.sprintStaminaCostPerSecond * Time.fixedDeltaTime);

            if (lockOn == null || !lockOn.IsLocked) RotateToward(direction, Time.fixedDeltaTime);
            Moved?.Invoke(direction);
        }

        private void ResolveInput()
        {
            input = GetComponent<IPlayerInput>();
            if (input == null) ServiceLocator.TryGet(out input);
        }

        private void SubscribeInput()
        {
            if (input == null || inputSubscribed) return;

            input.JumpPressed += OnJumpPressed;
            input.DashPressed += OnDashPressed;
            input.DodgePressed += OnDodgePressed;
            inputSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (input == null || !inputSubscribed) return;

            input.JumpPressed -= OnJumpPressed;
            input.DashPressed -= OnDashPressed;
            input.DodgePressed -= OnDodgePressed;
            inputSubscribed = false;
        }

        private void OnJumpPressed()
        {
            if (Motor.TryJump()) StaminaComponent.TrySpend(movementConfig != null ? movementConfig.jumpStaminaCost : 0f);
        }

        private void OnDashPressed()
        {
            if (movementConfig == null || StaminaComponent.Current < movementConfig.dashStaminaCost) return;

            var direction = GetMoveDirection(input != null ? input.Move : Vector2.zero);
            if (Motor.TryDash(direction)) StaminaComponent.TrySpend(movementConfig.dashStaminaCost);
        }

        private void OnDodgePressed()
        {
            if (movementConfig == null || StaminaComponent.Current < movementConfig.dodgeStaminaCost) return;

            var direction = GetMoveDirection(input != null ? input.Move : Vector2.zero);
            if (Motor.TryDodge(direction)) StaminaComponent.TrySpend(movementConfig.dodgeStaminaCost);
        }

        private Vector3 GetMoveDirection(Vector2 moveInput)
        {
            var forward = cameraRig != null ? cameraRig.Forward : Vector3.forward;
            var right = cameraRig != null ? cameraRig.Right : Vector3.right;
            return DirectionMapper.FromCamera(moveInput, forward, right);
        }

        private void RotateToward(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < 0.01f) return;

            var target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, movementConfig.turnSpeed * deltaTime);
        }

        private void SubscribeGameManager()
        {
            if (GameManager.Instance != null) GameManager.Instance.GameStateChanged += OnGameStateChanged;
        }

        private void UnsubscribeGameManager()
        {
            if (GameManager.Instance != null) GameManager.Instance.GameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.GameOver) matchOver = true;
            else if (current == GameState.Playing) matchOver = false;
        }
    }
}
