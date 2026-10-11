using UnityEngine;

namespace Rival.Utilities
{
    /// <summary>
    /// Pure movement-math helpers. Kept free of MonoBehaviour/Unity engine state
    /// so they are unit-testable and reusable by any actor (player or AI).
    /// </summary>
    public static class DirectionMapper
    {
        /// <summary>
        /// Converts 2D input (x = strafe, y = forward) into a world-space
        /// direction using the camera's axes. Falls back to world axes when the
        /// camera axes are degenerate (e.g. looking straight up or down).
        /// Returns zero for idle input.
        /// </summary>
        public static Vector3 FromCamera(Vector2 input, Vector3 forward, Vector3 right)
        {
            if (input.sqrMagnitude <= 0.01f) return Vector3.zero;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            if (forward.sqrMagnitude < 0.01f || right.sqrMagnitude < 0.01f)
            {
                return new Vector3(input.x, 0f, input.y).normalized;
            }

            var direction = forward * input.y + right * input.x;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
        }
    }
}
