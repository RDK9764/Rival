using UnityEngine;

namespace Rival.Telemetry
{
    /// <summary>
    /// Development sink that prints telemetry to the console so the pipeline is
    /// visible while fighting. Replaced or augmented by a JSON sink later.
    /// </summary>
    public class ConsoleTelemetrySink : MonoBehaviour
    {
        private void OnEnable() => Telemetry.Recorded += OnRecorded;
        private void OnDisable() => Telemetry.Recorded -= OnRecorded;

        private void OnRecorded(CombatEvent evt)
        {
            Debug.Log($"[Telemetry] t={evt.Timestamp:F2} {evt.Actor} {evt.Type}" +
                      $" outcome={evt.Outcome} dmg={evt.Damage:F0} dist={evt.Distance:F1}" +
                      $" hp={evt.HealthFraction:P0}");
        }
    }
}
