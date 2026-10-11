using System;
using System.Collections.Generic;
using UnityEngine;
using Rival.Utilities;

namespace Rival.Combat
{
    /// <summary>
    /// Converts raw ICombatInput presses into short-lived buffered intents.
    /// CombatControllers consume these buffers, which gives the player a
    /// forgiving input window and lets AI brains feed the same pipeline.
    /// The source is resolved as: inspector override, a component on this
    /// actor, then the global service locator.
    /// </summary>
    public class CombatInputBridge : MonoBehaviour
    {
        [Tooltip("Optional explicit input source. Leave empty to resolve on this actor or via ServiceLocator.")]
        [SerializeField] private MonoBehaviour inputSource;

        private const float BufferWindow = 0.22f;
        private readonly Queue<(AttackType type, float time)> attackBuffer = new Queue<(AttackType, float)>();
        private float parryBufferTime = -1f;
        private ICombatInput source;

        public bool IsBlockHeld => source != null && source.IsBlockHeld;

        private void Awake()
        {
            source = inputSource as ICombatInput;
            if (source == null) source = GetComponentInParent<ICombatInput>();
            if (source == null && ServiceLocator.TryGet(out ICombatInput found)) source = found;

            if (source == null)
            {
                Debug.LogWarning("[CombatInputBridge] No ICombatInput source found.", this);
                return;
            }

            source.LightAttackPressed += EnqueueLight;
            source.HeavyAttackPressed += EnqueueHeavy;
            source.ParryPressed += EnqueueParry;
        }

        private void OnDestroy()
        {
            if (source == null) return;
            source.LightAttackPressed -= EnqueueLight;
            source.HeavyAttackPressed -= EnqueueHeavy;
            source.ParryPressed -= EnqueueParry;
        }

        private void EnqueueLight() => attackBuffer.Enqueue((AttackType.Light, Time.time));
        private void EnqueueHeavy() => attackBuffer.Enqueue((AttackType.Heavy, Time.time));
        private void EnqueueParry() => parryBufferTime = Time.time;

        public bool TryConsumeAttack(out AttackType type)
        {
            while (attackBuffer.Count > 0)
            {
                var (queuedType, time) = attackBuffer.Dequeue();
                if (Time.time - time <= BufferWindow)
                {
                    type = queuedType;
                    return true;
                }
            }
            type = default;
            return false;
        }

        public bool TryConsumeParry()
        {
            if (parryBufferTime >= 0f && Time.time - parryBufferTime <= BufferWindow)
            {
                parryBufferTime = -1f;
                return true;
            }
            return false;
        }

        public void ClearBuffers()
        {
            attackBuffer.Clear();
            parryBufferTime = -1f;
        }
    }
}
