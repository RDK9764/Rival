using System;
using Rival.Telemetry;

namespace Rival.SaveSystem
{
    /// <summary>
    /// Versioned, serializable representation of one CombatEvent. Kept separate
    /// from the runtime type so the on-disk format can evolve without touching
    /// telemetry producers. Enums are stored as int codes to avoid dragging the
    /// Combat assembly into the save layer.
    /// </summary>
    [Serializable]
    public class TelemetryRecord
    {
        public double timestamp;
        public string actor;
        public int typeCode;
        public double distance;
        public double damage;
        public int comboIndex;
        public bool isHeavy;
        public int outcomeCode;
        public double healthFraction;

        public static TelemetryRecord From(CombatEvent evt)
        {
            return new TelemetryRecord
            {
                timestamp = evt.Timestamp,
                actor = evt.Actor,
                typeCode = (int)evt.Type,
                distance = evt.Distance,
                damage = evt.Damage,
                comboIndex = evt.ComboIndex,
                isHeavy = evt.IsHeavy,
                outcomeCode = (int)evt.Outcome,
                healthFraction = evt.HealthFraction
            };
        }
    }
}
