using UnityEngine;

namespace Rival.Combat
{
    public enum WeaponType
    {
        Sword,
        Axe,
        Spear,
        Dagger,
        Hammer,
        Fist
    }

    /// <summary>
    /// Weapon stats. Read-only runtime config; combat code never mutates it.
    /// Each weapon type has distinct feel: daggers are fast but weak, hammers
    /// are slow but devastating, spears have reach, axes break guard easily.
    /// </summary>
    [CreateAssetMenu(menuName = "Rival/Combat/Weapon", fileName = "Weapon")]
    public class Weapon : ScriptableObject
    {
        [SerializeField] private string weaponName = "Sword";
        [SerializeField] private WeaponType weaponType = WeaponType.Sword;
        [Tooltip("Raw damage of the weapon before attack multipliers.")]
        [SerializeField] private float baseDamage = 20f;
        [Tooltip("Chance per hit to critically strike.")]
        [SerializeField] private float critChance = 0.1f;
        [Tooltip("Damage multiplier applied on a crit.")]
        [SerializeField] private float critMultiplier = 1.5f;
        [Tooltip("How far an attack's hitbox reaches in metres.")]
        [SerializeField] private float attackRange = 1.6f;
        [Tooltip("Global attack speed multiplier for this weapon.")]
        [SerializeField, Range(0.6f, 1.5f)] private float speedMultiplier = 1f;
        [Tooltip("Global stamina cost multiplier for this weapon.")]
        [SerializeField, Range(0.5f, 2f)] private float staminaMultiplier = 1f;
        [Tooltip("How much stamina this weapon drains from a blocking defender per hit.")]
        [SerializeField, Range(0.5f, 3f)] private float blockStaminaMultiplier = 1f;
        [Tooltip("Knockback force applied on hit.")]
        [SerializeField] private float knockbackForce = 3f;
        [Tooltip("Visual scale multiplier for the hitbox trail.")]
        [SerializeField, Range(0.5f, 2f)] private float trailScale = 1f;

        public string WeaponName => weaponName;
        public WeaponType WeaponType => weaponType;
        public float BaseDamage => baseDamage;
        public float CritChance => critChance;
        public float CritMultiplier => critMultiplier;
        public float AttackRange => attackRange;
        public float SpeedMultiplier => speedMultiplier;
        public float StaminaMultiplier => staminaMultiplier;
        public float BlockStaminaMultiplier => blockStaminaMultiplier;
        public float KnockbackForce => knockbackForce;
        public float TrailScale => trailScale;

#if UNITY_EDITOR
        internal void Init(string name, WeaponType type, float damage, float crit, float critMul,
            float range, float speed, float stamina, float blockStamina, float knockback, float trail)
        {
            weaponName = name;
            weaponType = type;
            baseDamage = damage;
            critChance = crit;
            critMultiplier = critMul;
            attackRange = range;
            speedMultiplier = speed;
            staminaMultiplier = stamina;
            blockStaminaMultiplier = blockStamina;
            knockbackForce = knockback;
            trailScale = trail;
        }
#endif
    }
}
