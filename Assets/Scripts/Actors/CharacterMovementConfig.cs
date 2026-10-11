using UnityEngine;

namespace Rival.Actors
{
    /// <summary>
    /// All tunable locomotion values in one asset. ScriptableObjects let a
    /// designer change game feel without touching code, and let future enemy
    /// archetypes reuse this exact code with different data.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterMovementConfig", menuName = "Rival/Actors/Character Movement Config")]
    public class CharacterMovementConfig : ScriptableObject
    {
        [Header("Ground Movement")]
        public float moveSpeed = 6f;
        public float acceleration = 14f;
        public float turnSpeed = 12f;
        public float sprintMultiplier = 1.6f;
        public float airControl = 0.6f;

        [Header("Jump")]
        public float jumpHeight = 1.2f;
        public float jumpStaminaCost = 0f;

        [Header("Gravity")]
        public float gravityScale = 2.2f;
        public float maxFallSpeed = 30f;

        [Header("Dash")]
        public float dashSpeed = 20f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 1f;
        public float dashStaminaCost = 20f;

        [Header("Dodge / Roll")]
        public float dodgeSpeed = 12f;
        public float dodgeDuration = 0.35f;
        public float dodgeCooldown = 1.2f;
        public float dodgeStaminaCost = 25f;

        [Header("Stamina Costs")]
        public float sprintStaminaCostPerSecond = 12f;

        [Header("Ground Detection")]
        public float groundCheckRadius = 0.35f;
        public float groundCheckDistance = 0.35f;
        public LayerMask groundLayers = ~0;
    }
}
