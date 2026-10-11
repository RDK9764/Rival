using System;
using UnityEngine;

namespace Rival.Actors
{
    /// <summary>
    /// A drainable, regenerating resource. Gameplay code decides what costs what;
    /// this class only tracks the current value and notifies listeners.
    /// Plain class (not MonoBehaviour) so it is trivially unit-testable.
    /// </summary>
    public class Stamina
    {
        public float Max { get; }
        public float Current { get; private set; }
        public float RegenPerSecond { get; set; }
        public bool IsEmpty => Current <= 0f;

        /// <summary>Invoked with (current, max) whenever the value changes.</summary>
        public event Action<float, float> Changed;

        public Stamina(float max, float regenPerSecond)
        {
            Max = max;
            Current = max;
            RegenPerSecond = regenPerSecond;
        }

        public bool TrySpend(float amount)
        {
            if (Current < amount) return false;

            Current -= amount;
            Changed?.Invoke(Current, Max);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (Current >= Max) return;

            Current = Mathf.Min(Max, Current + RegenPerSecond * deltaTime);
            Changed?.Invoke(Current, Max);
        }

        public void ResetFull()
        {
            Current = Max;
            Changed?.Invoke(Current, Max);
        }
    }
}
