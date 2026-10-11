using System;

namespace Rival.Combat
{
    /// <summary>
    /// Contract for a health pool. Implemented by HealthComponent; future
    /// systems (bosses, destructibles) can implement it the same way.
    /// </summary>
    public interface IHealth
    {
        float Current { get; }
        float Max { get; }
        bool IsDead { get; }

        event Action<float, float> Changed;
        event Action Died;

        void ApplyDamage(float amount);
        void Heal(float amount);
        void ResetFull();
    }
}
