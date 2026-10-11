using UnityEngine;

namespace Rival.Utilities
{
    /// <summary>
    /// Runtime-safe quit helper. A runtime assembly cannot reference
    /// UnityEditor, so the editor can't force-stop play mode from here; in
    /// builds Application.Quit() ends the process. Logs in the editor instead.
    /// </summary>
    public static class GameQuit
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            Debug.Log("[GameQuit] Quit requested (no-op in the editor).");
#else
            Application.Quit();
#endif
        }
    }
}