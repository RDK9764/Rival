using System;
using System.Collections;
using UnityEngine;

namespace Rival.Combat
{
    /// <summary>
    /// A scripted ICombatInput used to test the player against a living target.
    /// The dummy blocks in windows, occasionally parries, and attacks back on a
    /// timer. It intentionally does not react to the player's actions — Milestone
    /// 3+ rivals will use telemetry-driven behavior analysis instead.
    /// </summary>
    public class TrainingDummyBrain : MonoBehaviour, ICombatInput
    {
        [Header("Blocking")]
        [Tooltip("How long the dummy holds its guard.")]
        [SerializeField] private float blockDuration = 1.5f;
        [Tooltip("Pause between block windows.")]
        [SerializeField] private float blockGap = 0.8f;

        [Header("Parry")]
        [Tooltip("Chance per parry chance that the dummy attempts a parry.")]
        [SerializeField, Range(0f, 1f)] private float parryChance = 0.25f;
        [Tooltip("Cooldown between parry attempts.")]
        [SerializeField] private float parryCooldown = 2.5f;

        [Header("Attacks")]
        [Tooltip("Whether the dummy attacks back at all.")]
        [SerializeField] private bool attacksBack = true;
        [Tooltip("Time between dummy attacks.")]
        [SerializeField] private float attackInterval = 2.2f;
        [Tooltip("Chance that a dummy attack is a heavy.")]
        [SerializeField, Range(0f, 1f)] private float heavyChance = 0.2f;

        public bool IsBlockHeld { get; private set; }
        public bool IsTargetLockHeld => true;

        public event Action LightAttackPressed;
        public event Action HeavyAttackPressed;
        public event Action ParryPressed;
        public event Action TargetLockPressed;

        private void Start()
        {
            StartCoroutine(BlockLoop());
            StartCoroutine(ParryLoop());
            StartCoroutine(AttackLoop());
        }

        private IEnumerator BlockLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(blockGap);
                IsBlockHeld = true;
                yield return new WaitForSeconds(blockDuration);
                IsBlockHeld = false;
            }
        }

        private IEnumerator ParryLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(parryCooldown);
                if (UnityEngine.Random.value < parryChance) ParryPressed?.Invoke();
            }
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(attackInterval);
                if (!attacksBack) continue;
                if (UnityEngine.Random.value < heavyChance) HeavyAttackPressed?.Invoke();
                else LightAttackPressed?.Invoke();
            }
        }
    }
}
