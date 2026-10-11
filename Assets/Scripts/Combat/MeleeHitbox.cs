using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rival.Combat
{
    /// <summary>
    /// Detects victims during an attack's active window. Toggled on/off by the
    /// CombatController; knows nothing about state machines. Each activation
    /// is one swing — a victim can only be hit once per swing.
    /// </summary>
    public class MeleeHitbox : MonoBehaviour
    {
        [Tooltip("Layers that can be hit. Ground and scenery are naturally skipped as they have no IDamageable.")]
        [SerializeField] private LayerMask victimLayers = ~0;
        [Tooltip("When enabled, each swing hits at most one victim.")]
        [SerializeField] private bool singleTarget = false;

        private readonly HashSet<Transform> alreadyHit = new HashSet<Transform>();
        private Transform attackerRoot;
        private Weapon weapon;
        private AttackDefinition attackDef;
        private float damage;
        private float radius;
        private bool isActive;

        public bool IsActive => isActive;

        public event Action<DamageContext, DamageResult> HitLanded;

        private void Awake()
        {
            attackerRoot = transform.root;
        }

        public void Activate(Weapon weapon, AttackDefinition attackDef, float damage)
        {
            this.weapon = weapon;
            this.attackDef = attackDef;
            this.damage = damage;
            radius = weapon != null ? weapon.AttackRange : 1.5f;
            alreadyHit.Clear();
            isActive = true;
        }

        public void Deactivate()
        {
            isActive = false;
        }

        private void FixedUpdate()
        {
            if (!isActive) return;

            var hits = Physics.OverlapSphere(transform.position, radius, victimLayers, QueryTriggerInteraction.Ignore);
            foreach (var collider in hits)
            {
                var root = collider.transform.root;
                if (root == attackerRoot) continue;
                if (alreadyHit.Contains(root)) continue;

                var damageable = collider.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;

                var context = new DamageContext
                {
                    Attacker = attackerRoot,
                    Weapon = weapon,
                    Attack = attackDef,
                    BaseDamage = damage,
                    HitDirection = (root.position - attackerRoot.position).normalized,
                    IsHeavy = attackDef != null && attackDef.AttackType == AttackType.Heavy
                };

                var result = damageable.ApplyDamage(context);
                alreadyHit.Add(root);
                HitLanded?.Invoke(context, result);

                if (singleTarget) break;
            }
        }
    }
}
