using System;

namespace Rival.Telemetry
{
    /// <summary>
    /// Minimal pub/sub bus for telemetry records. Producers call Record();
    /// sinks and analyzers subscribe to Recorded. Decoupled by design — the
    /// SaveSystem milestone adds a JSON sink that subscribes here without
    /// touching any producer.
    /// </summary>
    public static class Telemetry
    {
        public static event Action<CombatEvent> Recorded;

        public static void Record(CombatEvent evt) => Recorded?.Invoke(evt);
    }
}
