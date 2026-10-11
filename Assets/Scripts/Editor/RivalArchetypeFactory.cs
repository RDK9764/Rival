using UnityEditor;
using UnityEngine;
using Rival.BehaviorAnalysis;

namespace Rival.Editor
{
    /// <summary>
    /// Creates diverse rival archetypes with distinct combat personalities.
    /// Run via Tools → Rival → Create Rival Archetypes. Each archetype has
    /// unique BehaviorProfile traits, RivalPersonality modifiers, and visual
    /// accent colors for easy identification.
    /// </summary>
    public static class RivalArchetypeFactory
    {
        private const string ArchDir = "Assets/ScriptableObjects/Archetypes";

        [MenuItem("Tools/Rival/Create Rival Archetypes")]
        public static void CreateAll()
        {
            EnsureDirectory(ArchDir);

            CreateVanguard();
            CreatePhantom();
            CreateSentinel();
            CreateTempest();
            CreateWarlord();
            CreateTrickster();
            CreateBerserkerV2();
            CreateDefenderV2();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Rival] 8 rival archetypes created.");
        }

        // ── Vanguard: aggressive rush-down, always closing distance ──
        private static void CreateVanguard()
        {
            var a = Create("Vanguard", "Vanguard");
            a.personality.style = CombatStyle.Aggressive;
            a.personality.comboLengthMultiplier = 1.5f;
            a.personality.counterAttackChance = 0.4f;
            a.personality.chaseAggression = 0.9f;
            a.personality.doubleAttackChance = 0.35f;
            a.personality.strafeWhileIdle = false;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.85f, ComboPreference = 0.6f, BlockFrequency = 0.15f,
                DodgeFrequency = 0.15f, ParryFrequency = 0.1f, HeavyAttackChance = 0.25f,
                PreferredRange = 0.25f, ReactionSpeed = 0.6f, RiskTolerance = 0.8f, MovementStyle = 0.3f
            };
            a.LearningBlend = 0.5f;
            a.MovementSpeedMultiplier = 0.95f;
            a.MaxReactionDelay = 0.35f;
            a.AttackCooldown = 0.7f;
            a.MinEngageDistance = 0.8f;
            a.MaxEngageDistance = 3f;
            a.AccentColor = new Color(0.95f, 0.3f, 0.2f);
            Save(a);
        }

        // ── Phantom: evasive hit-and-run, bait-heavy, low commitment ──
        private static void CreatePhantom()
        {
            var a = Create("Phantom", "Phantom");
            a.personality.style = CombatStyle.Evasive;
            a.personality.feintChance = 0.25f;
            a.personality.counterAttackChance = 0.5f;
            a.personality.whiffPunish = 0.6f;
            a.personality.strafeWhileIdle = true;
            a.personality.doubleAttackChance = 0.1f;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.4f, ComboPreference = 0.2f, BlockFrequency = 0.2f,
                DodgeFrequency = 0.7f, ParryFrequency = 0.35f, HeavyAttackChance = 0.15f,
                PreferredRange = 0.6f, ReactionSpeed = 0.85f, RiskTolerance = 0.3f, MovementStyle = 0.8f
            };
            a.LearningBlend = 0.6f;
            a.MovementSpeedMultiplier = 0.9f;
            a.MaxReactionDelay = 0.25f;
            a.AttackCooldown = 0.8f;
            a.MinEngageDistance = 2f;
            a.MaxEngageDistance = 5f;
            a.AccentColor = new Color(0.4f, 0.2f, 0.9f);
            Save(a);
        }

        // ── Sentinel: reactive counter-puncher, patient, punishes aggression ──
        private static void CreateSentinel()
        {
            var a = Create("Sentinel", "Sentinel");
            a.personality.style = CombatStyle.CounterPuncher;
            a.personality.counterAttackChance = 0.7f;
            a.personality.whiffPunish = 0.5f;
            a.personality.chaseAggression = 0.2f;
            a.personality.strafeWhileIdle = true;
            a.personality.doubleAttackChance = 0.15f;
            a.personality.earlyHeavyThreshold = 0.3f;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.3f, ComboPreference = 0.35f, BlockFrequency = 0.55f,
                DodgeFrequency = 0.2f, ParryFrequency = 0.5f, HeavyAttackChance = 0.35f,
                PreferredRange = 0.4f, ReactionSpeed = 0.75f, RiskTolerance = 0.25f, MovementStyle = 0.3f
            };
            a.LearningBlend = 0.65f;
            a.MovementSpeedMultiplier = 0.8f;
            a.MaxReactionDelay = 0.3f;
            a.AttackCooldown = 1.1f;
            a.MinEngageDistance = 1.5f;
            a.MaxEngageDistance = 4f;
            a.AccentColor = new Color(0.2f, 0.7f, 0.9f);
            Save(a);
        }

        // ── Tempest: relentless combo specialist, never stops swinging ──
        private static void CreateTempest()
        {
            var a = Create("Tempest", "Tempest");
            a.personality.style = CombatStyle.ComboSpecialist;
            a.personality.comboLengthMultiplier = 2f;
            a.personality.doubleAttackChance = 0.5f;
            a.personality.counterAttackChance = 0.25f;
            a.personality.strafeWhileIdle = true;
            a.personality.earlyHeavyThreshold = 0f;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.75f, ComboPreference = 0.85f, BlockFrequency = 0.1f,
                DodgeFrequency = 0.1f, ParryFrequency = 0.05f, HeavyAttackChance = 0.15f,
                PreferredRange = 0.3f, ReactionSpeed = 0.55f, RiskTolerance = 0.7f, MovementStyle = 0.5f
            };
            a.LearningBlend = 0.55f;
            a.MovementSpeedMultiplier = 0.88f;
            a.MaxReactionDelay = 0.45f;
            a.AttackCooldown = 0.55f;
            a.MinEngageDistance = 1f;
            a.MaxEngageDistance = 3.5f;
            a.AccentColor = new Color(1f, 0.85f, 0.1f);
            Save(a);
        }

        // ── Warlord: tanky brawler, absorbs hits, uses heavies early ──
        private static void CreateWarlord()
        {
            var a = Create("Warlord", "Warlord");
            a.personality.style = CombatStyle.Defensive;
            a.personality.earlyHeavyThreshold = 0f;
            a.personality.counterAttackChance = 0.5f;
            a.personality.chaseAggression = 0.4f;
            a.personality.strafeWhileIdle = false;
            a.personality.doubleAttackChance = 0.25f;
            a.personality.whiffPunish = 0.2f;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.6f, ComboPreference = 0.4f, BlockFrequency = 0.45f,
                DodgeFrequency = 0.05f, ParryFrequency = 0.2f, HeavyAttackChance = 0.55f,
                PreferredRange = 0.35f, ReactionSpeed = 0.4f, RiskTolerance = 0.6f, MovementStyle = 0.2f
            };
            a.LearningBlend = 0.45f;
            a.MovementSpeedMultiplier = 0.75f;
            a.MaxReactionDelay = 0.55f;
            a.AttackCooldown = 1.2f;
            a.MinEngageDistance = 1f;
            a.MaxEngageDistance = 4f;
            a.AccentColor = new Color(0.6f, 0.3f, 0.1f);
            Save(a);
        }

        // ── Trickster: unpredictable, feints constantly, weird timing ──
        private static void CreateTrickster()
        {
            var a = Create("Trickster", "Trickster");
            a.personality.style = CombatStyle.Unorthodox;
            a.personality.feintChance = 0.4f;
            a.personality.counterAttackChance = 0.3f;
            a.personality.whiffPunish = 0.4f;
            a.personality.strafeWhileIdle = true;
            a.personality.doubleAttackChance = 0.2f;
            a.personality.chaseAggression = 0.6f;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.55f, ComboPreference = 0.3f, BlockFrequency = 0.25f,
                DodgeFrequency = 0.4f, ParryFrequency = 0.3f, HeavyAttackChance = 0.4f,
                PreferredRange = 0.5f, ReactionSpeed = 0.7f, RiskTolerance = 0.65f, MovementStyle = 0.7f
            };
            a.LearningBlend = 0.5f;
            a.MovementSpeedMultiplier = 0.88f;
            a.MaxReactionDelay = 0.4f;
            a.AttackCooldown = 0.85f;
            a.MinEngageDistance = 1.5f;
            a.MaxEngageDistance = 5f;
            a.AccentColor = new Color(0.3f, 0.9f, 0.4f);
            Save(a);
        }

        // ── Berserker V2: aggressive glass cannon, fast, reckless ──
        private static void CreateBerserkerV2()
        {
            var a = Create("Berserker", "Berserker");
            a.personality.style = CombatStyle.Aggressive;
            a.personality.comboLengthMultiplier = 1.3f;
            a.personality.doubleAttackChance = 0.3f;
            a.personality.chaseAggression = 0.8f;
            a.personality.strafeWhileIdle = false;
            a.personality.earlyHeavyThreshold = 0.5f;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.8f, ComboPreference = 0.5f, BlockFrequency = 0.1f,
                DodgeFrequency = 0.2f, ParryFrequency = 0.08f, HeavyAttackChance = 0.35f,
                PreferredRange = 0.3f, ReactionSpeed = 0.65f, RiskTolerance = 0.85f, MovementStyle = 0.35f
            };
            a.LearningBlend = 0.7f;
            a.MovementSpeedMultiplier = 0.9f;
            a.MaxReactionDelay = 0.45f;
            a.AttackCooldown = 0.75f;
            a.MinEngageDistance = 1f;
            a.MaxEngageDistance = 4f;
            a.AccentColor = new Color(0.9f, 0.2f, 0.2f);
            Save(a);
        }

        // ── Defender V2: patient tank, lets the player come to them ──
        private static void CreateDefenderV2()
        {
            var a = Create("Defender", "Defender");
            a.personality.style = CombatStyle.Defensive;
            a.personality.counterAttackChance = 0.6f;
            a.personality.whiffPunish = 0.45f;
            a.personality.chaseAggression = 0.15f;
            a.personality.strafeWhileIdle = false;
            a.personality.earlyHeavyThreshold = 0.2f;
            a.personality.doubleAttackChance = 0.15f;
            a.BaseStyle = new BehaviorProfile
            {
                Aggression = 0.35f, ComboPreference = 0.3f, BlockFrequency = 0.6f,
                DodgeFrequency = 0.15f, ParryFrequency = 0.4f, HeavyAttackChance = 0.3f,
                PreferredRange = 0.45f, ReactionSpeed = 0.6f, RiskTolerance = 0.2f, MovementStyle = 0.25f
            };
            a.LearningBlend = 0.65f;
            a.MovementSpeedMultiplier = 0.8f;
            a.MaxReactionDelay = 0.4f;
            a.AttackCooldown = 1f;
            a.MinEngageDistance = 1.5f;
            a.MaxEngageDistance = 5f;
            a.AccentColor = new Color(0.3f, 0.4f, 0.9f);
            Save(a);
        }

        // ── Helpers ──

        private static RivalArchetype Create(string assetName, string displayName)
        {
            var path = $"{ArchDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RivalArchetype>(path);
            if (existing != null) return existing;

            var archetype = ScriptableObject.CreateInstance<RivalArchetype>();
            archetype.DisplayName = displayName;
            archetype.ActorName = displayName;
            archetype.personality = new RivalPersonality();
            return archetype;
        }

        private static void Save(RivalArchetype archetype)
        {
            var path = $"{ArchDir}/{archetype.ActorName}.asset";
            if (AssetDatabase.LoadAssetAtPath<RivalArchetype>(path) != null) return;
            AssetDatabase.CreateAsset(archetype, path);
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
