using System;
using UnityEngine;

namespace Rival.BehaviorAnalysis
{
    /// <summary>
    /// A distilled, normalized summary of one fighter's style. Every trait is
    /// 0..1, produced by BehaviorAnalyzer from telemetry and consumed by
    /// RivalBrain. This is the "behavioral model" that makes a rival a copy of
    /// the player — without ever replaying their button presses.
    /// [Serializable] lets RivalArchetype assets embed an intrinsic base style.
    /// </summary>
    [Serializable]
    public class BehaviorProfile
    {
        public float Aggression;
        public float ComboPreference;
        public float BlockFrequency;
        public float DodgeFrequency;
        public float ParryFrequency;
        public float HeavyAttackChance;
        public float PreferredRange;
        public float ReactionSpeed;
        public float RiskTolerance;
        public float MovementStyle;
        public int SampleCount;

        public static BehaviorProfile Default { get; } = new BehaviorProfile
        {
            Aggression = 0.5f,
            ComboPreference = 0.25f,
            BlockFrequency = 0.3f,
            DodgeFrequency = 0.3f,
            ParryFrequency = 0.15f,
            HeavyAttackChance = 0.2f,
            PreferredRange = 0.45f,
            ReactionSpeed = 0.5f,
            RiskTolerance = 0.5f,
            MovementStyle = 0.4f
        };

        /// <summary>Blends between two profiles, e.g. a fair default and an observed copy.</summary>
        public static BehaviorProfile Lerp(BehaviorProfile from, BehaviorProfile to, float t)
        {
            return new BehaviorProfile
            {
                Aggression = Mathf.Lerp(from.Aggression, to.Aggression, t),
                ComboPreference = Mathf.Lerp(from.ComboPreference, to.ComboPreference, t),
                BlockFrequency = Mathf.Lerp(from.BlockFrequency, to.BlockFrequency, t),
                DodgeFrequency = Mathf.Lerp(from.DodgeFrequency, to.DodgeFrequency, t),
                ParryFrequency = Mathf.Lerp(from.ParryFrequency, to.ParryFrequency, t),
                HeavyAttackChance = Mathf.Lerp(from.HeavyAttackChance, to.HeavyAttackChance, t),
                PreferredRange = Mathf.Lerp(from.PreferredRange, to.PreferredRange, t),
                ReactionSpeed = Mathf.Lerp(from.ReactionSpeed, to.ReactionSpeed, t),
                RiskTolerance = Mathf.Lerp(from.RiskTolerance, to.RiskTolerance, t),
                MovementStyle = Mathf.Lerp(from.MovementStyle, to.MovementStyle, t),
                SampleCount = to.SampleCount
            };
        }
    }
}
