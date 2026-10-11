using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Brief time-scale freeze on hit. Subscribes to combat events and applies
    /// a short pause via ITimeService, restoring the previous scale after the
    /// duration. Stacks by resetting the timer on each trigger.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 0.15f)] private float hitStopDuration = 0.04f;
        [SerializeField, Range(0f, 0.1f)] private float killStopDuration = 0.12f;
        [SerializeField, Range(0.05f, 0.5f)] private float killSlowMotionDuration = 0.35f;
        [SerializeField, Range(0.1f, 0.5f)] private float killSlowMotionScale = 0.3f;

        private ITimeService timeService;
        private float stopTimer;
        private float restoredScale;
        private bool stopping;

        private void Start()
        {
            ServiceLocator.TryGet(out timeService);
        }

        private void Update()
        {
            if (!stopping) return;

            stopTimer -= Time.unscaledDeltaTime;
            if (stopTimer <= 0f)
            {
                stopping = false;
                timeService?.SetScale(restoredScale);
            }
        }

        /// <summary>Call from combat event subscribers to trigger a brief freeze.</summary>
        public void TriggerHitStop()
        {
            if (timeService == null) return;
            restoredScale = timeService.Scale;
            timeService.SetScale(0f);
            stopTimer = hitStopDuration;
            stopping = true;
        }

        /// <summary>Longer freeze + slow-motion for match-ending hits.</summary>
        public void TriggerKillStop()
        {
            if (timeService == null) return;
            restoredScale = timeService.Scale;
            timeService.SetScale(0f);
            stopTimer = killStopDuration;
            stopping = true;
        }

        /// <summary>After kill-stop expires, apply slow-motion for a duration.</summary>
        public void TriggerKillSlowMotion()
        {
            TriggerKillStop();
            // Slow-motion kicks in after the kill-stop via Invoke
            CancelInvoke(nameof(ApplyKillSlowMotion));
            Invoke(nameof(ApplyKillSlowMotion), killStopDuration);
        }

        private void ApplyKillSlowMotion()
        {
            timeService?.SetScale(killSlowMotionScale);
            CancelInvoke(nameof(RestoreFromKillSlowMotion));
            Invoke(nameof(RestoreFromKillSlowMotion), killSlowMotionDuration);
        }

        private void RestoreFromKillSlowMotion()
        {
            timeService?.SetScale(1f);
        }

        private void OnDisable()
        {
            CancelInvoke();
            if (stopping && timeService != null) timeService.SetScale(restoredScale);
            stopping = false;
        }
    }
}
