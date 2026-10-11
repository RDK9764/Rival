using System;
using UnityEngine;

namespace Rival.Actors
{
    /// <summary>
    /// Executes all locomotion physics: acceleration, custom gravity, jump,
    /// dash and dodge bursts. Receives intents, owns no input and no game rules,
    /// so any brain — human controller or future AI — can drive this motor.
    /// Shared by the player and every rival.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMotor : MonoBehaviour
    {
        public Rigidbody Rigidbody { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsDodging { get; private set; }

        /// <summary>Fired the moment a dodge starts. Combat uses it for i-frames.</summary>
        public event Action Dodged;
        /// <summary>Fired when the dodge burst ends.</summary>
        public event Action DodgeEnded;

        private CharacterMovementConfig config;
        private float dashTimer;
        private float dodgeTimer;
        private float dashCooldownTimer;
        private float dodgeCooldownTimer;
        private Vector3 dashVelocity;
        private Vector3 dodgeVelocity;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.freezeRotation = true;
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Rigidbody.useGravity = false;
        }

        public void Initialize(CharacterMovementConfig movementConfig)
        {
            config = movementConfig;
            if (config == null) Debug.LogWarning("[CharacterMotor] No movement config assigned.", this);
        }

        private void FixedUpdate()
        {
            if (config == null) return;

            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.fixedDeltaTime);
            dodgeCooldownTimer = Mathf.Max(0f, dodgeCooldownTimer - Time.fixedDeltaTime);
            UpdateGrounded();

            if (IsDashing)
            {
                Rigidbody.velocity = dashVelocity;
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f) IsDashing = false;
            }
            else if (IsDodging)
            {
                Rigidbody.velocity = dodgeVelocity;
                dodgeTimer -= Time.fixedDeltaTime;
                if (dodgeTimer <= 0f)
                {
                    IsDodging = false;
                    DodgeEnded?.Invoke();
                }
            }
        }

        /// <summary>Accelerates horizontally toward direction * speed, then applies gravity.</summary>
        public void Move(Vector3 direction, float speed, float deltaTime)
        {
            if (config == null || IsDashing || IsDodging) return;

            var velocity = Rigidbody.velocity;
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);

            var acceleration = config.acceleration * (IsGrounded ? 1f : config.airControl);
            horizontal = Vector3.MoveTowards(horizontal, direction * speed, acceleration * deltaTime);

            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
            velocity.y += Physics.gravity.y * config.gravityScale * deltaTime;
            velocity.y = Mathf.Max(velocity.y, -config.maxFallSpeed);

            Rigidbody.velocity = velocity;
        }

        public bool TryJump()
        {
            if (config == null || !IsGrounded) return false;

            var velocity = Rigidbody.velocity;
            velocity.y = Mathf.Sqrt(2f * config.jumpHeight * -Physics.gravity.y * config.gravityScale);
            Rigidbody.velocity = velocity;
            IsGrounded = false;
            return true;
        }

        public bool TryDash(Vector3 direction)
        {
            if (config == null || dashCooldownTimer > 0f || IsDashing || IsDodging) return false;

            dashVelocity = (direction.sqrMagnitude > 0.01f ? direction : transform.forward).normalized * config.dashSpeed;
            dashTimer = config.dashDuration;
            dashCooldownTimer = config.dashCooldown;
            IsDashing = true;
            return true;
        }

        public bool TryDodge(Vector3 direction)
        {
            if (config == null || dodgeCooldownTimer > 0f || IsDodging || IsDashing) return false;

            dodgeVelocity = (direction.sqrMagnitude > 0.01f ? direction : -transform.forward).normalized * config.dodgeSpeed;
            dodgeTimer = config.dodgeDuration;
            dodgeCooldownTimer = config.dodgeCooldown;
            IsDodging = true;
            Dodged?.Invoke();
            return true;
        }

        private void UpdateGrounded()
        {
            var origin = transform.position + Vector3.up * 0.05f;
            IsGrounded = Physics.SphereCast(
                origin,
                config.groundCheckRadius,
                Vector3.down,
                out _,
                config.groundCheckDistance,
                config.groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            var origin = transform.position + Vector3.up * 0.05f;
            Gizmos.DrawWireSphere(origin + Vector3.down * config.groundCheckDistance, config.groundCheckRadius);
        }
    }
}
