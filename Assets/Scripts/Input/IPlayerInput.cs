using System;
using UnityEngine;

namespace Rival.Input
{
    /// <summary>
    /// Gameplay-facing input contract. Gameplay code (player, camera) depends on
    /// this, never on UnityEngine.InputSystem. Because the contract carries
    /// *intent* rather than hardware, a bot brain can implement the same contract
    /// later and drive a character with zero input hardware — the Rival enemy
    /// never replays button presses by construction.
    /// </summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        Vector2 Look { get; }
        float ZoomDelta { get; }
        bool IsSprinting { get; }

        event Action JumpPressed;
        event Action DashPressed;
        event Action DodgePressed;

        /// <summary>Returns true exactly once per pause press and consumes it.</summary>
        bool ConsumePausePressed();
    }
}
