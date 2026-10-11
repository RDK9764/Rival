using System;
using UnityEngine;

namespace Rival.Input
{
    /// <summary>
    /// Menu-facing input contract. Gameplay menus (character select, future
    /// pause menu) consume this instead of IPlayerInput, so menu navigation is
    /// an explicit input mode the input layer owns. Only InputReader reads the
    /// hardware; everything else depends on this intent.
    /// </summary>
    public interface IMenuInput
    {
        /// <summary>Directional navigation (menu map), y = up/down, x = left/right.</summary>
        Vector2 Navigate { get; }

        /// <summary>Fired when the menu confirm button is pressed.</summary>
        event Action ConfirmPressed;

        /// <summary>Fired when the player presses back/cancel (Esc / gamepad East).</summary>
        event Action BackPressed;

        /// <summary>Swaps between gameplay and menu input maps. Only one is active at a time.</summary>
        void SetMenuActive(bool active);
    }
}
