using UnityEngine;
using UnityEngine.UI;
using Rival.Actors;
using Rival.Combat;
using Rival.Enemy;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Enhanced player HUD with health bar, stamina bar, actor name,
    /// weapon name, and round score. Positioned bottom-left. All runtime-built.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private StaminaComponent stamina;
        [SerializeField] private string actorName = "PLAYER";

        private PlayerLoadoutManager loadout;

        private Image healthFill;
        private Image staminaFill;
        private Text healthLabel;
        private Text staminaLabel;
        private Text nameLabel;
        private Text weaponLabel;
        private Text roundLabel;
        private GameObject canvasGo;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<HealthComponent>();
            if (stamina == null) stamina = GetComponentInParent<StaminaComponent>();
            BuildHud();
            if (health != null) health.Changed += OnHealthChanged;
            if (stamina != null) stamina.Changed += OnStaminaChanged;
            RefreshAll();
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
            if (stamina != null) stamina.Changed -= OnStaminaChanged;
            if (loadout != null) loadout.Changed -= OnLoadoutChanged;
        }

        private void OnLoadoutChanged()
        {
            var entry = loadout.GetCatalogEntry(loadout.EquippedWeaponId);
            if (entry != null && entry.weapon != null)
                SetWeaponName(entry.weapon.WeaponName);
        }

        public void SetWeaponName(string name)
        {
            if (weaponLabel != null) weaponLabel.text = name;
        }

        public void SetRoundScore(int playerWins, int rivalWins, int roundsToWin)
        {
            if (roundLabel != null)
                roundLabel.text = $"{playerWins} — {rivalWins}  (Best of {roundsToWin * 2 - 1})";
        }

        private void RefreshAll()
        {
            if (health != null) OnHealthChanged(health.Current, health.Max);
            if (stamina != null) OnStaminaChanged(stamina.Current, stamina.Max);
        }

        private void OnHealthChanged(float current, float max)
        {
            healthFill.fillAmount = max > 0f ? current / max : 0f;
            healthLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void OnStaminaChanged(float current, float max)
        {
            staminaFill.fillAmount = max > 0f ? current / max : 0f;
            staminaLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void BuildHud()
        {
            canvasGo = new GameObject("PlayerHUDCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.GetComponent<RectTransform>();

            // Background panel
            var panel = CreatePanel(root, "PlayerPanel", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10f, -10f), new Vector2(360f, 110f));

            nameLabel = CreateText(panel, "Name", actorName, 18, new Vector2(0f, 30f));
            nameLabel.color = new Color(0.3f, 0.7f, 1f);
            weaponLabel = CreateText(panel, "Weapon", "", 14, new Vector2(0f, 10f));
            weaponLabel.color = new Color(0.6f, 0.6f, 0.6f);
            healthFill = CreateBar(panel, "HP", new Color(0.85f, 0.2f, 0.2f), new Vector2(0f, -12f), new Vector2(340f, 18f), out healthLabel);
            staminaFill = CreateBar(panel, "SP", new Color(0.2f, 0.7f, 0.9f), new Vector2(0f, -36f), new Vector2(340f, 12f), out staminaLabel);
            roundLabel = CreateText(panel, "Round", "", 13, new Vector2(0f, -55f));
            roundLabel.color = new Color(0.5f, 0.5f, 0.5f);
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
            text.fontSize = name == "HP" ? 13 : 11;
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
