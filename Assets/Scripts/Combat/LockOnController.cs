using System;
using UnityEngine;
using Rival.Utilities;

namespace Rival.Combat
{
    /// <summary>
    /// Turns on facing toward a target when the actor's combat input says the
    /// target is locked. Player lock is toggled by input; an AI brain simply
    /// holds IsTargetLockHeld = true to keep fighting its current target.
    /// </summary>
    public class LockOnController : MonoBehaviour
    {
        [Tooltip("Optional explicit input source. Leave empty to resolve on this actor or via ServiceLocator.")]
        [SerializeField] private MonoBehaviour inputSource;
        [Tooltip("Target to face while locked. Assigned in scene or at runtime.")]
        [SerializeField] private Transform target;
        [Tooltip("How fast the actor turns toward the target, in degrees per second.")]
        [SerializeField] private float rotationSpeed = 720f;

        private ICombatInput source;

        public bool IsLocked { get; private set; }
        public Transform Target => target;

        public event Action<bool> LockStateChanged;

        private void Awake()
        {
            source = inputSource as ICombatInput;
            if (source == null) source = GetComponentInParent<ICombatInput>();
            if (source == null && ServiceLocator.TryGet(out ICombatInput found)) source = found;
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Update()
        {
            var targetInvalid = target == null;
            var desired = source != null && source.IsTargetLockHeld && !targetInvalid;

            if (desired != IsLocked)
            {
                IsLocked = desired;
                LockStateChanged?.Invoke(IsLocked);
            }

            if (!IsLocked || targetInvalid) return;

            var direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.01f) return;

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
