using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rival.SaveSystem
{
    /// <summary>
    /// Versioned on-disk snapshot of the player's progression: which weapon id
    /// is equipped, which weapon ids are unlocked, and the currency balance.
    /// Weapon identity is plain string ids so the SaveSystem layer never
    /// depends on the Weapons assembly.
    /// </summary>
    [Serializable]
    public class LoadoutRecord
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string equippedWeaponId = "";
        public List<string> unlockedWeaponIds = new List<string>();
        public int currency;
    }

    /// <summary>
    /// Runtime view of the player loadout. Immutable-ish transport between the
    /// store, the menu, and the loadout manager.
    /// </summary>
    public class LoadoutData
    {
        public string EquippedWeaponId;
        public IReadOnlyList<string> UnlockedWeaponIds;
        public int Currency;

        public bool IsUnlocked(string weaponId) =>
            UnlockedWeaponIds != null && UnlockedWeaponIds.Contains(weaponId);
    }
}
