using System;
using System.Collections.Generic;
using UnityEngine;
using Rival.Combat;
using Rival.Core;
using Rival.SaveSystem;
using Rival.Utilities;

namespace Rival.Enemy
{
    /// <summary>
    /// Owning controller for the player's progression (weapon loadout +
    /// currency) and the rival's mirror. Reads/writes through ILoadoutStore,
    /// applies the equipped weapon to the player's CombatController at match
    /// start, awards currency from match outcomes, and unlocks weapons for a
    /// spend. The rival is a live copy: it is re-equipped with the same weapon
    /// and rebuilds its procedural model to match, so you literally fight
    /// yourself. Registered as a ServiceLocator service so the loadout menu can
    /// read/change it without a direct dependency on the SaveSystem layer.
    /// </summary>
    public class PlayerLoadoutManager : MonoBehaviour
    {
        [Tooltip("All selectable weapons. Resolves equipped/unlock by id.")]
        [SerializeField] private WeaponCatalog catalog;
        [Tooltip("The player's combat controller to equip during a match.")]
        [SerializeField] private CombatController playerCombat;
        [Tooltip("The rival's combat controller, mirrored to the player's weapon.")]
        [SerializeField] private CombatController rivalCombat;
        [Tooltip("The rival's procedural model, rebuilt to mirror the player's weapon.")]
        [SerializeField] private ProceduralFighterBuilder rivalBuilder;
        [Tooltip("Rival body color used when rebuilding the mirrored model.")]
        [SerializeField] private Color rivalBodyColor = new Color(0.25f, 0.08f, 0.08f);
        [Tooltip("Rival accent color used when rebuilding the mirrored model.")]
        [SerializeField] private Color rivalAccentColor = new Color(0.9f, 0.2f, 0.2f);
        [Tooltip("The player's procedural model, rebuilt when a new weapon is equipped.")]
        [SerializeField] private ProceduralFighterBuilder playerBuilder;
        [Tooltip("Player body color used when rebuilding their model.")]
        [SerializeField] private Color playerBodyColor = new Color(0.2f, 0.5f, 0.9f);
        [Tooltip("Player accent color used when rebuilding their model.")]
        [SerializeField] private Color playerAccentColor = new Color(0.3f, 0.7f, 1f);
        [Tooltip("Time (s) after match start before the equipped weapon is applied.")]
        [SerializeField] private float equipDelay = 0.1f;

        [Serializable]
        public struct UnlockCost
        {
            public string weaponId;
            public int cost;
        }
        [Tooltip("Base unlock cost per weapon id.")]
        [SerializeField] private UnlockCost[] unlockCosts;

        private ILoadoutStore store;
        private LoadoutData data;
        private float equipTimer = -1f;

        public event Action Changed;

        public string EquippedWeaponId => data != null ? data.EquippedWeaponId : null;
        public int Currency => data != null ? data.Currency : 0;
        public int SessionEarnings { get; private set; }
        public IReadOnlyList<string> UnlockedWeaponIds => data != null ? data.UnlockedWeaponIds : null;

        private void Awake()
        {
            ServiceLocator.Register<PlayerLoadoutManager>(this);
        }

        private void OnDestroy() => ServiceLocator.Unregister<PlayerLoadoutManager>(this);

        private void Start()
        {
            ServiceLocator.TryGet(out store);
            ReloadFromStore();
            if (catalog == null) ServiceLocator.TryGet(out catalog);
            equipTimer = equipDelay;
            SessionEarnings = 0;
        }

        private void Update()
        {
            if (equipTimer < 0f) return;
            equipTimer -= Time.deltaTime;
            if (equipTimer <= 0f)
            {
                equipTimer = -1f;
                ApplyEquippedWeapon();
            }
        }

        // ---- public loadout API ----

        public WeaponCatalogEntry GetCatalogEntry(string weaponId) => catalog != null ? catalog.Find(weaponId) : null;

        public IEnumerable<WeaponCatalogEntry> AllCatalogWeapons() => catalog != null ? catalog.Entries : (IEnumerable<WeaponCatalogEntry>)Array.Empty<WeaponCatalogEntry>();

        public bool IsUnlocked(string weaponId) => data != null && data.IsUnlocked(weaponId);

        public int UnlockCostFor(string weaponId)
        {
            if (unlockCosts == null) return 0;
            foreach (var cost in unlockCosts)
            {
                if (string.Equals(cost.weaponId, weaponId, StringComparison.OrdinalIgnoreCase))
                    return cost.cost;
            }
            return 0;
        }

        /// <summary>Attempts to unlock a weapon for its cost. True on success.</summary>
        public bool TryUnlock(string weaponId)
        {
            if (data == null || data.IsUnlocked(weaponId)) return false;
            var cost = UnlockCostFor(weaponId);
            if (data.Currency < cost) return false;

            data.Currency -= cost;
            ((List<string>)data.UnlockedWeaponIds).Add(weaponId);
            PersistAndNotify();
            return true;
        }

        /// <summary>Equips an already-unlocked weapon by id. True on success.</summary>
        public bool TryEquip(string weaponId)
        {
            if (data == null || !data.IsUnlocked(weaponId)) return false;
            data.EquippedWeaponId = weaponId;
            PersistAndNotify();
            ApplyEquippedWeapon();
            return true;
        }

        /// <summary>Awards currency (e.g. from a won match or endless waves).</summary>
        public void AwardCurrency(int amount)
        {
            if (data == null || amount <= 0) return;
            data.Currency += amount;
            SessionEarnings += amount;
            PersistAndNotify();
        }

        /// <summary>Finds the cheapest still-locked weapon, for the game-over hint. True if any exist.</summary>
        public bool TryGetCheapestLocked(out string weaponId, out int cost)
        {
            weaponId = null;
            cost = int.MaxValue;
            if (catalog == null || data == null) return false;

            foreach (var entry in catalog.Entries)
            {
                if (entry == null || data.IsUnlocked(entry.id)) continue;
                var c = UnlockCostFor(entry.id);
                if (c < cost)
                {
                    cost = c;
                    weaponId = entry.id;
                }
            }
            return !string.IsNullOrEmpty(weaponId);
        }

        // ---- internals ----

        private void ReloadFromStore()
        {
            if (store != null) data = store.Load();
            if (data == null) data = defaultLoadout;
        }

        private static readonly LoadoutData defaultLoadout = new LoadoutData
        {
            EquippedWeaponId = LoadoutStore.DefaultEquippedWeapon,
            UnlockedWeaponIds = new List<string> { LoadoutStore.DefaultEquippedWeapon },
            Currency = 0
        };

        private void PersistAndNotify()
        {
            store?.Save(data);
            Changed?.Invoke();
        }

        private void ApplyEquippedWeapon()
        {
            if (playerCombat == null || data == null) return;

            var entry = GetCatalogEntry(data.EquippedWeaponId);
            if (entry == null || entry.weapon == null) return;
            playerCombat.Equip(entry.weapon, entry.comboChain);
            if (playerBuilder != null) playerBuilder.Build(entry.weapon, playerBodyColor, playerAccentColor);

            // The rival is a live copy: give it the same weapon and rebuild
            // its procedural model so it visibly wields what you wield.
            if (rivalCombat != null) rivalCombat.Equip(entry.weapon, entry.comboChain);
            if (rivalBuilder != null) rivalBuilder.Build(entry.weapon, rivalBodyColor, rivalAccentColor);
        }
    }
}
