using UnityEngine;
using Rival.Input;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Bridges the input layer to the game-state layer: pause press → GameManager.
    /// Resolution is lazy (rechecked every frame) so this survives any Awake
    /// ordering between scenes and any late service registration.
    /// </summary>
    public class PauseHandler : MonoBehaviour
    {
        private IPlayerInput input;
        private GameManager gameManager;

        private void Update()
        {
            if (input == null) ServiceLocator.TryGet(out input);
            if (gameManager == null) gameManager = GameManager.Instance;

            if (gameManager != null && input != null && input.ConsumePausePressed())
            {
                gameManager.TogglePause();
            }
        }
    }
}
