using UnityEngine;
using Rival.BehaviorAnalysis;

namespace Rival.Enemy
{
    /// <summary>
    /// Combat personality modifiers that go beyond BehaviorProfile traits.
    /// These are intrinsic to the archetype and never change — they define
    /// the rival's fighting "soul" while BehaviorProfile defines learned behavior.
    /// </summary>
    public enum CombatStyle
    {
        Balanced,
        Aggressive,
        Defensive,
        Evasive,
        CounterPuncher,
        ComboSpecialist,
        Unorthodox
    }

    [System.Serializable]
    public class RivalPersonality
    {
        [Tooltip("How this rival tends to fight — overrides default decision weights.")]
        public CombatStyle style = CombatStyle.Balanced;

        [Tooltip("Multiplier on combo chain length preference. 1 = normal, 2 = always full combo.")]
        [Range(0.5f, 2f)] public float comboLengthMultiplier = 1f;

        [Tooltip("How often this rival feints (starts attack then cancels into dodge/block).")]
        [Range(0f, 0.5f)] public float feintChance = 0f;

        [Tooltip("How likely to counter-attack immediately after a successful block/parry.")]
        [Range(0f, 1f)] public float counterAttackChance = 0.3f;

        [Tooltip("Whether this rival strafes while idle (true) or holds ground (false).")]
        public bool strafeWhileIdle = true;

        [Tooltip("How aggressively this rival closes distance when the player retreats.")]
        [Range(0f, 1f)] public float chaseAggression = 0.5f;

        [Tooltip("Probability of chaining a second attack after landing the first.")]
        [Range(0f, 1f)] public float doubleAttackChance = 0.2f;

        [Tooltip("How early in a round this rival starts using heavy attacks.")]
        [Range(0f, 1f)] public float earlyHeavyThreshold = 0f;

        [Tooltip("How much this rival punishes whiffed attacks from the player.")]
        [Range(0f, 1f)] public float whiffPunish = 0.3f;

        /// <summary>Creates a default personality for Balanced style.</summary>
        public static RivalPersonality Default => new RivalPersonality();
    }

    /// <summary>
    /// A named rival identity: its intrinsic fighting style, how strongly it
    /// copies the player, and its body tuning. Each archetype owns a unique
    /// actorName that keys its own stored profile, so every roster rival has
    /// its own learned memory separate from the player's.
    /// </summary>
    [CreateAssetMenu(menuName = "Rival/Enemy/Rival Archetype", fileName = "RivalArchetype")]
    public class RivalArchetype : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Human-readable name for UI (menu display).")]
        public string DisplayName = "Rival";

        [Tooltip("Unique key for telemetry and the stored profile. One rival per actorName.")]
        public string ActorName = "Rival";

        [Header("Style")]
        [Tooltip("Intrinsic base style used before any stored profile exists. If unset, the fair Default profile is used.")]
        public BehaviorProfile BaseStyle;

        [Tooltip("How much of the player's live profile this rival adopts (0 = pure archetype, 1 = full copy).")]
        [Range(0f, 1f)] public float LearningBlend = 0.7f;

        [Header("Personality")]
        [Tooltip("Personality modifiers that define this rival's combat soul.")]
        public RivalPersonality personality = new RivalPersonality();

        [Header("Body Tuning")]
        [Tooltip("Rival movement speed as a fraction of the player's.")]
        [Range(0.5f, 1.2f)] public float MovementSpeedMultiplier = 0.85f;

        [Tooltip("Upper bound on reaction delay, in seconds.")]
        public float MaxReactionDelay = 0.6f;

        [Tooltip("Minimum time between attacks, in seconds.")]
        public float AttackCooldown = 0.9f;

        [Tooltip("Engagement band, in meters.")]
        public float MinEngageDistance = 1.3f;
        public float MaxEngageDistance = 6f;

        [Header("Visuals")]
        [Tooltip("Color tint applied to the rival's material for visual distinction.")]
        public Color AccentColor = Color.white;
    }
}
