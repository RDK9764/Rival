using UnityEngine;

namespace Rival.Combat
{
    public enum AttackType
    {
        Light,
        Heavy
    }

    /// <summary>
    /// One attack in a combo chain. The three timing windows (windup, active,
    /// recovery) define when movement/aim lock, when the hitbox is live and
    /// when control returns. Animation clips are wired in via an adapter later.
    /// </summary>
    [CreateAssetMenu(menuName = "Rival/Combat/Attack Definition", fileName = "AttackDefinition")]
    public class AttackDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Slash";
        [SerializeField] private AttackType attackType = AttackType.Light;

        [Header("Damage")]
        [Tooltip("Multiplier applied to the base damage of the attack. Light attacks hit 1x, heavies break guard.")]
        [SerializeField] private float damageMultiplier = 1f;

        [Header("Timing")]
        [Tooltip("Seconds before the hitbox becomes active. Movement locks during windup.")]
        [SerializeField] private float windupTime = 0.2f;
        [Tooltip("Seconds the hitbox is live.")]
        [SerializeField] private float activeTime = 0.15f;
        [Tooltip("Seconds before the next attack can be chained.")]
        [SerializeField] private float recoveryTime = 0.3f;

        [Header("Stamina")]
        [Tooltip("Stamina spent by the attacker to perform this attack.")]
        [SerializeField] private float staminaCost = 10f;
        [Tooltip("Stamina drained from a defender who blocks this attack.")]
        [SerializeField] private float blockStaminaCost = 12f;

        [Header("Behavior")]
        [Tooltip("Whether this attack breaks a defending guard.")]
        [SerializeField] private bool breaksBlock = false;

        public string DisplayName => displayName;
        public AttackType AttackType => attackType;
        public float DamageMultiplier => damageMultiplier;
        public float WindupTime => windupTime;
        public float ActiveTime => activeTime;
        public float RecoveryTime => recoveryTime;
        public float StaminaCost => staminaCost;
        public float BlockStaminaCost => blockStaminaCost;
        public bool BreaksBlock => breaksBlock;

#if UNITY_EDITOR
        internal void Init(string name, AttackType type, float dmg, float windup,
            float active, float recovery, float stamina, float blockStamina, bool breaks)
        {
            displayName = name;
            attackType = type;
            damageMultiplier = dmg;
            windupTime = windup;
            activeTime = active;
            recoveryTime = recovery;
            staminaCost = stamina;
            blockStaminaCost = blockStamina;
            breaksBlock = breaks;
        }
#endif
    }
}
