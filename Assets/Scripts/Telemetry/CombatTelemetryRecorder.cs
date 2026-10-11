using UnityEngine;
using Rival.Actors;
using Rival.Combat;

namespace Rival.Telemetry
{
    /// <summary>
    /// One per fighting actor. Translates that actor's combat events into
    /// structured telemetry. It reports behavior (attacks, blocks, parries,
    /// dodges, hits taken, distance, outcomes) — never button presses.
    /// </summary>
    [RequireComponent(typeof(CombatController))]
    public class CombatTelemetryRecorder : MonoBehaviour
    {
        [Tooltip("The opponent. Used to record distance at each event. Leave empty to record -1.")]
        [SerializeField] private Transform opponent;
        [Tooltip("Actor identity recorded on every event. Empty = uses the GameObject name.")]
        [SerializeField] private string actorName;

        private CombatController combat;
        private CharacterMotor motor;
        private HealthComponent health;
        private DamageReceiver damageReceiver;

        /// <summary>Runtime identity override for roster archetypes (RivalMatchSetup). Empty values fall back to the GameObject name.</summary>
        public void SetActorName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            actorName = name;
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(actorName)) actorName = gameObject.name;

            combat = GetComponent<CombatController>();
            motor = GetComponent<CharacterMotor>();
            health = GetComponent<HealthComponent>();
            damageReceiver = GetComponent<DamageReceiver>();

            combat.AttackStarted += OnAttackStarted;
            combat.AttackResolved += OnAttackResolved;
            combat.StateChanged += OnStateChanged;
            combat.BlockBroken += OnBlockBroken;
            if (health != null) health.Died += OnDied;
            if (motor != null) motor.Dodged += OnDodged;
            if (damageReceiver != null) damageReceiver.HitTaken += OnHitTaken;
        }

        private void OnDestroy()
        {
            if (combat == null) return;
            combat.AttackStarted -= OnAttackStarted;
            combat.AttackResolved -= OnAttackResolved;
            combat.StateChanged -= OnStateChanged;
            combat.BlockBroken -= OnBlockBroken;
            if (health != null) health.Died -= OnDied;
            if (motor != null) motor.Dodged -= OnDodged;
            if (damageReceiver != null) damageReceiver.HitTaken -= OnHitTaken;
        }

        private void OnAttackStarted(AttackDefinition def, int comboIndex)
            => Record(CombatEventType.AttackPerformed, comboIndex: comboIndex, isHeavy: def.AttackType == AttackType.Heavy);

        private void OnAttackResolved(AttackDefinition def, int comboIndex, HitOutcome outcome)
            => Record(CombatEventType.AttackResolved, comboIndex: comboIndex, isHeavy: def.AttackType == AttackType.Heavy, outcome: outcome);

        private void OnStateChanged(CombatState state)
        {
            switch (state)
            {
                case CombatState.Blocking: Record(CombatEventType.BlockPerformed); break;
                case CombatState.Parry: Record(CombatEventType.ParryPerformed); break;
            }
        }

        private void OnBlockBroken() => Record(CombatEventType.BlockBroken);

        private void OnHitTaken(DamageContext context, DamageResult result)
            => Record(CombatEventType.HitTaken, damage: result.FinalDamage, outcome: result.Outcome);

        private void OnDied() => Record(CombatEventType.Died);

        private void OnDodged() => Record(CombatEventType.DodgePerformed);

        private void Record(CombatEventType type, float damage = 0f, int comboIndex = -1, bool isHeavy = false, HitOutcome outcome = HitOutcome.Normal)
        {
            if (combat == null) return;

            Telemetry.Record(new CombatEvent
            {
                Timestamp = Time.time,
                Actor = actorName,
                Type = type,
                Distance = opponent != null ? Vector3.Distance(transform.position, opponent.position) : -1f,
                Damage = damage,
                ComboIndex = comboIndex,
                IsHeavy = isHeavy,
                Outcome = outcome,
                HealthFraction = health != null && health.Max > 0f ? health.Current / health.Max : 0f
            });
        }
    }
}
