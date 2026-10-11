using System;
using System.IO;
using UnityEngine;
using Rival.BehaviorAnalysis;
using Rival.Utilities;

namespace Rival.SaveSystem
{
    /// <summary>
    /// Default IProfileStore: reads and writes one JSON file per actor under
    /// SavePaths. Registers itself as a service so brains and persisters depend
    /// on the interface, not on this implementation.
    /// </summary>
    public class ProfileStore : MonoBehaviour, IProfileStore
    {
        private void Awake() => ServiceLocator.Register<IProfileStore>(this);

        private void OnDestroy() => ServiceLocator.Unregister<IProfileStore>(this);

        public bool TryLoad(string actor, out BehaviorProfile profile)
        {
            profile = null;
            var path = SavePaths.ProfileFile(actor);
            if (!File.Exists(path)) return false;

            try
            {
                var record = JsonUtility.FromJson<ProfileRecord>(File.ReadAllText(path));
                if (record == null) return false;
                profile = record.ToProfile();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProfileStore] Failed to load profile for '{actor}': {e.Message}", this);
                return false;
            }
        }

        public void Save(string actor, BehaviorProfile profile)
        {
            var path = SavePaths.ProfileFile(actor);
            try
            {
                SavePaths.EnsureDirectory(path);
                File.WriteAllText(path, JsonUtility.ToJson(ProfileRecord.From(profile, actor), true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProfileStore] Failed to save profile for '{actor}': {e.Message}", this);
            }
        }
    }
}
