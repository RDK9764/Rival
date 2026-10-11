using UnityEngine;
using Rival.Combat;
using Rival.Actors;

namespace Rival.Combat
{
    /// <summary>
    /// Visual indicator for the parry window. Shows a flash ring around the
    /// character when the parry window is active, and changes color on
    /// successful parry (green) vs missed (red fade). Uses a scaled
    /// LineRenderer circle that expands and fades.
    /// </summary>
    public class ParryIndicator : MonoBehaviour
    {
        [SerializeField] private CombatController combat;
        [SerializeField, Range(0.3f, 2f)] private float ringRadius = 1f;
        [SerializeField, Range(4, 32)] private int ringSegments = 16;
        [SerializeField, Range(0.1f, 0.5f)] private float flashDuration = 0.2f;
        [SerializeField] private Color parryWindowColor = new(1f, 0.9f, 0.3f, 0.8f);
        [SerializeField] private Color parrySuccessColor = new(0.2f, 1f, 0.3f, 1f);
        [SerializeField] private Color parryFailColor = new(1f, 0.2f, 0.2f, 0.6f);

        private LineRenderer lineRenderer;
        private float timer;
        private bool showing;
        private Color targetColor;

        private void Awake()
        {
            if (combat == null) combat = GetComponent<CombatController>();
            CreateRing();
            lineRenderer.enabled = false;
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.StateChanged += OnStateChanged;
                combat.Parried += OnParried;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.StateChanged -= OnStateChanged;
                combat.Parried -= OnParried;
            }
        }

        private void Update()
        {
            if (!showing) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                showing = false;
                lineRenderer.enabled = false;
                return;
            }

            float t = timer / flashDuration;
            var color = targetColor;
            color.a *= t;
            for (int i = 0; i <= ringSegments; i++)
            {
                lineRenderer.SetPosition(i, GetRingPoint(i, t));
            }
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        private void OnStateChanged(CombatState newState)
        {
            if (newState == CombatState.Parry)
            {
                ShowRing(parryWindowColor);
            }
        }

        private void OnParried()
        {
            ShowRing(parrySuccessColor);
        }

        private void ShowRing(Color color)
        {
            targetColor = color;
            timer = flashDuration;
            showing = true;
            lineRenderer.enabled = true;
        }

        private void CreateRing()
        {
            var go = new GameObject("ParryRing");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.5f;

            lineRenderer = go.AddComponent<LineRenderer>();
            lineRenderer.positionCount = ringSegments + 1;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = 0.06f;
            lineRenderer.endWidth = 0.06f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = parryWindowColor;
            lineRenderer.endColor = parryWindowColor;
            lineRenderer.enabled = false;

            for (int i = 0; i <= ringSegments; i++)
            {
                lineRenderer.SetPosition(i, GetRingPoint(i, 1f));
            }
        }

        private Vector3 GetRingPoint(int index, float scale)
        {
            float angle = (float)index / ringSegments * Mathf.PI * 2f;
            float r = ringRadius * scale;
            return new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
        }
    }
}
