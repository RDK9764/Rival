using Rival.Combat;

namespace Rival.Telemetry
{
    /// <summary>
    /// What happened in a fight. Behavioral, not mechanical: we record that an
    /// attack was started and how it resolved, never which button was pressed.
    /// </summary>
    public enum CombatEventType
    {
        AttackPerformed,
        AttackResolved,
        BlockPerformed,
        DodgePerformed,
        ParryPerformed,
        HitTaken,
        BlockBroken,
        Died
    }

    /// <summary>
    /// One immutable telemetry record. Fields are intentionally broad so the
    /// same record feeds console sinks today and JSON persistence later.
    /// </summary>
    public struct CombatEvent
    {
        public float Timestamp;
        /// <summary>Which actor produced the event (scene-set, e.g. "Player", "Rival").</summary>
        public string Actor;
        public CombatEventType Type;
        /// <summary>Distance to the opponent at the time of the event; -1 if no opponent is set.</summary>
        public float Distance;
        public float Damage;
        public int ComboIndex;
        public bool IsHeavy;
        public HitOutcome Outcome;
        public float HealthFraction;
    }
}
