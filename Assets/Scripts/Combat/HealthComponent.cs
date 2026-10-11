using System;
using UnityEngine;

namespace Rival.Combat
{
    /// <summary>
    /// A pure health pool with no combat rules. Emits Changed (current, max)
    /// on any change and Died once. UI and telemetry subscribe to these events.
    /// </summary>
    public class HealthComponent : MonoBehaviour, IHealth
    {
        [SerializeField] private float maxHealth = 100f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsDead => Current <= 0f;

        public event Action<float, float> Changed;
        public event Action Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke(Current, Max);
            if (Current <= 0f) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Min(Max, Current + amount);
            Changed?.Invoke(Current, Max);
        }

        public void ResetFull()
        {
            Current = maxHealth;
            Changed?.Invoke(Current, Max);
        }
    }
}
