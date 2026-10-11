using UnityEngine;
using Rival.Telemetry;

namespace Rival.BehaviorAnalysis
{
    /// <summary>
    /// Turns one fighter's CombatEvent stream into a BehaviorProfile using
    /// lightweight statistics. Pure C# (no MonoBehaviour) so it is
    /// unit-testable. Reaction speed is measured as the gap between an
    /// opponent's attack and this fighter's response.
    /// </summary>
    public class BehaviorAnalyzer
    {
        private const float MaxAttackRate = 3f;          // attacks per second that reads as "max aggression"
        private const float ComboWindow = 0.9f;          // gap that still counts as one combo chain
        private const float MaxCombo = 5f;               // chain length that reads as "max combo preference"
        private const float MaxReactionGap = 1.0f;       // response gap that reads as "slowest"
        private const float MaxEngageDistance = 8f;      // attack distance that reads as "max preferred range"
        private const float ReactionWindow = 1.5f;       // longest gap we still count as a response
        private const float MovementRange = 3f;          // distance spread that reads as "max movement"

        private readonly string name;

        private int attacks;
        private int heavies;
        private int blocks;
        private int dodges;
        private int parries;
        private float firstAttackTime = -1f;
        private float lastAttackTime = -1f;
        private int currentChain;

        private float distanceSum;
        private float distanceSqSum;
        private int distanceSamples;
        private float comboSum;
        private int comboCount;

        private float lastThreatTime = float.NegativeInfinity;
        private bool respondedToThreat;
        private float reactionSum;
        private int reactionSamples;

        public BehaviorAnalyzer(string trackedName)
        {
            name = trackedName;
        }

        public void Handle(CombatEvent evt)
        {
            if (evt.Actor == name) HandleOwnAction(evt);
            HandleThreat(evt);
        }

        private void HandleOwnAction(CombatEvent evt)
        {
            switch (evt.Type)
            {
                case CombatEventType.AttackPerformed:
                    attacks++;
                    if (evt.IsHeavy) heavies++;

                    if (evt.Distance >= 0f)
                    {
                        distanceSum += evt.Distance;
                        distanceSqSum += evt.Distance * evt.Distance;
                        distanceSamples++;
                    }

                    if (lastAttackTime >= 0f && evt.Timestamp - lastAttackTime <= ComboWindow)
                    {
                        currentChain++;
                    }
                    else
                    {
                        CloseChain();
                        currentChain = 1;
                    }
                    lastAttackTime = evt.Timestamp;
                    if (firstAttackTime < 0f) firstAttackTime = evt.Timestamp;
                    break;

                case CombatEventType.BlockPerformed: blocks++; break;
                case CombatEventType.DodgePerformed: dodges++; break;
                case CombatEventType.ParryPerformed: parries++; break;
            }
        }

        private void CloseChain()
        {
            if (currentChain <= 0) return;
            comboSum += currentChain;
            comboCount++;
            currentChain = 0;
        }

        private void HandleThreat(CombatEvent evt)
        {
            if (evt.Actor != name && evt.Type == CombatEventType.AttackPerformed)
            {
                lastThreatTime = evt.Timestamp;
                respondedToThreat = false;
                return;
            }

            if (evt.Actor != name) return;

            var isResponse = evt.Type == CombatEventType.BlockPerformed
                             || evt.Type == CombatEventType.DodgePerformed
                             || evt.Type == CombatEventType.ParryPerformed
                             || evt.Type == CombatEventType.AttackPerformed;
            if (!isResponse || respondedToThreat) return;

            var gap = evt.Timestamp - lastThreatTime;
            if (gap < 0f || gap > ReactionWindow) return;

            reactionSum += gap;
            reactionSamples++;
            respondedToThreat = true;
        }

        public BehaviorProfile BuildProfile()
        {
            var profile = new BehaviorProfile
            {
                SampleCount = attacks + blocks + dodges + parries
            };

            var span = Mathf.Max(0.1f, lastAttackTime - firstAttackTime);
            profile.Aggression = Clamp(attacks / (span * MaxAttackRate));

            var avgCombo = comboCount > 0 ? comboSum / comboCount : 1f;
            profile.ComboPreference = Clamp((avgCombo - 1f) / (MaxCombo - 1f));

            var defenses = Mathf.Max(1, blocks + dodges + parries);
            profile.BlockFrequency = Clamp((float)blocks / defenses);
            profile.DodgeFrequency = Clamp((float)dodges / defenses);
            profile.ParryFrequency = Clamp((float)parries / defenses);

            profile.HeavyAttackChance = attacks > 0 ? Clamp((float)heavies / attacks) : 0f;

            var avgDistance = distanceSamples > 0 ? distanceSum / distanceSamples : 0f;
            profile.PreferredRange = Clamp(avgDistance / MaxEngageDistance);

            var avgReaction = reactionSamples > 0 ? reactionSum / reactionSamples : MaxReactionGap;
            profile.ReactionSpeed = Clamp(1f - avgReaction / MaxReactionGap);

            var totalActions = attacks + blocks + dodges + parries;
            profile.RiskTolerance = totalActions > 0 ? Clamp((float)attacks / totalActions) : 0f;

            var variance = distanceSamples > 1 ? (distanceSqSum / distanceSamples) - (avgDistance * avgDistance) : 0f;
            profile.MovementStyle = Clamp(Mathf.Sqrt(Mathf.Max(0f, variance)) / MovementRange);

            return profile;
        }

        private static float Clamp(float value) => Mathf.Clamp01(value);
    }
}
