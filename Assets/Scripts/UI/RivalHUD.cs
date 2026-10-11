using UnityEngine;
using UnityEngine.UI;
using Rival.Combat;
using Rival.Enemy;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Enhanced rival HUD with health bar, actor name, weapon name, and
    /// archetype style label. Positioned top-right. Mirrors PlayerHUD's
    /// structure but anchored to the right side.
    /// </summary>
    public class RivalHUD : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private string actorName = "RIVAL";
        [SerializeField] private string styleName = "";

        private PlayerLoadoutManager loadout;
        private Image healthFill;
        private Text healthLabel;
        private Text nameLabel;
        private Text styleLabel;
        private Text weaponLabel;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<HealthComponent>();
            BuildHud();
            if (health != null) health.Changed += OnHealthChanged;
            Refresh();
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out loadout))
            {
                loadout.Changed += OnLoadoutChanged;
                OnLoadoutChanged();
            }
        }

        private void OnDestroy()
        {
            if (health != null) health.Changed -= OnHealthChanged;
            if (loadout != null) loadout.Changed -= OnLoadoutChanged;
        }

        private void OnLoadoutChanged()
        {
            var entry = loadout.GetCatalogEntry(loadout.EquippedWeaponId);
            if (entry != null && entry.weapon != null)
                weaponLabel.text = $"MIRROR: {entry.weapon.WeaponName}";
        }

        public void SetInfo(string name, string style, string weapon)
        {
            actorName = name;
            styleName = style;
            if (nameLabel != null) nameLabel.text = name;
            if (styleLabel != null) styleLabel.text = style;
            if (weaponLabel != null) weaponLabel.text = weapon;
        }

        private void Refresh()
        {
            if (health != null) OnHealthChanged(health.Current, health.Max);
        }

        private void OnHealthChanged(float current, float max)
        {
            healthFill.fillAmount = max > 0f ? current / max : 0f;
            healthLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void BuildHud()
        {
            var canvasGo = new GameObject("RivalHUDCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.GetComponent<RectTransform>();

            var panel = CreatePanel(root, "RivalPanel", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-10f, -10f), new Vector2(360f, 100f));

            nameLabel = CreateText(panel, "Name", actorName, 18, new Vector2(0f, 28f));
            nameLabel.color = new Color(1f, 0.35f, 0.35f);
            styleLabel = CreateText(panel, "Style", styleName, 14, new Vector2(0f, 8f));
            styleLabel.color = new Color(0.6f, 0.6f, 0.6f);
            weaponLabel = CreateText(panel, "Weapon", "", 12, new Vector2(0f, -10f));
            weaponLabel.color = new Color(0.5f, 0.5f, 0.5f);
            healthFill = CreateBar(panel, "RivalHP", new Color(0.85f, 0.2f, 0.2f),
                new Vector2(0f, -32f), new Vector2(340f, 16f), out healthLabel);
        }

        private static Image CreateBar(RectTransform parent, string name, Color fillColor,
            Vector2 anchoredPosition, Vector2 size, out Text label)
        {
            var barGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.SetParent(parent, false);
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = anchoredPosition;
            barRect.sizeDelta = size;
            barGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.SetParent(barRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = fillColor;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(barRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            label = text;

            return fill;
        }

        private static GameObject CreatePanel(RectTransform parent, string name,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            return go;
        }

        private static Text CreateText(RectTransform parent, string name, string content,
            int size, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(320f, 24f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            return text;
        }
    }
}
