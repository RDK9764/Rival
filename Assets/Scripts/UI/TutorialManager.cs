using System;
using UnityEngine;
using Rival.Core;
using Rival.Combat;
using Rival.Utilities;

namespace Rival.UI
{
    public enum TutorialStep
    {
        None,
        Welcome,
        Movement,
        LightAttack,
        HeavyAttack,
        Block,
        Parry,
        Dodge,
        TargetLock,
        Complete
    }

    /// <summary>
    /// Progressive tutorial that teaches combat mechanics step by step.
    /// Subscribes to input and combat events to detect when the player
    /// has performed each action. Shows contextual prompts via TutorialUI.
    /// Completes automatically when all steps are done; persists completion
    /// to PlayerPrefs so it only shows once.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 3f)] private float promptDelay = 1f;
        [SerializeField] private bool forceTutorial = false;

        private IPlayerInput playerInput;
        private ICombatInput combatInput;
        private IMenuInput menuInput;
        private GameManager gameManager;

        private TutorialUI tutorialUI;
        private TutorialStep currentStep = TutorialStep.None;
        private float stepTimer;
        private bool isActive;
        private bool hasCompleted;

        private static readonly string CompletedKey = "Rival_Tutorial_Completed";

        private void Start()
        {
            ServiceLocator.TryGet(out playerInput);
            ServiceLocator.TryGet(out combatInput);
            ServiceLocator.TryGet(out menuInput);
            gameManager = GameManager.Instance;

            hasCompleted = PlayerPrefs.GetInt(CompletedKey, 0) == 1;

            tutorialUI = GetComponent<TutorialUI>();
            if (tutorialUI == null) tutorialUI = gameObject.AddComponent<TutorialUI>();
            tutorialUI.Hide();

            // Endless is raw action; the teaching run only belongs to Match mode.
            if ((!hasCompleted || forceTutorial) && !IsEndlessMode())
            {
                if (gameManager != null) gameManager.GameStateChanged += OnGameStateChanged;
            }
        }

        private bool IsEndlessMode()
        {
            return ServiceLocator.TryGet<IMatchConfig>(out var config)
                && config.GameMode == RivalGameMode.Endless;
        }

        private void OnDestroy()
        {
            if (gameManager != null) gameManager.GameStateChanged -= OnGameStateChanged;
            UnsubscribeEvents();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Playing && !isActive && (!hasCompleted || forceTutorial))
            {
                StartTutorial();
            }
        }

        private void Update()
        {
            if (!isActive) return;

            stepTimer -= Time.unscaledDeltaTime;
            if (stepTimer > 0f) return;

            switch (currentStep)
            {
                case TutorialStep.Welcome:
                    if (menuInput != null && menuInput.ConfirmPressed != null)
                        Advance();
                    break;
                case TutorialStep.Movement:
                    if (playerInput != null && playerInput.Move.sqrMagnitude > 0.1f)
                        Advance();
                    break;
                case TutorialStep.LightAttack:
                    // Will advance via event
                    break;
                case TutorialStep.HeavyAttack:
                    // Will advance via event
                    break;
                case TutorialStep.Block:
                    if (combatInput != null && combatInput.IsBlockHeld)
                        Advance();
                    break;
                case TutorialStep.Parry:
                    // Will advance via event
                    break;
                case TutorialStep.Dodge:
                    // Will advance via event
                    break;
                case TutorialStep.TargetLock:
                    if (combatInput != null && combatInput.IsTargetLockHeld)
                        Advance();
                    break;
            }
        }

        private void StartTutorial()
        {
            isActive = true;
            SubscribeEvents();
            AdvanceTo(TutorialStep.Welcome);
        }

        private void SubscribeEvents()
        {
            if (combatInput != null)
            {
                combatInput.LightAttackPressed += OnLightAttack;
                combatInput.HeavyAttackPressed += OnHeavyAttack;
                combatInput.ParryPressed += OnParry;
            }
            if (playerInput != null)
            {
                playerInput.DodgePressed += OnDodge;
            }
        }

        private void UnsubscribeEvents()
        {
            if (combatInput != null)
            {
                combatInput.LightAttackPressed -= OnLightAttack;
                combatInput.HeavyAttackPressed -= OnHeavyAttack;
                combatInput.ParryPressed -= OnParry;
            }
            if (playerInput != null)
            {
                playerInput.DodgePressed -= OnDodge;
            }
        }

        private void OnLightAttack()
        {
            if (currentStep == TutorialStep.LightAttack) Advance();
        }

        private void OnHeavyAttack()
        {
            if (currentStep == TutorialStep.HeavyAttack) Advance();
        }

        private void OnParry()
        {
            if (currentStep == TutorialStep.Parry) Advance();
        }

        private void OnDodge()
        {
            if (currentStep == TutorialStep.Dodge) Advance();
        }

        private void Advance()
        {
            if (currentStep == TutorialStep.Complete)
            {
                CompleteTutorial();
                return;
            }
            currentStep++;
            ShowCurrentStep();
        }

        private void AdvanceTo(TutorialStep step)
        {
            currentStep = step;
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            stepTimer = promptDelay;
            switch (currentStep)
            {
                case TutorialStep.Welcome:
                    tutorialUI.Show("Welcome to RIVAL. Press any button to begin.", "");
                    break;
                case TutorialStep.Movement:
                    tutorialUI.Show("Move: WASD or Left Stick", "Move around the arena");
                    break;
                case TutorialStep.LightAttack:
                    tutorialUI.Show("Light Attack: Left Click / RB / F", "Hit your opponent");
                    break;
                case TutorialStep.HeavyAttack:
                    tutorialUI.Show("Heavy Attack: Right Click / RT / G", "Break their guard");
                    break;
                case TutorialStep.Block:
                    tutorialUI.Show("Block: Hold R / LT", "Reduce incoming damage");
                    break;
                case TutorialStep.Parry:
                    tutorialUI.Show("Parry: Tap E / Y just before hit", "Deflect and stun");
                    break;
                case TutorialStep.Dodge:
                    tutorialUI.Show("Dodge: Ctrl / B", "Invincibility frames");
                    break;
                case TutorialStep.TargetLock:
                    tutorialUI.Show("Target Lock: Tab / Right Stick Press", "Lock onto opponent");
                    break;
                case TutorialStep.Complete:
                    tutorialUI.Show("Tutorial complete! Fight!", "");
                    break;
            }
        }

        private void CompleteTutorial()
        {
            isActive = false;
            hasCompleted = true;
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            UnsubscribeEvents();
            tutorialUI.Hide();
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }
    }
}
