using UnityEngine;
using Rival.Core;
using Rival.Combat;
using Rival.Actors;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Subscribes to MeleeHitbox.HitLanded on both actors and spawns VFX
    /// at the approximate contact point (midpoint between hitbox and victim).
    /// Uses IVfxService from the ServiceLocator.
    /// </summary>
    public class HitVfxSpawner : MonoBehaviour
    {
        [SerializeField] private MeleeHitbox playerHitbox;
        [SerializeField] private MeleeHitbox rivalHitbox;
        [SerializeField] private CombatController playerCombat;
        [SerializeField] private CombatController rivalCombat;

        private IVfxService vfxService;

        private void Start()
        {
            ServiceLocator.TryGet(out vfxService);
            if (playerHitbox != null) playerHitbox.HitLanded += OnPlayerHitLanded;
            if (rivalHitbox != null) rivalHitbox.HitLanded += OnRivalHitLanded;
            if (playerCombat != null) playerCombat.BlockBroken += OnPlayerBlockBroken;
            if (rivalCombat != null) rivalCombat.BlockBroken += OnRivalBlockBroken;
        }

        private void OnDestroy()
        {
            if (playerHitbox != null) playerHitbox.HitLanded -= OnPlayerHitLanded;
            if (rivalHitbox != null) rivalHitbox.HitLanded -= OnRivalHitLanded;
            if (playerCombat != null) playerCombat.BlockBroken -= OnPlayerBlockBroken;
            if (rivalCombat != null) rivalCombat.BlockBroken -= OnRivalBlockBroken;
        }

        private void OnPlayerHitLanded(DamageContext ctx, DamageResult result)
        {
            SpawnForHit(ctx, playerHitbox, result);
        }

        private void OnRivalHitLanded(DamageContext ctx, DamageResult result)
        {
            SpawnForHit(ctx, rivalHitbox, result);
        }

        private void OnPlayerBlockBroken()
        {
            if (vfxService != null && playerCombat != null)
                vfxService.Spawn(VfxType.GuardBreak, playerCombat.transform.position + Vector3.up, Vector3.up);
        }

        private void OnRivalBlockBroken()
        {
            if (vfxService != null && rivalCombat != null)
                vfxService.Spawn(VfxType.GuardBreak, rivalCombat.transform.position + Vector3.up, Vector3.up);
        }

        private void SpawnForHit(DamageContext ctx, MeleeHitbox hitbox, DamageResult result)
        {
            if (vfxService == null) return;

            var hitPoint = (hitbox.transform.position + ctx.Attacker.position + Vector3.up) * 0.5f;
            var hitNormal = (ctx.HitDirection.sqrMagnitude > 0.01f) ? -ctx.HitDirection : Vector3.up;

            VfxType type;
            switch (result.Outcome)
            {
                case HitOutcome.Parried:
                    type = VfxType.ParryFlash;
                    break;
                case HitOutcome.Blocked:
                    type = VfxType.BlockSpark;
                    break;
                default:
                    type = ctx.IsHeavy ? VfxType.HeavyHit : VfxType.LightHit;
                    break;
            }

            vfxService.Spawn(type, hitPoint, hitNormal);
        }
    }
}
