using UnityEngine;
using Rival.Combat;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Flashes a renderer's material white briefly when the actor takes damage.
    /// Uses MaterialPropertyBlock to avoid material instance allocation. Attaches
    /// to the same GameObject as the HealthComponent or any child renderer.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private Color flashColor = Color.white;

        private Renderer[] renderers;
        private MaterialPropertyBlock propBlock;
        private float flashTimer;
        private bool flashing;

        private static readonly int ColorID = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<HealthComponent>();
            renderers = GetComponentsInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (health != null) health.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (health != null) health.Changed -= OnHealthChanged;
            ResetFlash();
        }

        private void Update()
        {
            if (!flashing) return;

            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) ResetFlash();
        }

        private void OnHealthChanged(float current, float max)
        {
            if (health == null || health.IsDead) return;

            flashing = true;
            flashTimer = flashDuration;

            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(ColorID, flashColor);
                r.SetPropertyBlock(propBlock);
            }
        }

        private void ResetFlash()
        {
            flashing = false;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.Clear();
                r.SetPropertyBlock(propBlock);
            }
        }
    }
}
