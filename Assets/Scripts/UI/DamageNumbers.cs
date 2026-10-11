using UnityEngine;
using UnityEngine.UI;
using Rival.Combat;

namespace Rival.UI
{
    /// <summary>
    /// Spawns floating damage numbers at the hit location. Attaches to the actor
    /// that receives damage (subscribes to its DamageReceiver.HitTaken). Numbers
    /// float upward, fade, and destroy themselves.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        [SerializeField] private DamageReceiver damageReceiver;
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private float spreadX = 0.4f;
        [SerializeField] private int fontSize = 28;

        private Camera mainCam;

        private void Start()
        {
            mainCam = UnityEngine.Camera.main;
            if (damageReceiver == null) damageReceiver = GetComponentInParent<DamageReceiver>();
            if (damageReceiver != null) damageReceiver.HitTaken += OnHitTaken;
        }

        private void OnDestroy()
        {
            if (damageReceiver != null) damageReceiver.HitTaken -= OnHitTaken;
        }

        private void OnHitTaken(DamageContext context, DamageResult result)
        {
            if (result.Outcome == HitOutcome.Evaded || result.Outcome == HitOutcome.Parried) return;
            if (result.FinalDamage <= 0f) return;

            var worldPos = transform.position + Vector3.up * 1.8f
                + new Vector3(Random.Range(-spreadX, spreadX), 0, 0);
            SpawnNumber(worldPos, Mathf.CeilToInt(result.FinalDamage), result.Outcome);
        }

        private void SpawnNumber(Vector3 worldPos, int amount, HitOutcome outcome)
        {
            var canvasGo = new GameObject("DmgNum", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 50f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = GetColor(outcome);
            text.text = amount.ToString();

            canvasGo.transform.position = worldPos;
            Destroy(canvasGo, lifetime);

            var mover = canvasGo.AddComponent<DamageNumberMover>();
            mover.Init(floatSpeed, lifetime);
        }

        private static Color GetColor(HitOutcome outcome)
        {
            return outcome switch
            {
                HitOutcome.Normal => new Color(1f, 1f, 1f),
                HitOutcome.Blocked => new Color(0.5f, 0.7f, 1f),
                HitOutcome.BlockBroken => new Color(1f, 0.6f, 0.1f),
                _ => Color.white
            };
        }
    }

    public class DamageNumberMover : MonoBehaviour
    {
        private float speed;
        private float timer;
        private float maxLife;
        private Text text;

        public void Init(float speed, float lifetime)
        {
            this.speed = speed;
            maxLife = lifetime;
            timer = lifetime;
            text = GetComponentInChildren<Text>();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            transform.position += Vector3.up * speed * Time.deltaTime;

            if (text != null)
            {
                var alpha = Mathf.Clamp01(timer / maxLife);
                var c = text.color;
                c.a = alpha;
                text.color = c;
            }
        }
    }
}
