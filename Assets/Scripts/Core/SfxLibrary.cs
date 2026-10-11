using UnityEngine;

namespace Rival.Core
{
    /// <summary>
    /// ScriptableObject that maps SfxCategory keys to AudioClip assets.
    /// One asset shared across the project; AudioService resolves it at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Rival/Audio/SFX Library")]
    public class SfxLibrary : ScriptableObject
    {
        [SerializeField] private SfxEntry[] entries;

        [System.Serializable]
        public struct SfxEntry
        {
            public SfxCategory category;
            public AudioClip clip;
        }

        public AudioClip GetClip(SfxCategory category)
        {
            if (entries == null) return null;
            foreach (var entry in entries)
            {
                if (entry.category == category) return entry.clip;
            }
            return null;
        }
    }
}
