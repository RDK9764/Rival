using System;
using System.Collections.Generic;

namespace Rival.SaveSystem
{
    /// <summary>
    /// One session's telemetry, serialized as a single JSON file. The
    /// schemaVersion field is the contract for format evolution.
    /// </summary>
    [Serializable]
    public class TelemetryLog
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string sessionId;
        public long sessionStartedAtUnixMs;
        public List<TelemetryRecord> records = new List<TelemetryRecord>();
    }
}
