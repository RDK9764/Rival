#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Rival.Combat;

namespace Rival.Editor
{
    /// <summary>
    /// Creates a diverse arsenal of weapons with distinct stats and attack
    /// definitions. Run via Tools → Rival → Create Weapons. Each weapon has
    /// a unique feel: speed, damage, range, stamina costs, and knockback.
    /// </summary>
    public static class WeaponFactory
    {
        private const string WeaponDir = "Assets/ScriptableObjects/Weapons";
        private const string AttackDir = "Assets/ScriptableObjects/Attacks";

        [MenuItem("Tools/Rival/Create Weapons")]
        public static void CreateAll()
        {
            EnsureDirectory(WeaponDir);
            EnsureDirectory(AttackDir);

            CreateSword();
            CreateAxe();
            CreateSpear();
            CreateDagger();
            CreateHammer();
            CreateFist();
            CreateCatalog();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Rival] 6 weapons and their attack sets created.");
        }

        // ── Sword: balanced, all-rounder ──
        private static void CreateSword()
        {
            var w = CreateWeapon("Sword", WeaponType.Sword, 20f, 0.1f, 1.5f, 1.6f, 1f, 1f, 1f, 3f, 1f);
            CreateAttack("Sword_Light", AttackType.Light, 1.0f, 0.18f, 0.12f, 0.28f, 10f, 12f, false, w);
            CreateAttack("Sword_Heavy", AttackType.Heavy, 1.8f, 0.35f, 0.15f, 0.45f, 18f, 20f, true, w);
        }

        // ── Axe: slow, heavy, breaks guard easily ──
        private static void CreateAxe()
        {
            var w = CreateWeapon("Axe", WeaponType.Axe, 25f, 0.08f, 1.6f, 1.4f, 0.8f, 1.2f, 1.5f, 5f, 1.2f);
            CreateAttack("Axe_Light", AttackType.Light, 1.2f, 0.25f, 0.14f, 0.35f, 14f, 18f, false, w);
            CreateAttack("Axe_Heavy", AttackType.Heavy, 2.2f, 0.5f, 0.18f, 0.55f, 24f, 30f, true, w);
        }

        // ── Spear: long reach, moderate speed, weak up close ──
        private static void CreateSpear()
        {
            var w = CreateWeapon("Spear", WeaponType.Spear, 18f, 0.12f, 1.4f, 2.4f, 0.95f, 0.9f, 0.8f, 4f, 1.3f);
            CreateAttack("Spear_Light", AttackType.Light, 0.9f, 0.2f, 0.1f, 0.3f, 9f, 10f, false, w);
            CreateAttack("Spear_Heavy", AttackType.Heavy, 1.6f, 0.4f, 0.12f, 0.5f, 16f, 18f, false, w);
            CreateAttack("Spear_Thrust", AttackType.Heavy, 2.0f, 0.35f, 0.08f, 0.4f, 20f, 22f, true, w);
        }

        // ── Dagger: fast, weak, high crit ──
        private static void CreateDagger()
        {
            var w = CreateWeapon("Dagger", WeaponType.Dagger, 12f, 0.2f, 1.8f, 1.0f, 1.4f, 0.6f, 0.6f, 1.5f, 0.7f);
            CreateAttack("Dagger_Light", AttackType.Light, 0.7f, 0.1f, 0.08f, 0.18f, 6f, 7f, false, w);
            CreateAttack("Dagger_Heavy", AttackType.Heavy, 1.2f, 0.2f, 0.1f, 0.25f, 10f, 12f, false, w);
            CreateAttack("Dagger_Slash", AttackType.Light, 0.8f, 0.08f, 0.06f, 0.15f, 5f, 6f, false, w);
        }

        // ── Hammer: very slow, massive damage, guard break ──
        private static void CreateHammer()
        {
            var w = CreateWeapon("Hammer", WeaponType.Hammer, 35f, 0.05f, 1.8f, 1.3f, 0.65f, 1.5f, 2f, 8f, 1.5f);
            CreateAttack("Hammer_Light", AttackType.Light, 1.5f, 0.4f, 0.2f, 0.6f, 20f, 25f, false, w);
            CreateAttack("Hammer_Heavy", AttackType.Heavy, 3.0f, 0.7f, 0.25f, 0.8f, 35f, 40f, true, w);
        }

        // ── Fist: fast, close range, combo-focused ──
        private static void CreateFist()
        {
            var w = CreateWeapon("Fist", WeaponType.Fist, 10f, 0.08f, 1.4f, 0.8f, 1.3f, 0.5f, 0.5f, 2f, 0.6f);
            CreateAttack("Fist_Light", AttackType.Light, 0.6f, 0.08f, 0.06f, 0.15f, 5f, 5f, false, w);
            CreateAttack("Fist_Heavy", AttackType.Heavy, 1.0f, 0.18f, 0.08f, 0.22f, 9f, 10f, false, w);
            CreateAttack("Fist_Combo", AttackType.Light, 0.5f, 0.06f, 0.05f, 0.12f, 4f, 4f, false, w);
        }

        // ── Helpers ──

        private static void CreateCatalog()
        {
            var path = $"{WeaponDir}/WeaponCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            var serialized = new UnityEditor.SerializedObject(catalog);
            var entries = serialized.FindProperty("entries");
            entries.ClearArray();
            AddCatalogEntry(entries, "Sword", "Sword_Light", "Sword_Heavy");
            AddCatalogEntry(entries, "Axe", "Axe_Light", "Axe_Heavy");
            AddCatalogEntry(entries, "Spear", "Spear_Light", "Spear_Heavy", "Spear_Thrust");
            AddCatalogEntry(entries, "Dagger", "Dagger_Light", "Dagger_Heavy", "Dagger_Slash");
            AddCatalogEntry(entries, "Hammer", "Hammer_Light", "Hammer_Heavy");
            AddCatalogEntry(entries, "Fist", "Fist_Light", "Fist_Heavy", "Fist_Combo");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddCatalogEntry(UnityEditor.SerializedProperty entries, string weaponId, params string[] attackNames)
        {
            entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);

            entry.FindPropertyRelative("id").stringValue = weaponId;
            entry.FindPropertyRelative("weapon").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Weapon>($"{WeaponDir}/{weaponId}.asset");

            var chain = entry.FindPropertyRelative("comboChain");
            chain.arraySize = attackNames.Length;
            for (var i = 0; i < attackNames.Length; i++)
            {
                chain.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AttackDefinition>($"{AttackDir}/{attackNames[i]}.asset");
            }
        }

        private static Weapon CreateWeapon(string name, WeaponType type, float damage, float crit,
            float critMul, float range, float speed, float stamina, float blockStamina, float knockback, float trail)
        {
            var path = $"{WeaponDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Weapon>(path);
            if (existing != null) return existing;

            var weapon = ScriptableObject.CreateInstance<Weapon>();
            weapon.Init(name, type, damage, crit, critMul, range, speed, stamina, blockStamina, knockback, trail);
            AssetDatabase.CreateAsset(weapon, path);
            return weapon;
        }

        private static void CreateAttack(string name, AttackType type, float dmg, float windup,
            float active, float recovery, float stamina, float blockStamina, bool breaks, Weapon weapon)
        {
            var path = $"{AttackDir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<AttackDefinition>(path) != null) return;

            var attack = ScriptableObject.CreateInstance<AttackDefinition>();
            attack.Init(name, type, dmg, windup, active, recovery, stamina, blockStamina, breaks);
            AssetDatabase.CreateAsset(attack, path);
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                var folder = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent)) EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
