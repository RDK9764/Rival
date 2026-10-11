using System;
using Rival.BehaviorAnalysis;

namespace Rival.SaveSystem
{
    /// <summary>
    /// Versioned, serializable snapshot of a BehaviorProfile. Kept separate from
    /// the runtime type so the saved format can evolve independently.
    /// </summary>
    [Serializable]
    public class ProfileRecord
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string actor;
        public float aggression;
        public float comboPreference;
        public float blockFrequency;
        public float dodgeFrequency;
        public float parryFrequency;
        public float heavyAttackChance;
        public float preferredRange;
        public float reactionSpeed;
        public float riskTolerance;
        public float movementStyle;
        public int sampleCount;

        public static ProfileRecord From(BehaviorProfile profile, string actor)
        {
            return new ProfileRecord
            {
                actor = actor,
                aggression = profile.Aggression,
                comboPreference = profile.ComboPreference,
                blockFrequency = profile.BlockFrequency,
                dodgeFrequency = profile.DodgeFrequency,
                parryFrequency = profile.ParryFrequency,
                heavyAttackChance = profile.HeavyAttackChance,
                preferredRange = profile.PreferredRange,
                reactionSpeed = profile.ReactionSpeed,
                riskTolerance = profile.RiskTolerance,
                movementStyle = profile.MovementStyle,
                sampleCount = profile.SampleCount
            };
        }

        public BehaviorProfile ToProfile()
        {
            return new BehaviorProfile
            {
                Aggression = aggression,
                ComboPreference = comboPreference,
                BlockFrequency = blockFrequency,
                DodgeFrequency = dodgeFrequency,
                ParryFrequency = parryFrequency,
                HeavyAttackChance = heavyAttackChance,
                PreferredRange = preferredRange,
                ReactionSpeed = reactionSpeed,
                RiskTolerance = riskTolerance,
                MovementStyle = movementStyle,
                SampleCount = sampleCount
            };
        }
    }
}
