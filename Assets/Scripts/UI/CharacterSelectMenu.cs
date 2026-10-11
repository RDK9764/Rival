using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Enemy;
using Rival.Input;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Enhanced character-select menu with archetype info panel showing
    /// personality style, weapon type, engagement range, and accent color
    /// preview. Runtime-built. Navigates with IMenuInput.
    /// </summary>
    public class CharacterSelectMenu : MonoBehaviour
    {
        [SerializeField] private RivalMatchSetup matchSetup;
        [SerializeField, Range(0.05f, 0.6f)] private float navigationRepeatDelay = 0.22f;

        private IMenuInput input;
        private ITimeService time;
        private int selectedIndex;
        private float navigationTimer;
        private bool open;

        private GameObject canvasGo;
        private Text[] optionLabels;
        private Text styleLabel;
        private Text weaponLabel;
        private Text rangeLabel;
        private Text speedLabel;
        private Image colorPreview;
        private Image[] optionHighlights;

        private void Awake()
        {
            if (input == null) ServiceLocator.TryGet(out input);
            if (time == null) ServiceLocator.TryGet(out time);
        }

        private void Start()
        {
            if (matchSetup == null) ServiceLocator.TryGet(out matchSetup);
            if (input == null) ServiceLocator.TryGet(out input);
            if (time == null) ServiceLocator.TryGet(out time);

            if (matchSetup == null || matchSetup.ArchetypeCount == 0) return;
            if (input == null) return;

            BuildUi();
            selectedIndex = matchSetup.CurrentIndex;
            Open();
        }

        private void OnDestroy() => Close();

        private void Open()
        {
            open = true;
            input.SetMenuActive(true);
            time?.SetScale(0f);
            canvasGo.SetActive(true);
            input.ConfirmPressed += OnConfirmPressed;
            input.BackPressed += OnBackPressed;
            Refresh();
        }

        private void Close()
        {
            if (!open) return;
            open = false;
            input.ConfirmPressed -= OnConfirmPressed;
            input.BackPressed -= OnBackPressed;
            input.SetMenuActive(false);
            time?.SetScale(1f);
            if (canvasGo != null) canvasGo.SetActive(false);
        }

        private void OnBackPressed()
        {
            if (open) Close();
        }

        private void OnConfirmPressed()
        {
            matchSetup.SetArchetype(selectedIndex);
            Close();
        }

        private void Update()
        {
            if (!open) return;

            var navigate = input.Navigate;
            navigationTimer -= Time.unscaledDeltaTime;

            if (navigationTimer <= 0f)
            {
                if (navigate.y > 0.5f && selectedIndex > 0)
                {
                    selectedIndex--;
                    navigationTimer = navigationRepeatDelay;
                    Refresh();
                }
                else if (navigate.y < -0.5f && selectedIndex < matchSetup.ArchetypeCount - 1)
                {
                    selectedIndex++;
                    navigationTimer = navigationRepeatDelay;
                    Refresh();
                }
                else if (Mathf.Abs(navigate.y) <= 0.5f)
                {
                    navigationTimer = 0f;
                }
            }
        }

        private void Refresh()
        {
            for (var i = 0; i < optionLabels.Length; i++)
            {
                var isSelected = i == selectedIndex;
                optionLabels[i].text = (isSelected ? "> " : "  ") + matchSetup.GetArchetypeName(i);
                if (optionHighlights != null && i < optionHighlights.Length)
                    optionHighlights[i].color = isSelected
                        ? new Color(0.2f, 0.5f, 0.8f, 0.3f)
                        : Color.clear;
            }

            var archetype = matchSetup.GetArchetype(selectedIndex);
            if (archetype != null)
            {
                var p = archetype.personality;
                styleLabel.text = $"Style: {p.style}";
                weaponLabel.text = $"Engage: {archetype.MinEngageDistance:F1}m – {archetype.MaxEngageDistance:F1}m";
                speedLabel.text = $"Speed: {archetype.MovementSpeedMultiplier * 100:F0}%  |  Cooldown: {archetype.AttackCooldown:F2}s";
                if (colorPreview != null) colorPreview.color = archetype.AccentColor;
            }
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("CharSelectCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.GetComponent<RectTransform>();

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            var backdropRect = backdropGo.GetComponent<RectTransform>();
            backdropRect.SetParent(root, false);
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

            CreateText(root, "Title", "CHOOSE YOUR RIVAL", 38, new Vector2(0f, 280f));

            // Left side: roster list
            optionLabels = new Text[matchSetup.ArchetypeCount];
            optionHighlights = new Image[matchSetup.ArchetypeCount];
            for (var i = 0; i < optionLabels.Length; i++)
            {
                var entry = CreateListEntry(root, i, new Vector2(-250f, 160f - i * 44f));
                optionLabels[i] = entry.GetComponentInChildren<Text>();
                optionHighlights[i] = entry.GetComponent<Image>();
            }

            // Right side: info panel
            var infoPanel = CreatePanel(root, "InfoPanel", new Vector2(180f, 60f), new Vector2(350f, 280f));
            CreateText(infoPanel, "InfoTitle", "ARCHETYPE INFO", 20, new Vector2(0f, 110f)).color = new Color(0.8f, 0.8f, 0.8f);
            styleLabel = CreateText(infoPanel, "Style", "Style: Balanced", 16, new Vector2(0f, 75f));
            weaponLabel = CreateText(infoPanel, "Weapon", "", 14, new Vector2(0f, 48f));
            weaponLabel.color = new Color(0.6f, 0.8f, 1f);
            speedLabel = CreateText(infoPanel, "Speed", "", 14, new Vector2(0f, 24f));
            speedLabel.color = new Color(0.6f, 0.8f, 1f);

            // Color preview
            var colorGo = new GameObject("ColorPreview", typeof(RectTransform), typeof(Image));
            var colorRect = colorGo.GetComponent<RectTransform>();
            colorRect.SetParent(infoPanel.GetComponent<RectTransform>(), false);
            colorRect.anchorMin = new Vector2(0.5f, 0.5f);
            colorRect.anchorMax = new Vector2(0.5f, 0.5f);
            colorRect.pivot = new Vector2(0.5f, 0.5f);
            colorRect.anchoredPosition = new Vector2(0f, -20f);
            colorRect.sizeDelta = new Vector2(60f, 60f);
            colorPreview = colorGo.GetComponent<Image>();
            colorPreview.color = Color.white;

            CreateText(root, "Hint", "WASD / Arrows to browse — Enter / Space to confirm", 16, new Vector2(0f, -300f));
        }

        private GameObject CreateListEntry(RectTransform parent, int index, Vector2 position)
        {
            var go = new GameObject($"Entry_{index}", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400f, 38f);
            go.GetComponent<Image>().color = Color.clear;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = matchSetup.GetArchetypeName(index);

            return go;
        }

        private static GameObject CreatePanel(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.8f);
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
            rect.sizeDelta = new Vector2(600f, 30f);

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
