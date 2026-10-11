using UnityEngine;
using UnityEngine.UI;
using Rival.Combat;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Tracks consecutive hits landed by an actor and displays a combo counter.
    /// Resets when the actor stops attacking for a configurable window or when
    /// the actor takes damage. Attaches to the player GameObject.
    /// </summary>
    public class ComboCounter : MonoBehaviour
    {
        [SerializeField] private CombatController combat;
        [SerializeField] private HealthComponent health;
        [SerializeField, Range(0.5f, 3f)] private float comboTimeout = 1.5f;
        [SerializeField] private int fontSize = 36;

        private int count;
        private float timer;
        private bool active;

        private GameObject canvasGo;
        private Text comboLabel;
        private Camera mainCam;

        private void Start()
        {
            mainCam = UnityEngine.Camera.main;
            if (combat == null) combat = GetComponentInParent<CombatController>();
            if (health == null) health = GetComponentInParent<HealthComponent>();

            if (combat != null) combat.AttackResolved += OnAttackResolved;
            if (health != null) health.HitTaken += OnHitTaken;

            BuildUi();
            canvasGo.SetActive(false);
        }

        private void OnDestroy()
        {
            if (combat != null) combat.AttackResolved -= OnAttackResolved;
            if (health != null) health.HitTaken -= OnHitTaken;
        }

        private void Update()
        {
            if (!active) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) Hide();
        }

        private void OnAttackResolved(AttackDefinition attack, int comboIndex, HitOutcome outcome)
        {
            if (outcome == HitOutcome.Normal || outcome == HitOutcome.BlockBroken)
            {
                count++;
                timer = comboTimeout;
                active = true;
                Show();
            }
        }

        private void OnHitTaken(DamageContext context, DamageResult result)
        {
            if (count > 0) Hide();
        }

        private void Show()
        {
            canvasGo.SetActive(true);
            comboLabel.text = $"{count} HIT COMBO";
        }

        private void Hide()
        {
            active = false;
            count = 0;
            canvasGo.SetActive(false);
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("ComboCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.GetComponent<RectTransform>();

            var labelGo = new GameObject("ComboLabel", typeof(RectTransform), typeof(Text));
            var rect = labelGo.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(30f, -80f);
            rect.sizeDelta = new Vector2(400f, 50f);

            comboLabel = labelGo.GetComponent<Text>();
            comboLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            comboLabel.fontSize = fontSize;
            comboLabel.alignment = TextAnchor.MiddleLeft;
            comboLabel.color = new Color(1f, 0.85f, 0.2f);
            comboLabel.text = "";
        }
    }
}
