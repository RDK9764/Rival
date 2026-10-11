using UnityEngine;
using Rival.Combat;

namespace Rival.Actors
{
    /// <summary>
    /// Enables a TrailRenderer during the active phase of an attack. Attach to
    /// the MeleeHitbox child or the weapon tip. The trail is disabled by default
    /// and only shows during swings.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class WeaponTrail : MonoBehaviour
    {
        [SerializeField] private CombatController combat;
        [SerializeField] private MeleeHitbox hitbox;

        private TrailRenderer trail;

        private void Awake()
        {
            trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.emitting = false;
                trail.time = 0.15f;
            }

            if (combat == null) combat = GetComponentInParent<CombatController>();
            if (hitbox == null) hitbox = GetComponentInParent<MeleeHitbox>();
        }

        private void Update()
        {
            if (trail == null || combat == null) return;

            var active = combat.State == CombatState.Attacking
                         && combat.Phase == AttackPhase.Active;
            trail.emitting = active;
        }
    }
}
