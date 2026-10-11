using System.Collections.Generic;
using UnityEngine;
using Rival.Telemetry;

namespace Rival.BehaviorAnalysis
{
    /// <summary>
    /// Scene host for the analyzers. Bridges the Telemetry bus: an analyzer is
    /// created for every actor that produces events, and every event reaches
    /// every analyzer so each can also measure reaction to its opponent's
    /// attacks. Brains query GetProfile(actorName) to copy a fighting style.
    /// </summary>
    public class BehaviorProfiler : MonoBehaviour
    {
        private readonly Dictionary<string, BehaviorAnalyzer> analyzers = new Dictionary<string, BehaviorAnalyzer>();

        /// <summary>Actors with a live analyzer. Zero-allocation view of the keys.</summary>
        public IReadOnlyCollection<string> ActorNames => analyzers.Keys;

        private void OnEnable() => Telemetry.Recorded += OnRecorded;
        private void OnDisable() => Telemetry.Recorded -= OnRecorded;

        private void OnRecorded(CombatEvent evt)
        {
            if (!analyzers.TryGetValue(evt.Actor, out _))
            {
                analyzers[evt.Actor] = new BehaviorAnalyzer(evt.Actor);
            }

            foreach (var analyzer in analyzers.Values)
            {
                analyzer.Handle(evt);
            }
        }

        /// <summary>Latest profile for an actor; default if no events have been recorded yet.</summary>
        public BehaviorProfile GetProfile(string actorName)
        {
            if (analyzers.TryGetValue(actorName, out var analyzer))
            {
                return analyzer.BuildProfile();
            }

            var created = new BehaviorAnalyzer(actorName);
            analyzers[actorName] = created;
            return created.BuildProfile();
        }
    }
}
