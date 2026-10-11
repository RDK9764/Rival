using UnityEngine;
using Rival.Actors;

namespace Rival.Combat
{
    /// <summary>
    /// Gives the primitive fighter models life without any real animation
    /// assets: idle breathing, walk/run limb swings, attack lunges, dodge
    /// spins, hit flinches, block poses and a death fall. Animates only the
    /// visual parts under "FighterModel" via local transforms, never the
    /// physics body, so it cannot fight the Rigidbody motor or the hitboxes.
    /// Re-resolves the model parts after ProceduralFighterBuilder rebuilds.
    /// </summary>
    public class ProceduralBodyAnimator : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)] private float intensity = 1f;

        private Transform modelRoot;
        private CharacterMotor motor;
        private CombatController combat;
        private HealthComponent health;

        private Transform torso;
        private Transform head;
        private Transform armL;
        private Transform armR;
        private Transform legL;
        private Transform legR;
        private Transform weapon;

        private float movePhase;
        private float hitShake;
        private float dodgeSpin;
        private bool dodging;
        private float deathTimer = -1f;
        private float lastHealth;

        private void Awake()
        {
            motor = GetComponent<CharacterMotor>();
            combat = GetComponent<CombatController>();
            health = GetComponent<HealthComponent>();
            CaptureParts();

            if (health != null)
            {
                lastHealth = health.Current;
                health.Changed += OnHealthChanged;
                health.Died += OnDied;
            }
            if (motor != null) motor.Dodged += OnDodged;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Changed -= OnHealthChanged;
                health.Died -= OnDied;
            }
            if (motor != null) motor.Dodged -= OnDodged;
        }

        private void CaptureParts()
        {
            modelRoot = transform.Find("FighterModel");
            if (modelRoot == null) return;

            torso = modelRoot.Find("Torso");
            head = modelRoot.Find("Head");
            armL = modelRoot.Find("Arm_L");
            armR = modelRoot.Find("Arm_R");
            legL = modelRoot.Find("Leg_L");
            legR = modelRoot.Find("Leg_R");
            weapon = FindWeaponPart(modelRoot);
        }

        private static readonly string[] WeaponNames = { "Sword", "AxeBlade", "SpearShaft", "Dagger_R", "HammerHead", "Fist_R" };

        private static Transform FindWeaponPart(Transform root)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                foreach (var name in WeaponNames)
                {
                    if (child.name == name) return child;
                }
            }
            return null;
        }

        private void OnDodged()
        {
            dodgeSpin = 180f;
            dodging = true;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (current < lastHealth)
            {
                hitShake = 1f;
            }
            else if (current > lastHealth && deathTimer >= 0f)
            {
                // Health restored (new round / next endless wave): get back up.
                deathTimer = -1f;
                if (modelRoot != null)
                {
                    modelRoot.localRotation = Quaternion.identity;
                    modelRoot.localPosition = Vector3.zero;
                }
            }
            lastHealth = current;
        }

        private void OnDied()
        {
            deathTimer = 0f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            if (intensity <= 0f) return;

            // Re-resolve after a procedural rebuild replaces the model.
            if (modelRoot == null) CaptureParts();
            if (modelRoot == null) return;

            // True horizontal speed from the physics body.
            float speed = 0f;
            if (motor != null && motor.Rigidbody != null)
            {
                var v = motor.Rigidbody.velocity;
                speed = new Vector3(v.x, 0f, v.z).magnitude;
            }

            bool attacking = combat != null && combat.State == CombatState.Attacking;
            bool blocking = combat != null && combat.State == CombatState.Blocking;
            bool stunned = combat != null && combat.State == CombatState.Stunned;
            bool parrying = combat != null && combat.State == CombatState.Parry;
            bool dead = deathTimer >= 0f;

            if (dead)
            {
                deathTimer += dt;
                UpdateDeath(deathTimer);
                return;
            }

            movePhase += dt * (4f + speed * 1.8f);
            hitShake = Mathf.Max(0f, hitShake - dt * 3.5f);
            if (dodging)
            {
                dodgeSpin = Mathf.Max(0f, dodgeSpin - dt * 480f);
                if (dodgeSpin <= 0f) dodging = false;
            }

            var sway = Mathf.Sin(t * 2.2f) * 0.02f * intensity;
            var motion = Mathf.Clamp01(speed / 6f);
            var step = Mathf.Sin(movePhase);

            // Whole body: dodge roll + breath sway + subtle run bob.
            modelRoot.localRotation = Quaternion.Euler(0f, dodgeSpin, 0f);
            modelRoot.localPosition = new Vector3(0f, sway + (motion > 0f ? Mathf.Abs(step) * 0.02f : 0f), 0f);

            if (torso != null)
            {
                float rx = 0f;
                rx += motion * -6f;                                   // lean into the run
                if (stunned) rx += 22f;
                if (parrying) rx -= 8f;
                if (blocking) rx -= 6f;
                if (attacking && combat != null && combat.Phase == AttackPhase.Active) rx -= 14f;
                mx(torso, rx + HitJitter(), 0f, 0f);
            }

            if (head != null)
            {
                mx(head, -3f * motion - (stunned ? 14f : 0f), 0f, step * 2f * motion);
            }

            float armXL = ArmSwing(step, motion) * -1f;
            float armXR = ArmSwing(step, motion) * 1f - (blocking ? 50f : 0f);

            if (armL != null) mx(armL, armXL + (parrying ? -30f : 0f) + (stunned ? 24f : 0f), 0f, 10f);
            if (armR != null) mx(armR, armXR + (parrying ? -40f : 0f) + (stunned ? 24f : 0f), 0f, -10f);

            if (legL != null) mx(legL, step * -40f * motion, 0f, 0f);
            if (legR != null) mx(legR, step * 40f * motion, 0f, 0f);

            // Weapon lunge arcs through the attack phases.
            if (weapon != null && attacking && combat != null)
            {
                var p = combat.Phase;
                var swing = p == AttackPhase.Active ? -65f : p == AttackPhase.Windup ? 20f : 0f;
                mx(weapon, swing, 0f, p == AttackPhase.Active ? 25f : 0f);
            }
        }

        private void UpdateDeath(float t)
        {
            var fall = Mathf.Clamp01(t * 1.4f);
            modelRoot.localRotation = Quaternion.Euler(fall * -85f, 0f, 0f);
            modelRoot.localPosition = new Vector3(0f, -fall * 0.15f, fall * 0.1f);
            if (torso != null) mx(torso, -20f * fall, 0f, 0f);
        }

        private float HitJitter()
        {
            if (hitShake <= 0f) return 0f;
            return (Mathf.Sin(Time.unscaledTime * 90f) * 12f + Random.Range(-4f, 4f)) * hitShake;
        }

        private float ArmSwing(float step, float motion)
        {
            return step * 34f * motion;
        }

        private static void mx(Transform part, float rx, float ry, float rz)
        {
            part.localRotation = Quaternion.Euler(rx, ry, rz);
        }
    }
}