using UnityEngine;

namespace Rival.Combat
{
    public enum HitOutcome
    {
        Normal,
        Blocked,
        BlockBroken,
        Parried,
        Evaded
    }

    /// <summary>
    /// Immutable data describing one incoming hit. Built by the attacker's
    /// hitbox and consumed by the defender's DamageReceiver.
    /// </summary>
    public struct DamageContext
    {
        public Transform Attacker;
        public Weapon Weapon;
        public AttackDefinition Attack;
        public float BaseDamage;
        public Vector3 HitDirection;
        public bool IsHeavy;
    }

    /// <summary>
    /// The resolved result of a hit: what the defender actually took and why.
    /// Emitted so UI, audio and telemetry can react without coupling to rules.
    /// </summary>
    public struct DamageResult
    {
        public float FinalDamage;
        public HitOutcome Outcome;
        public bool WasCrit;
    }
}
