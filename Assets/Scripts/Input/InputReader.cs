using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Rival.Combat;
using Rival.Utilities;

namespace Rival.Input
{
    /// <summary>
    /// The only component allowed to touch UnityEngine.InputSystem. Converts raw
    /// actions into intent events on IPlayerInput, ICombatInput and IMenuInput.
    /// Registered as a service so controllers, cameras and menus find it without
    /// inspector wiring. Owns two action maps (Gameplay and Menu) so a menu can
    /// suppress gameplay input while it is open.
    /// </summary>
    public class InputReader : MonoBehaviour, IPlayerInput, ICombatInput, IMenuInput
    {
        public Vector2 Move => moveAction.ReadValue<Vector2>();
        public Vector2 Look => lookAction.ReadValue<Vector2>();
        public float ZoomDelta { get; private set; }
        public bool IsSprinting => sprintAction.ReadValue<float>() > 0.5f;

        public bool IsBlockHeld => blockAction.ReadValue<float>() > 0.5f;
        public bool IsTargetLockHeld => targetLocked;

        public event Action JumpPressed;
        public event Action DashPressed;
        public event Action DodgePressed;

        public event Action LightAttackPressed;
        public event Action HeavyAttackPressed;
        public event Action ParryPressed;
        public event Action TargetLockPressed;

        public Vector2 Navigate => menuNavigateAction.ReadValue<Vector2>();
        public event Action ConfirmPressed;
        public event Action BackPressed;

        private InputActionAsset asset;
        private InputActionMap gameplayMap;
        private InputActionMap menuMap;
        private InputAction menuNavigateAction;
        private InputAction menuConfirmAction;
        private InputAction menuCancelAction;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction dashAction;
        private InputAction dodgeAction;
        private InputAction pauseAction;
        private InputAction lightAttackAction;
        private InputAction heavyAttackAction;
        private InputAction blockAction;
        private InputAction parryAction;
        private InputAction targetLockAction;
        private bool pauseQueued;
        private bool targetLocked;
        private float lastScrollY;

        private void Awake()
        {
            BuildActions();
            ServiceLocator.Register<IPlayerInput>(this);
            ServiceLocator.Register<IMenuInput>(this);
        }

        private void OnEnable()
        {
            asset.Enable();
            gameplayMap.Enable();
            menuMap.Disable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            asset.Disable();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IPlayerInput>(this);
            ServiceLocator.Unregister<IMenuInput>(this);
        }

        /// <summary>Menu mode suppresses gameplay input and vice-versa.</summary>
        public void SetMenuActive(bool active)
        {
            if (active)
            {
                gameplayMap.Disable();
                menuMap.Enable();
            }
            else
            {
                menuMap.Disable();
                gameplayMap.Enable();
            }
        }

        private void Update()
        {
            var scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            ZoomDelta = scroll - lastScrollY;
            lastScrollY = scroll;
        }

        public bool ConsumePausePressed()
        {
            var queued = pauseQueued;
            pauseQueued = false;
            return queued;
        }

        private void BuildActions()
        {
            asset = new InputActionAsset();
            gameplayMap = asset.AddActionMap("Gameplay");
            var gameplay = gameplayMap;

            moveAction = gameplay.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Gamepad>/dpad/up")
                .With("Down", "<Gamepad>/dpad/down")
                .With("Left", "<Gamepad>/dpad/left")
                .With("Right", "<Gamepad>/dpad/right");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookAction = gameplay.AddAction("Look", InputActionType.Value);
            lookAction.AddBinding("<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jumpAction = gameplay.AddAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            sprintAction = gameplay.AddAction("Sprint", InputActionType.Button);
            sprintAction.AddBinding("<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");

            dashAction = gameplay.AddAction("Dash", InputActionType.Button);
            dashAction.AddBinding("<Keyboard>/q");
            dashAction.AddBinding("<Gamepad>/buttonWest");

            dodgeAction = gameplay.AddAction("Dodge", InputActionType.Button);
            dodgeAction.AddBinding("<Keyboard>/leftCtrl");
            dodgeAction.AddBinding("<Gamepad>/buttonEast");

            pauseAction = gameplay.AddAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");

            lightAttackAction = gameplay.AddAction("LightAttack", InputActionType.Button);
            lightAttackAction.AddBinding("<Mouse>/leftButton");
            lightAttackAction.AddBinding("<Keyboard>/f");
            lightAttackAction.AddBinding("<Gamepad>/rightShoulder");

            heavyAttackAction = gameplay.AddAction("HeavyAttack", InputActionType.Button);
            heavyAttackAction.AddBinding("<Mouse>/rightButton");
            heavyAttackAction.AddBinding("<Keyboard>/g");
            heavyAttackAction.AddBinding("<Gamepad>/rightTrigger");

            blockAction = gameplay.AddAction("Block", InputActionType.Button);
            blockAction.AddBinding("<Keyboard>/r");
            blockAction.AddBinding("<Gamepad>/leftTrigger");

            parryAction = gameplay.AddAction("Parry", InputActionType.Button);
            parryAction.AddBinding("<Keyboard>/e");
            parryAction.AddBinding("<Gamepad>/buttonNorth");

            targetLockAction = gameplay.AddAction("TargetLock", InputActionType.Button);
            targetLockAction.AddBinding("<Keyboard>/tab");
            targetLockAction.AddBinding("<Gamepad>/rightStickPress");

            jumpAction.performed += _ => JumpPressed?.Invoke();
            dashAction.performed += _ => DashPressed?.Invoke();
            dodgeAction.performed += _ => DodgePressed?.Invoke();
            pauseAction.performed += _ => pauseQueued = true;
            lightAttackAction.performed += _ => LightAttackPressed?.Invoke();
            heavyAttackAction.performed += _ => HeavyAttackPressed?.Invoke();
            parryAction.performed += _ => ParryPressed?.Invoke();
            targetLockAction.performed += _ =>
            {
                targetLocked = !targetLocked;
                TargetLockPressed?.Invoke();
            };

            menuMap = asset.AddActionMap("Menu");
            menuNavigateAction = menuMap.AddAction("Navigate", InputActionType.Value);
            menuNavigateAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow")
                .With("Up", "<Gamepad>/dpad/up")
                .With("Down", "<Gamepad>/dpad/down")
                .With("Left", "<Gamepad>/dpad/left")
                .With("Right", "<Gamepad>/dpad/right");
            menuNavigateAction.AddBinding("<Gamepad>/leftStick");

            menuConfirmAction = menuMap.AddAction("Confirm", InputActionType.Button);
            menuConfirmAction.AddBinding("<Keyboard>/enter");
            menuConfirmAction.AddBinding("<Keyboard>/space");
            menuConfirmAction.AddBinding("<Gamepad>/buttonSouth");
            menuConfirmAction.performed += _ => ConfirmPressed?.Invoke();

            menuCancelAction = menuMap.AddAction("Back", InputActionType.Button);
            menuCancelAction.AddBinding("<Keyboard>/escape");
            menuCancelAction.AddBinding("<Gamepad>/buttonEast");
            menuCancelAction.performed += _ => BackPressed?.Invoke();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            menuMap.Disable();
            gameplayMap.Enable();
        }
    }
}
