using System;
using UnityEngine;
using Rival.Actors;
using Rival.BehaviorAnalysis;
using Rival.Combat;
using Rival.SaveSystem;
using Rival.Utilities;

namespace Rival.Enemy
{
    /// <summary>
    /// The first true rival: an ICombatInput driven by a BehaviorProfile copied
    /// from the player's live telemetry. It decides movement, attacks, blocks,
    /// parries and dodges from traits — it never replays what the player pressed.
    /// </summary>
    public class RivalBrain : MonoBehaviour, ICombatInput
    {
        [Header("Wiring")]
        [SerializeField] private BehaviorProfiler profiler;
        [SerializeField] private Transform opponent;
        [SerializeField] private CombatController combat;
        [SerializeField] private CharacterMotor motor;
        [Tooltip("Whose profile this rival copies from telemetry.")]
        [SerializeField] private string profileActorName = "Player";
        [Tooltip("How much of the observed profile the rival adopts (0 = default fighter, 1 = full copy).")]
        [SerializeField, Range(0f, 1f)] private float learningBlend = 0.7f;
        [Tooltip("Start from the stored profile of the target, if one exists, instead of the default fighter.")]
        [SerializeField] private bool useStoredProfile = true;

        [Header("Archetype (optional)")]
        [Tooltip("When set, the rival fights as this identity: its own stored memory seeds the blend, its intrinsic style is the fallback, and its tuning overrides the timings below. Set at runtime by RivalMatchSetup for the roster.")]
        [SerializeField] private RivalArchetype archetype;

        [Header("Timing")]
        [SerializeField] private float thinkInterval = 0.15f;
        [SerializeField] private float maxReactionDelay = 0.6f;
        [SerializeField] private float threatMemory = 0.6f;
        [SerializeField] private float attackCooldown = 0.9f;
        [SerializeField] private float minEngageDistance = 1.3f;
        [SerializeField] private float maxEngageDistance = 6f;

        // ---- Personality (set by archetype) ----
        private RivalPersonality personality = new RivalPersonality();
        private float comboChainTimer;
        private bool canDoubleAttack;
        private bool lastAttackLanded;
        private float roundTimer;

        // ---- ICombatInput ----
        public bool IsBlockHeld { get; private set; }
        public bool IsTargetLockHeld => true;

        public event Action LightAttackPressed;
        public event Action HeavyAttackPressed;
        public event Action ParryPressed;
        public event Action TargetLockPressed;

        /// <summary>Fired when the rival decides to dodge; RivalController executes it on the motor.</summary>
        public event Action DodgeRequested;
        public Vector3 MovementDirection { get; private set; }
        public Vector3 DodgeDirection { get; private set; }

        private BehaviorProfile profile = BehaviorProfile.Default;
        private BehaviorProfile seedProfile = BehaviorProfile.Default;
        private float thinkTimer;
        private float threatTimer = float.PositiveInfinity;
        private float cooldownTimer;
        private bool respondedToThreat;
        private float strafeSign = 1f;
        private CombatController opponentCombat;

        private void Awake()
        {
            if (combat == null) combat = GetComponent<CombatController>();
            if (motor == null) motor = GetComponent<CharacterMotor>();

            if (opponent != null)
            {
                opponentCombat = opponent.GetComponent<CombatController>();
            }
        }

        private void OnEnable()
        {
            if (opponentCombat != null) opponentCombat.AttackStarted += OnOpponentAttack;
            if (combat != null) combat.AttackResolved += OnAttackResolved;
        }

        private void OnDisable()
        {
            if (opponentCombat != null) opponentCombat.AttackStarted -= OnOpponentAttack;
            if (combat != null) combat.AttackResolved -= OnAttackResolved;
        }

        private void Start()
        {
            // A remembered style from a previous session becomes the blend base,
            // so the rival fights like the player *before* a single new event.
            // An archetype's memory is its own actorName; without one the rival
            // inherits the target's stored profile (the legacy single-rival case).
            var memoryKey = archetype != null ? archetype.ActorName : profileActorName;
            if (useStoredProfile && ServiceLocator.TryGet(out IProfileStore store)
                && store.TryLoad(memoryKey, out var stored))
            {
                seedProfile = stored;
            }
            else if (archetype != null && archetype.BaseStyle != null)
            {
                seedProfile = archetype.BaseStyle;
            }
        }

        /// <summary>
        /// Applies an archetype's identity, style and body tuning at runtime.
        /// Called by RivalMatchSetup on Awake, so Start's seed logic below sees
        /// it. Tuning only overrides the serialized defaults when an archetype
        /// is configured; a brain with no archetype keeps its inspector values.
        /// </summary>
        public void Configure(RivalArchetype archetype)
        {
            if (archetype == null) return;
            this.archetype = archetype;
            learningBlend = archetype.LearningBlend;
            maxReactionDelay = archetype.MaxReactionDelay;
            attackCooldown = archetype.AttackCooldown;
            minEngageDistance = archetype.MinEngageDistance;
            maxEngageDistance = archetype.MaxEngageDistance;
            personality = archetype.personality != null
                ? archetype.personality
                : RivalPersonality.Default;
        }

        private void OnOpponentAttack(AttackDefinition def, int comboIndex)
        {
            threatTimer = 0f;
            respondedToThreat = false;
        }

        private void OnAttackResolved(AttackDefinition def, int comboIndex, HitOutcome outcome)
        {
            if (outcome == HitOutcome.Normal || outcome == HitOutcome.BlockBroken)
                lastAttackLanded = true;
        }

        public void ResetRoundTimer()
        {
            roundTimer = 0f;
            canDoubleAttack = false;
            comboChainTimer = 0f;
            lastAttackLanded = false;
        }

        private void Update()
        {
            var delta = Time.deltaTime;
            cooldownTimer = Mathf.Max(0f, cooldownTimer - delta);
            threatTimer += delta;
            comboChainTimer = Mathf.Max(0f, comboChainTimer - delta);
            roundTimer += delta;

            thinkTimer -= delta;
            if (thinkTimer > 0f) return;
            thinkTimer = thinkInterval;

            UpdateProfile();
            Decide();
        }

        /// <summary>
        /// Copies the target's style once per think tick, not every frame, so
        /// the analyzer's profile and the blend don't allocate per frame.
        /// Blends from the remembered seed (persisted profile) toward the live
        /// analysis of this session.
        /// </summary>
        private void UpdateProfile()
        {
            if (profiler == null) return;
            profile = BehaviorProfile.Lerp(seedProfile, profiler.GetProfile(profileActorName), learningBlend);
        }

        private void Decide()
        {
            if (combat == null || opponent == null) return;

            if (combat.State == CombatState.Stunned)
            {
                IsBlockHeld = false;
                MovementDirection = Vector3.zero;
                return;
            }

            var distance = Vector3.Distance(transform.position, opponent.position);
            var preferredDistance = Mathf.Lerp(minEngageDistance, maxEngageDistance, profile.PreferredRange);
            var reactionDelay = (1f - profile.ReactionSpeed) * maxReactionDelay;
            var threatActive = threatTimer < threatMemory;
            var canRespond = threatTimer >= reactionDelay;

            // ── Double attack chaining ──
            if (canDoubleAttack && comboChainTimer <= 0f && !threatActive)
            {
                canDoubleAttack = false;
                if (Roll(personality.doubleAttackChance))
                {
                    IsBlockHeld = false;
                    if (UnityEngine.Random.value < profile.HeavyAttackChance) HeavyAttackPressed?.Invoke();
                    else LightAttackPressed?.Invoke();
                    cooldownTimer = attackCooldown;
                    return;
                }
            }

            // ── Feint: start attack then cancel into movement ──
            if (!threatActive && distance <= preferredDistance + 1f && cooldownTimer <= 0f
                && Roll(personality.feintChance) && personality.feintChance > 0f)
            {
                IsBlockHeld = false;
                // Feint: fire a light attack but immediately dodge sideways
                LightAttackPressed?.Invoke();
                DodgeDirection = Vector3.Cross(Vector3.up, (opponent.position - transform.position).normalized) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
                DodgeRequested?.Invoke();
                cooldownTimer = attackCooldown * 0.6f;
                return;
            }

            // ── Counter-attack after successful defense ──
            if (lastAttackLanded && canRespond && Roll(personality.counterAttackChance))
            {
                lastAttackLanded = false;
                if (distance <= preferredDistance + 1.5f)
                {
                    IsBlockHeld = false;
                    if (UnityEngine.Random.value < profile.HeavyAttackChance * 1.5f) HeavyAttackPressed?.Invoke();
                    else LightAttackPressed?.Invoke();
                    cooldownTimer = attackCooldown * 0.7f;
                    return;
                }
            }
            lastAttackLanded = false;

            // Respond to a fresh threat — once, after the reaction delay passes.
            if (threatActive && canRespond && !respondedToThreat)
            {
                var urgency = Mathf.Clamp01(1f - Mathf.Max(0f, threatTimer - reactionDelay)
                                                / Mathf.Max(0.01f, threatMemory - reactionDelay));

                if (Roll(profile.DodgeFrequency * urgency))
                {
                    IsBlockHeld = false;
                    DodgeDirection = (transform.position - opponent.position);
                    DodgeDirection.y = 0f;
                    DodgeDirection = DodgeDirection.sqrMagnitude > 0.01f
                        ? DodgeDirection.normalized
                        : -transform.forward;
                    respondedToThreat = true;
                    DodgeRequested?.Invoke();
                    return;
                }

                if (Roll(profile.ParryFrequency * urgency))
                {
                    IsBlockHeld = false;
                    respondedToThreat = true;
                    ParryPressed?.Invoke();
                    return;
                }

                if (Roll(profile.BlockFrequency * urgency))
                {
                    respondedToThreat = true;
                    IsBlockHeld = true;
                }
            }

            // ── Attack when not under pressure ──
            if (!threatActive && distance <= preferredDistance + 1f && cooldownTimer <= 0f && Roll(profile.Aggression))
            {
                IsBlockHeld = false;

                // Personality: early heavy threshold — more heavies as the round progresses
                var roundProgress = Mathf.Clamp01(roundTimer / 30f);
                var heavyBias = roundProgress > personality.earlyHeavyThreshold ? 1.3f : 0.7f;

                if (UnityEngine.Random.value < profile.HeavyAttackChance * heavyBias) HeavyAttackPressed?.Invoke();
                else LightAttackPressed?.Invoke();

                cooldownTimer = attackCooldown;

                // Personality: double attack chance
                if (Roll(personality.doubleAttackChance))
                {
                    canDoubleAttack = true;
                    comboChainTimer = attackCooldown * 0.5f;
                }
                return;
            }

            DecideMovement(distance, preferredDistance);

            // Release the guard once the threat window has fully passed.
            if (IsBlockHeld && threatTimer > threatMemory + 0.5f) IsBlockHeld = false;
        }

        private void DecideMovement(float distance, float preferredDistance)
        {
            if (distance > preferredDistance + 1f)
            {
                MovementDirection = opponent.position - transform.position;
            }
            else if (distance < minEngageDistance)
            {
                MovementDirection = transform.position - opponent.position;
            }
            else if (personality.strafeWhileIdle && Roll(profile.MovementStyle))
            {
                if (UnityEngine.Random.value < 0.2f) strafeSign = -strafeSign;
                var toOpponent = (opponent.position - transform.position).normalized;
                MovementDirection = Vector3.Cross(Vector3.up, toOpponent) * strafeSign;
            }
            else if (!personality.strafeWhileIdle)
            {
                // Hold ground — only move when opponent retreats
                if (distance > preferredDistance + 0.5f)
                {
                    var chaseChance = personality.chaseAggression;
                    if (Roll(chaseChance))
                        MovementDirection = (opponent.position - transform.position).normalized;
                    else
                        MovementDirection = Vector3.zero;
                }
                else
                {
                    MovementDirection = Vector3.zero;
                }
            }
            else
            {
                MovementDirection = Vector3.zero;
            }

            MovementDirection.y = 0f;
            if (MovementDirection.sqrMagnitude > 1f) MovementDirection.Normalize();
        }

        private static bool Roll(float probability) => UnityEngine.Random.value < probability;
    }
}
