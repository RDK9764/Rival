using System;
using UnityEngine;

namespace Rival.Actors
{
    /// <summary>
    /// Component wrapper around the Stamina resource. Any actor (player now,
    /// rival later) and any system (movement, combat, UI) can share one
    /// instance and subscribe to its events. Ticks itself; consumes and
    /// regenerates without knowing who spends the points.
    /// </summary>
    public class StaminaComponent : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float regenPerSecond = 20f;

        private Stamina model;

        public float Current => model.Current;
        public float Max => model.Max;
        public bool RegenerationEnabled { get; set; } = true;

        /// <summary>Invoked with (current, max) whenever the value changes.</summary>
        public event Action<float, float> Changed;

        private void Awake()
        {
            model = new Stamina(maxStamina, regenPerSecond);
            model.Changed += OnModelChanged;
        }

        private void OnDestroy()
        {
            if (model != null) model.Changed -= OnModelChanged;
        }

        private void Update()
        {
            if (!RegenerationEnabled) return;
            model.Tick(Time.deltaTime);
        }

        private void OnModelChanged(float current, float max) => Changed?.Invoke(current, max);

        public bool TrySpend(float amount) => model.TrySpend(amount);

        public void ResetFull() => model.ResetFull();
    }
}
