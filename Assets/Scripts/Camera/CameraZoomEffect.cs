using UnityEngine;
using Rival.Core;
using Rival.Combat;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Camera zoom effect triggered on kill. Briefly zooms in toward the
    /// target, holds, then smoothly returns to normal. Uses
    /// PlayerCameraController's existing fields via reflection to avoid
    /// tight coupling.
    /// </summary>
    public class CameraZoomEffect : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 2f)] private float zoomAmount = 1.2f;
        [SerializeField, Range(0.1f, 1f)] private float zoomInDuration = 0.15f;
        [SerializeField, Range(0.3f, 2f)] private float holdDuration = 0.5f;
        [SerializeField, Range(0.3f, 1.5f)] private float zoomOutDuration = 0.8f;

        private float zoomTimer;
        private float holdTimer;
        private float returnTimer;
        private float originalDistance;
        private bool zooming;
        private bool holding;
        private bool returning;
        private Camera.Camera cam;
        private Transform camTransform;

        private void Start()
        {
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null) cam = GetComponentInChildren<UnityEngine.Camera>();
            camTransform = cam != null ? cam.transform : null;

            // Only the player's kills should zoom the camera. The player is
            // the GameObjects tagged "Player"; the rival never triggers it.
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null) return;
            var combat = playerGo.GetComponent<CombatController>();
            if (combat != null) combat.AttackResolved += OnAttackResolved;
        }

        private void Update()
        {
            if (camTransform == null) return;

            if (zooming)
            {
                zoomTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(zoomTimer / zoomInDuration);
                var pos = camTransform.localPosition;
                pos.z = Mathf.Lerp(originalDistance, originalDistance * (1f / zoomAmount), t);
                camTransform.localPosition = pos;

                if (t >= 1f)
                {
                    zooming = false;
                    holding = true;
                    holdTimer = 0f;
                }
            }
            else if (holding)
            {
                holdTimer += Time.unscaledDeltaTime;
                if (holdTimer >= holdDuration)
                {
                    holding = false;
                    returning = true;
                    returnTimer = 0f;
                }
            }
            else if (returning)
            {
                returnTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(returnTimer / zoomOutDuration);
                t = t * t * (3f - 2f * t); // smoothstep
                var pos = camTransform.localPosition;
                pos.z = Mathf.Lerp(originalDistance * (1f / zoomAmount), originalDistance, t);
                camTransform.localPosition = pos;

                if (t >= 1f) returning = false;
            }
        }

        private void OnAttackResolved(AttackDefinition def, int combo, HitOutcome outcome)
        {
            if (outcome == HitOutcome.Normal && def != null && def.AttackType == AttackType.Heavy)
            {
                TriggerZoom();
            }
        }

        public void TriggerZoom()
        {
            if (camTransform == null) return;
            originalDistance = camTransform.localPosition.z;
            zooming = true;
            holding = false;
            returning = false;
            zoomTimer = 0f;
        }
    }
}
