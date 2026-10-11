using System;
using System.IO;
using UnityEngine;
using Rival.Telemetry;

namespace Rival.SaveSystem
{
    /// <summary>
    /// The promised JSON subscriber on the Telemetry bus: buffers CombatEvents
    /// and writes one versioned JSON file per session. Pure subscriber — no
    /// producer changes were needed to persist telemetry.
    /// </summary>
    public class JsonTelemetrySink : MonoBehaviour
    {
        [Tooltip("Seconds between full-log flushes.")]
        [SerializeField] private float flushInterval = 5f;

        private readonly TelemetryLog log = new TelemetryLog();
        private float flushTimer;
        private bool dirty;

        private void Awake()
        {
            log.sessionId = Guid.NewGuid().ToString("N");
            log.sessionStartedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void OnEnable() => Telemetry.Recorded += OnRecorded;

        private void OnDisable()
        {
            Telemetry.Recorded -= OnRecorded;
            Flush();
        }

        private void OnApplicationQuit() => Flush();

        private void OnDestroy() => Flush();

        private void Update()
        {
            flushTimer -= Time.deltaTime;
            if (flushTimer > 0f || !dirty) return;
            flushTimer = flushInterval;
            Flush();
        }

        private void OnRecorded(CombatEvent evt)
        {
            log.records.Add(TelemetryRecord.From(evt));
            dirty = true;
        }

        /// <summary>Writes the session log to disk if it changed since the last flush.</summary>
        public void Flush()
        {
            if (!dirty) return;

            var path = SavePaths.TelemetryLogFile(log.sessionId);
            try
            {
                SavePaths.EnsureDirectory(path);
                File.WriteAllText(path, JsonUtility.ToJson(log, true));
                dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonTelemetrySink] Failed to write telemetry log: {e.Message}", this);
            }
        }
    }
}
