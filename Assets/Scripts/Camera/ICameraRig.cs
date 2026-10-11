using UnityEngine;

namespace Rival.Camera
{
    /// <summary>
    /// Minimal contract for the camera rig that gameplay code orients around.
    /// Player movement depends on this interface, so the implementation can be
    /// swapped (orbit, shoulder, or a Cinemachine adapter) without touching
    /// player code.
    /// </summary>
    public interface ICameraRig
    {
        Vector3 Forward { get; }
        Vector3 Right { get; }
    }
}
