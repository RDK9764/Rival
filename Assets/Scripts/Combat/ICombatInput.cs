using System;

namespace Rival.Combat
{
    /// <summary>
    /// Intent-level combat input, independent of hardware. The player's
    /// InputReader implements it for real devices; a TrainingDummyBrain
    /// implements it for testing the AI harness. The brain is the source of
    /// truth — no button presses are ever replayed.
    /// </summary>
    public interface ICombatInput
    {
        bool IsBlockHeld { get; }
        bool IsTargetLockHeld { get; }

        event Action LightAttackPressed;
        event Action HeavyAttackPressed;
        event Action ParryPressed;
        event Action TargetLockPressed;
    }
}
