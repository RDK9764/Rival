using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Central game-feel service. Provides camera shake and manages
    /// hit-flash on renderers. Registered as IGameFeelService so combat
    /// event subscribers can trigger feedback without knowing the camera.
    /// </summary>
    public interface IGameFeelService
    {
        void ShakeCamera(float intensity, float duration);
    }

    public class GameFeelService : MonoBehaviour, IGameFeelService
    {
        [SerializeField, Range(0.01f, 0.5f)] private float defaultShakeIntensity = 0.12f;
        [SerializeField, Range(0.05f, 0.5f)] private float defaultShakeDuration = 0.15f;

        private Transform cameraTransform;
        private Vector3 originalCameraLocalPos;
        private float shakeTimer;
        private float shakeIntensity;
        private bool shaking;

        private void Awake()
        {
            ServiceLocator.Register<IGameFeelService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IGameFeelService>(this);
        }

        private void Start()
        {
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                cameraTransform = cam.transform;
                originalCameraLocalPos = cameraTransform.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (!shaking || cameraTransform == null) return;

            shakeTimer -= Time.unscaledDeltaTime;
            if (shakeTimer <= 0f)
            {
                shaking = false;
                cameraTransform.localPosition = originalCameraLocalPos;
                return;
            }

            var offset = Random.insideUnitSphere * shakeIntensity;
            cameraTransform.localPosition = originalCameraLocalPos + offset;
        }

        public void ShakeCamera(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
            shaking = true;
        }

        public void ShakeCameraDefault() => ShakeCamera(defaultShakeIntensity, defaultShakeDuration);
    }
}
