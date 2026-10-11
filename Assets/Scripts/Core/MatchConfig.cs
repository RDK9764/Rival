using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Persistent match configuration. Lives on the BootSystems GameObject in
    /// the Boot scene so it survives every scene transition. The main menu
    /// writes SelectedIndex; the gameplay scene reads it in Awake.
    /// </summary>
    public class MatchConfig : MonoBehaviour, IMatchConfig
    {
        public int SelectedIndex { get; set; }
        public RivalGameMode GameMode { get; set; } = RivalGameMode.Match;

        private void Awake()
        {
            ServiceLocator.Register<IMatchConfig>(this);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IMatchConfig>(this);
        }
    }
}
