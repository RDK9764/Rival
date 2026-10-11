using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Input;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Runtime-built settings menu. Accessible from the pause menu or main menu.
    /// Adjusts audio volume, camera sensitivity, quality, and fullscreen via
    /// IGameSettings. All values persist to PlayerPrefs.
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        private IMenuInput menuInput;
        private IGameSettings settings;
        private GameManager gameManager;

        private GameObject canvasGo;
        private Text musicLabel;
        private Text sfxLabel;
        private Text sensLabel;
        private Text qualityLabel;
        private Text fullscreenLabel;
        private int selection;
        private bool active;

        private float[] sensSteps = { 1f, 2f, 3f, 4f, 5f, 6f, 8f, 10f };
        private int sensIndex = 2;
        private System.Action onClosed;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out menuInput)) return;
            if (!ServiceLocator.TryGet(out settings)) return;
            gameManager = GameManager.Instance;

            BuildUi();
            canvasGo.SetActive(false);

            for (var i = 0; i < sensSteps.Length; i++)
            {
                if (Mathf.Approximately(sensSteps[i], settings.CameraSensitivity))
                {
                    sensIndex = i;
                    break;
                }
            }
        }

        private void OnDestroy() => Close();

        public void Open(System.Action onClose = null)
        {
            if (active) return;
            active = true;
            onClosed = onClose;
            selection = 0;
            menuInput?.SetMenuActive(true);
            canvasGo.SetActive(true);
            menuInput.ConfirmPressed += OnConfirmPressed;
            menuInput.BackPressed += OnBackPressed;
            Refresh();
        }

        public void Close()
        {
            if (!active) return;
            active = false;
            menuInput.ConfirmPressed -= OnConfirmPressed;
            menuInput.BackPressed -= OnBackPressed;
            menuInput?.SetMenuActive(false);
            canvasGo.SetActive(false);
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }

        private void OnBackPressed()
        {
            if (!active) return;
            Close();
            // Re-open the pause menu: it expects to be driven by the caller.
            // The pause menu re-opens itself by re-toggling its own open state.
            var pause = GetComponentInParent<PauseMenuUI>();
            if (pause != null) pause.OnReturnToPause();
        }

        private void Update()
        {
            if (!active) return;

            var navigate = menuInput.Navigate;
            if (navigate.y > 0.5f)
            {
                selection = Mathf.Max(0, selection - 1);
                Refresh();
            }
            else if (navigate.y < -0.5f)
            {
                selection = Mathf.Min(4, selection + 1);
                Refresh();
            }

            if (navigate.x > 0.5f) Adjust(1);
            else if (navigate.x < -0.5f) Adjust(-1);
        }

        private void Adjust(int dir)
        {
            switch (selection)
            {
                case 0:
                    settings.SetMusicVolume(settings.MusicVolume + dir * 0.1f);
                    break;
                case 1:
                    settings.SetSfxVolume(settings.SfxVolume + dir * 0.1f);
                    break;
                case 2:
                    sensIndex = Mathf.Clamp(sensIndex + dir, 0, sensSteps.Length - 1);
                    settings.SetCameraSensitivity(sensSteps[sensIndex]);
                    break;
                case 3:
                    settings.SetQualityLevel(settings.QualityLevel + dir);
                    break;
                case 4:
                    settings.SetFullscreen(!settings.Fullscreen);
                    break;
            }
            Refresh();
        }

        private void OnConfirmPressed()
        {
            if (!active) return;
            if (selection == 4)
            {
                settings.SetFullscreen(!settings.Fullscreen);
                Refresh();
            }
        }

        private void Refresh()
        {
            var arrow = (s, idx) => s == idx ? "> " : "  ";
            musicLabel.text = $"{arrow(selection, 0)}Music: {settings.MusicVolume:P0}";
            sfxLabel.text = $"{arrow(selection, 1)}SFX: {settings.SfxVolume:P0}";
            sensLabel.text = $"{arrow(selection, 2)}Sensitivity: {settings.CameraSensitivity:F1}";
            qualityLabel.text = $"{arrow(selection, 3)}Quality: {QualitySettings.names[settings.QualityLevel]}";
            fullscreenLabel.text = $"{arrow(selection, 4)}Fullscreen: {(settings.Fullscreen ? "On" : "Off")}";
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("SettingsCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            CreateText(root, "Title", "SETTINGS", 42, new Vector2(0f, 200f));
            musicLabel = CreateText(root, "Music", "", 24, new Vector2(0f, 100f));
            sfxLabel = CreateText(root, "Sfx", "", 24, new Vector2(0f, 50f));
            sensLabel = CreateText(root, "Sens", "", 24, new Vector2(0f, 0f));
            qualityLabel = CreateText(root, "Quality", "", 24, new Vector2(0f, -50f));
            fullscreenLabel = CreateText(root, "Fullscreen", "", 24, new Vector2(0f, -100f));
            CreateText(root, "Hint", "WASD/Arrows: navigate & adjust — Esc: back", 16, new Vector2(0f, -220f));
        }

        private static Text CreateText(RectTransform parent, string name, string content, int size, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(600f, 40f);

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
