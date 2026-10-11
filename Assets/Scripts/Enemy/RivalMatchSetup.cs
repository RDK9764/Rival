using UnityEngine;
using Rival.Core;
using Rival.Telemetry;
using Rival.Utilities;

namespace Rival.Enemy
{
    /// <summary>
    /// Scene glue for the roster. In Awake it applies roster[startingIndex] to
    /// the local rival's brain, controller and telemetry recorder, so the whole
    /// identity swap is one call. Exposes SetArchetype(int) for a future menu;
    /// with no roster assigned the scene behaves exactly like Milestone 4.
    /// </summary>
    public class RivalMatchSetup : MonoBehaviour
    {
        [Tooltip("The ordered list of rival identities. Empty = leave the scene's manual wiring untouched.")]
        [SerializeField] private RivalRoster roster;
        [Tooltip("Which archetype to spawn with (index into the roster).")]
        [SerializeField, Range(0, 31)] private int startingIndex;
        [SerializeField] private RivalBrain brain;
        [SerializeField] private RivalController controller;
        [SerializeField] private CombatTelemetryRecorder telemetry;
        [Tooltip("Health components for match-end detection. MatchController subscribes to these.")]
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private HealthComponent rivalHealth;

        private void Awake()
        {
            ServiceLocator.Register<RivalMatchSetup>(this);

            if (roster == null || roster.Archetypes.Count == 0) return;

            if (brain == null) brain = GetComponent<RivalBrain>();
            if (controller == null) controller = GetComponent<RivalController>();
            if (telemetry == null) telemetry = GetComponent<CombatTelemetryRecorder>();

            var index = startingIndex;
            if (ServiceLocator.TryGet<IMatchConfig>(out var matchConfig))
            {
                index = matchConfig.SelectedIndex;
            }

            SetArchetype(index);
        }

        private void OnDestroy() => ServiceLocator.Unregister<RivalMatchSetup>(this);

        /// <summary>Number of selectable archetypes in the roster (0 = no roster wired).</summary>
        public int ArchetypeCount => roster != null ? roster.Archetypes.Count : 0;

        /// <summary>Index last applied by SetArchetype; what the menu highlights first.</summary>
        public int CurrentIndex { get; private set; }

        /// <summary>Get the archetype at an index, or null.</summary>
        public RivalArchetype GetArchetype(int index)
        {
            if (roster == null || index < 0 || index >= roster.Archetypes.Count) return null;
            return roster.Archetypes[index];
        }

        /// <summary>Display name of the archetype at an index, or empty string.</summary>
        public string GetArchetypeName(int index)
        {
            if (roster == null || index < 0 || index >= roster.Archetypes.Count) return string.Empty;
            var archetype = roster.Archetypes[index];
            return archetype != null && !string.IsNullOrEmpty(archetype.DisplayName) ? archetype.DisplayName : string.Empty;
        }

        /// <summary>Player's health, wired in the inspector for MatchController.</summary>
        public HealthComponent PlayerHealth => playerHealth;

        /// <summary>Rival's health, wired in the inspector for MatchController.</summary>
        public HealthComponent RivalHealth => rivalHealth;

        /// <summary>Swaps the active rival identity at runtime by index; a future menu picks the index.</summary>
        public void SetArchetype(int index)
        {
            if (roster == null || roster.Archetypes.Count == 0) return;

            index = Mathf.Clamp(index, 0, roster.Archetypes.Count - 1);
            CurrentIndex = index;
            var archetype = roster.Archetypes[index];
            if (archetype == null) return;

            if (brain != null) brain.Configure(archetype);
            if (controller != null) controller.SetMovementSpeedMultiplier(archetype.MovementSpeedMultiplier);
            if (telemetry != null) telemetry.SetActorName(archetype.ActorName);
            ApplyAccentColor(archetype.AccentColor);
        }

        /// <summary>Swaps the active rival identity directly by archetype asset. Used by endless mode.</summary>
        public void SetArchetypeByAsset(RivalArchetype archetype)
        {
            if (archetype == null) return;

            if (brain != null) brain.Configure(archetype);
            if (controller != null) controller.SetMovementSpeedMultiplier(archetype.MovementSpeedMultiplier);
            if (telemetry != null) telemetry.SetActorName(archetype.ActorName);
            ApplyAccentColor(archetype.AccentColor);
        }

        private void ApplyAccentColor(Color color)
        {
            // The procedural model carries the archetype accent on its belts,
            // blades and caps; recolor those. Fall back to the first renderer
            // for scenes that still use a plain capsule + Material tint.
            var builder = GetComponentInChildren<ProceduralFighterBuilder>();
            if (builder != null)
            {
                builder.SetAccentColor(color);
                return;
            }
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer == null) return;
            var mat = renderer.material;
            if (mat.HasProperty("_Color")) mat.color = color;
        }
    }
}
