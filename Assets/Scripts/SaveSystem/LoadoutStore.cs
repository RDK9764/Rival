using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Rival.SaveSystem
{
    /// <summary>
    /// Default file-backed ILoadoutStore. Reads/writes one JSON file via
    /// SavePaths. Provides the default starting state (Sword equipped, only
    /// Sword unlocked) when no save exists yet.
    /// </summary>
    public class LoadoutStore : MonoBehaviour, ILoadoutStore
    {
        public const string DefaultEquippedWeapon = "Sword";

        private LoadoutRecord cached;

        private void Awake()
        {
            ServiceLocator.Register<ILoadoutStore>(this);
            var data = Load();
            cached = ToRecord(data);
        }

        private void OnDestroy() => ServiceLocator.Unregister<ILoadoutStore>(this);

        public LoadoutData Load()
        {
            if (cached != null) return FromRecord(cached);

            var path = SavePaths.LoadoutFile();
            if (!File.Exists(path)) return DefaultData();

            try
            {
                var record = JsonUtility.FromJson<LoadoutRecord>(File.ReadAllText(path));
                if (record == null) return DefaultData();
                return FromRecord(record);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoadoutStore] Failed to load loadout: {e.Message}");
                return DefaultData();
            }
        }

        public void Save(LoadoutData data)
        {
            cached = ToRecord(data);
            var path = SavePaths.LoadoutFile();
            try
            {
                SavePaths.EnsureDirectory(path);
                File.WriteAllText(path, JsonUtility.ToJson(cached, true));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoadoutStore] Failed to save loadout: {e.Message}");
            }
        }

        private static LoadoutData DefaultData()
        {
            return new LoadoutData
            {
                EquippedWeaponId = DefaultEquippedWeapon,
                UnlockedWeaponIds = new List<string> { DefaultEquippedWeapon },
                Currency = 0
            };
        }

        private static LoadoutRecord ToRecord(LoadoutData data)
        {
            return new LoadoutRecord
            {
                schemaVersion = LoadoutRecord.CurrentSchemaVersion,
                equippedWeaponId = data.EquippedWeaponId,
                unlockedWeaponIds = data.UnlockedWeaponIds != null
                    ? new List<string>(data.UnlockedWeaponIds)
                    : new List<string>(),
                currency = data.Currency
            };
        }

        private static LoadoutData FromRecord(LoadoutRecord record)
        {
            return new LoadoutData
            {
                EquippedWeaponId = string.IsNullOrEmpty(record.equippedWeaponId)
                    ? DefaultEquippedWeapon
                    : record.equippedWeaponId,
                UnlockedWeaponIds = record.unlockedWeaponIds != null
                    ? new List<string>(record.unlockedWeaponIds)
                    : new List<string>(),
                Currency = record.currency
            };
        }
    }
}
