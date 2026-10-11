using UnityEngine;

namespace Rival.Camera
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Rival/Camera/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Orbit")]
        public float sensitivityX = 3f;
        public float sensitivityY = 3f;
        public bool invertY;

        [Header("Pitch")]
        public float startPitch = 12f;
        public float pitchMin = -25f;
        public float pitchMax = 60f;

        [Header("Distance")]
        public float startDistance = 5f;
        public float minDistance = 2f;
        public float maxDistance = 8f;
        public float zoomStep = 0.5f;

        [Header("Smoothing")]
        public float positionDamping = 0.15f;
        public float lookDamping = 0.08f;

        [Header("Occlusion")]
        public float collisionRadius = 0.15f;
        public LayerMask occlusionLayers;
    }
}
