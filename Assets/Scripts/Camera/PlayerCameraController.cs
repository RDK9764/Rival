using UnityEngine;
using Rival.Input;
using Rival.Utilities;

namespace Rival.Camera
{
    /// <summary>
    /// Third-person orbit camera driven by IPlayerInput. Registers itself as
    /// ICameraRig so player movement can be camera-relative without depending
    /// on this concrete type. Smooths follow and look targets, and pushes in
    /// toward the subject when a wall occludes the view.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class PlayerCameraController : MonoBehaviour, ICameraRig
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private CameraConfig cameraConfig;

        public Vector3 Forward => transform.forward;
        public Vector3 Right => transform.right;

        private IPlayerInput input;
        private float yaw;
        private float pitch;
        private float currentDistance;
        private Vector3 positionVelocity;
        private Vector3 lookPointVelocity;
        private Vector3 currentLookPoint;

        private void Awake()
        {
            currentDistance = cameraConfig != null ? cameraConfig.startDistance : 5f;
            pitch = cameraConfig != null ? cameraConfig.startPitch : 12f;
            currentLookPoint = lookTarget != null ? lookTarget.position : Vector3.zero;
            ServiceLocator.TryGet(out input);
            ServiceLocator.Register<ICameraRig>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ICameraRig>(this);
        }

        private void Start()
        {
            if (followTarget != null) yaw = followTarget.eulerAngles.y;
            transform.position = DesiredPosition();
            currentLookPoint = lookTarget != null ? lookTarget.position : Vector3.zero;
        }

        private void Update()
        {
            if (cameraConfig == null || followTarget == null || lookTarget == null) return;

            if (input == null) ServiceLocator.TryGet(out input); // retry until the player's input exists

            if (input != null)
            {
                var look = input.Look;
                yaw += look.x * cameraConfig.sensitivityX;
                pitch -= look.y * cameraConfig.sensitivityY * (cameraConfig.invertY ? -1f : 1f);
                pitch = Mathf.Clamp(pitch, cameraConfig.pitchMin, cameraConfig.pitchMax);

                if (input.ZoomDelta != 0f)
                {
                    currentDistance = Mathf.Clamp(
                        currentDistance - input.ZoomDelta * cameraConfig.zoomStep,
                        cameraConfig.minDistance,
                        cameraConfig.maxDistance);
                }
            }

            var desired = ResolveOcclusion(DesiredPosition());
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref positionVelocity, cameraConfig.positionDamping);

            currentLookPoint = Vector3.SmoothDamp(currentLookPoint, lookTarget.position, ref lookPointVelocity, cameraConfig.lookDamping);
            transform.LookAt(currentLookPoint);
        }

        private Vector3 DesiredPosition()
        {
            return followTarget.position + Quaternion.Euler(pitch, yaw, 0f) * (Vector3.back * currentDistance);
        }

        private Vector3 ResolveOcclusion(Vector3 desired)
        {
            if (cameraConfig.occlusionLayers.value == 0) return desired;

            var direction = desired - followTarget.position;
            var distance = direction.magnitude;
            direction.Normalize();

            if (Physics.SphereCast(followTarget.position, cameraConfig.collisionRadius, direction, out var hit, distance, cameraConfig.occlusionLayers, QueryTriggerInteraction.Ignore))
            {
                desired = followTarget.position + direction * Mathf.Max(0.2f, hit.distance - cameraConfig.collisionRadius);
            }

            return desired;
        }
    }
}
