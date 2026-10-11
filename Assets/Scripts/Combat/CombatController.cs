using System;
using UnityEngine;
using Rival.Actors;

namespace Rival.Combat
{
    public enum CombatState
    {
        Idle,
        Attacking,
        Blocking,
        Parry,
        Stunned
    }

    public enum AttackPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    /// <summary>
    /// The combat rules engine, shared by the player and (later) every rival.
    /// Owns a small state machine (idle / attacking / blocking / parry /
    /// stunned), drives the MeleeHitbox through attack phases, spends stamina
    /// for attacks, blocks and parries, and resolves hits against itself as
    /// the defender. All effects are broadcast as events so UI, audio and
    /// telemetry stay decoupled.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(StaminaComponent))]
    [RequireComponent(typeof(DamageReceiver))]
    [RequireComponent(typeof(CombatInputBridge))]
    public class CombatController : MonoBehaviour, IDefenseState
    {
        public const float BlockSlowMultiplier = 0.45f;

        [Header("References")]
        [SerializeField] private HealthComponent health;
        [SerializeField] private StaminaComponent stamina;
        [SerializeField] private DamageReceiver damageReceiver;
        [SerializeField] private CombatInputBridge inputBridge;
        [SerializeField] private MeleeHitbox hitbox;
        [SerializeField] private Weapon weapon;
        [SerializeField] private AttackDefinition[] comboChain;

        [Header("Defense")]
        [Tooltip("How long the parry window stays open after pressing parry.")]
        [SerializeField] private float parryWindowDuration = 0.25f;
        [Tooltip("Stamina spent to attempt a parry.")]
        [SerializeField] private float parryStaminaCost = 12f;
        [Tooltip("Stamina drained per second while blocking.")]
        [SerializeField] private float blockStaminaCostPerSecond = 12f;
        [Tooltip("How long a successful hit stuns an attacker who was mid-swing.")]
        [SerializeField] private float hitStunDuration = 0.4f;
        [Tooltip("How long a guard break stuns the blocker.")]
        [SerializeField] private float blockBreakStunDuration = 0.7f;
        [Tooltip("How long a successful parry stuns the attacker.")]
        [SerializeField] private float parryStunOnAttacker = 1.0f;

        public CombatState State { get; private set; } = CombatState.Idle;
        public AttackPhase Phase { get; private set; } = AttackPhase.None;

        public bool IsMovementLocked => State == CombatState.Attacking || State == CombatState.Stunned;
        public bool IsBlocking => State == CombatState.Blocking;
        public bool IsParryWindowActive => State == CombatState.Parry;
        public bool IsDodgeInvulnerable { get; private set; }

        public event Action<CombatState> StateChanged;
        public event Action<AttackDefinition, int> AttackStarted;
        public event Action<AttackDefinition, int, HitOutcome> AttackResolved;
        public event Action BlockBroken;
        public event Action Parried;
        public event Action Stunned;

        private int comboIndex;
        private AttackDefinition currentAttack;
        private float currentDamage;
        private float phaseTimer;
        private float stunTimer;

        private void Awake()
        {
            health = Resolve(health);
            stamina = Resolve(stamina);
            damageReceiver = Resolve(damageReceiver);
            inputBridge = Resolve(inputBridge);
            if (hitbox == null) hitbox = GetComponentInChildren<MeleeHitbox>();

            if (weapon == null) Debug.LogWarning("[CombatController] No Weapon assigned.", this);
            if (comboChain == null || comboChain.Length == 0) Debug.LogWarning("[CombatController] No combo chain assigned.", this);

            if (damageReceiver != null) damageReceiver.HitTaken += OnHitTaken;
            if (health != null) health.Died += OnDied;
            if (hitbox != null) hitbox.HitLanded += OnHitLanded;
        }

        private void OnDestroy()
        {
            if (damageReceiver != null) damageReceiver.HitTaken -= OnHitTaken;
            if (health != null) health.Died -= OnDied;
            if (hitbox != null) hitbox.HitLanded -= OnHitLanded;
        }

        private void Update()
        {
            switch (State)
            {
                case CombatState.Idle: UpdateIdle(); break;
                case CombatState.Attacking: UpdateAttack(); break;
                case CombatState.Blocking: UpdateBlocking(); break;
                case CombatState.Parry: UpdateParry(); break;
                case CombatState.Stunned: UpdateStun(); break;
            }
        }

        private void UpdateIdle()
        {
            if (inputBridge == null) return;

            if (inputBridge.IsBlockHeld)
            {
                Phase = AttackPhase.None;
                if (stamina != null) stamina.RegenerationEnabled = false;
                SetState(CombatState.Blocking);
                return;
            }

            if (inputBridge.TryConsumeParry())
            {
                if (stamina != null && !stamina.TrySpend(parryStaminaCost)) return;
                phaseTimer = parryWindowDuration;
                SetState(CombatState.Parry);
                return;
            }

            if (inputBridge.TryConsumeAttack(out _)) StartAttack(0);
        }

        private void UpdateAttack()
        {
            phaseTimer -= Time.deltaTime;

            switch (Phase)
            {
                case AttackPhase.Windup:
                    if (phaseTimer <= 0f)
                    {
                        Phase = AttackPhase.Active;
                        phaseTimer = currentAttack.ActiveTime;
                        hitbox?.Activate(weapon, currentAttack, currentDamage);
                    }
                    break;

                case AttackPhase.Active:
                    if (phaseTimer <= 0f)
                    {
                        hitbox?.Deactivate();
                        Phase = AttackPhase.Recovery;
                        phaseTimer = currentAttack.RecoveryTime;
                    }
                    break;

                case AttackPhase.Recovery:
                    if (inputBridge != null && inputBridge.TryConsumeAttack(out _))
                    {
                        if (comboChain != null && comboIndex + 1 < comboChain.Length) StartAttack(comboIndex + 1);
                        break;
                    }
                    if (phaseTimer <= 0f) EndAttack();
                    break;
            }
        }

        private void UpdateBlocking()
        {
            if (stamina != null)
            {
                if (stamina.Current <= 0.01f)
                {
                    BreakBlock();
                    return;
                }
                stamina.TrySpend(blockStaminaCostPerSecond * Time.deltaTime);
            }

            if (inputBridge == null || !inputBridge.IsBlockHeld)
            {
                if (stamina != null) stamina.RegenerationEnabled = true;
                SetState(CombatState.Idle);
            }
        }

        private void UpdateParry()
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0f) SetState(CombatState.Idle);
        }

        private void UpdateStun()
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) SetState(CombatState.Idle);
        }

        private void StartAttack(int index)
        {
            if (comboChain == null || comboChain.Length == 0) return;

            var def = comboChain[index];
            var damage = weapon != null ? weapon.BaseDamage * def.DamageMultiplier : def.DamageMultiplier;
            if (stamina != null && !stamina.TrySpend(def.StaminaCost)) return;

            comboIndex = index;
            currentAttack = def;
            currentDamage = damage;
            Phase = AttackPhase.Windup;
            phaseTimer = def.WindupTime;
            SetState(CombatState.Attacking);
            AttackStarted?.Invoke(def, index);
        }

        private void EndAttack()
        {
            hitbox?.Deactivate();
            comboIndex = 0;
            currentAttack = null;
            Phase = AttackPhase.None;
            SetState(CombatState.Idle);
        }

        private void BreakBlock()
        {
            hitbox?.Deactivate();
            if (stamina != null) stamina.RegenerationEnabled = true;
            stunTimer = blockBreakStunDuration;
            SetState(CombatState.Stunned);
            BlockBroken?.Invoke();
        }

        private void SetState(CombatState state)
        {
            if (State == state) return;
            State = state;
            if (state == CombatState.Stunned) Stunned?.Invoke();
            StateChanged?.Invoke(state);
        }

        private T Resolve<T>(T serialized) where T : Component => serialized != null ? serialized : GetComponent<T>();

        private void OnHitLanded(DamageContext context, DamageResult result)
        {
            AttackResolved?.Invoke(context.Attack, comboIndex, result.Outcome);
        }

        private void OnHitTaken(DamageContext context, DamageResult result)
        {
            if (health != null && health.IsDead) return;

            if (result.Outcome == HitOutcome.Normal && State == CombatState.Attacking)
            {
                ApplyStun(hitStunDuration);
            }
        }

        private void OnDied()
        {
            hitbox?.Deactivate();
            stunTimer = Mathf.Infinity;
            SetState(CombatState.Stunned);
        }

        // ---- IDefenseState (the defender side of this actor) ----

        public bool OnBlocked(DamageContext context)
        {
            var broke = context.Attack != null && context.Attack.BreaksBlock;
            var cost = context.Attack != null ? context.Attack.BlockStaminaCost : 10f;

            if (!broke && (stamina == null || stamina.TrySpend(cost))) return true;

            BreakBlock();
            return false;
        }

        public void OnSuccessfulParry(DamageContext context)
        {
            Parried?.Invoke();

            if (context.Attacker != null && context.Attacker.TryGetComponent(out CombatController attackerController))
            {
                attackerController.ApplyStun(parryStunOnAttacker);
            }
        }

        public void ApplyStun(float duration)
        {
            hitbox?.Deactivate();
            inputBridge?.ClearBuffers();
            stunTimer = duration;
            SetState(CombatState.Stunned);
        }

        public void SetDodgeInvulnerable(bool value) => IsDodgeInvulnerable = value;

        /// <summary>
        /// Resets the combat state machine to Idle, clears stun, re-activates
        /// the hitbox if a weapon is assigned, and resets stamina. Used by the
        /// round system between rounds.
        /// </summary>
        public void ResetState()
        {
            hitbox?.Deactivate();
            inputBridge?.ClearBuffers();
            comboIndex = 0;
            currentAttack = null;
            Phase = AttackPhase.None;
            phaseTimer = 0f;
            stunTimer = 0f;
            IsBlockHeld = false;
            IsDodgeInvulnerable = false;
            if (stamina != null) stamina.RegenerationEnabled = true;
            SetState(CombatState.Idle);
        }

        /// <summary>
        /// Swaps the active weapon and its combo chain at runtime. Used by the
        /// loadout system to change the player's equipment between matches.
        /// Deactivates any in-progress attack and re-arms the hitbox.
        /// </summary>
        public void Equip(Weapon newWeapon, AttackDefinition[] newComboChain)
        {
            weapon = newWeapon;
            comboChain = newComboChain;
            if (hitbox != null) hitbox.Deactivate();
            if (weapon == null)
                Debug.LogWarning("[CombatController] Equip called with a null weapon.", this);
        }

        /// <summary>The currently equipped weapon, or null.</summary>
        public Weapon CurrentWeapon => weapon;
    }
}
