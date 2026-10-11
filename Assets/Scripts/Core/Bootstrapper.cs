using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Lives in the Boot scene. Once all boot systems have registered
    /// themselves (GameManager, SceneLoader), it kicks off the first scene.
    /// This keeps "how the game starts" in one visible, explicit place.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private string startupScene = SceneRegistry.MainMenu;

        private void Start()
        {
            if (!ServiceLocator.TryGet<ISceneLoader>(out var sceneLoader))
            {
                Debug.LogError("[Bootstrapper] No ISceneLoader registered. Add a SceneLoader to the Boot scene.");
                return;
            }

            sceneLoader.LoadAsync(startupScene);
        }
    }
}
