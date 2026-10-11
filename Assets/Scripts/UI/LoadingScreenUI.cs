using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Runtime-built loading screen with a progress bar. Shows when GameManager
    /// enters Loading state and hides on Playing. Subscribes to SceneLoader's
    /// progress callback for real-time bar fill.
    /// </summary>
    public class LoadingScreenUI : MonoBehaviour
    {
        [SerializeField] private float minDisplayTime = 0.5f;

        private GameManager gameManager;
        private ISceneLoader sceneLoader;

        private GameObject canvasGo;
        private Image fillBar;
        private Text tipLabel;
        private bool active;
        private float displayTimer;

        private static readonly string[] Tips =
        {
            "Block at the right moment to parry and stun your opponent.",
            "Sprinting drains stamina — manage it carefully.",
            "Heavy attacks break guards but have longer windups.",
            "Dodge gives invincibility frames — use them to escape combos.",
            "Each rival archetype remembers your fighting style."
        };

        private void Start()
        {
            gameManager = GameManager.Instance;
            ServiceLocator.TryGet(out sceneLoader);

            BuildUi();
            canvasGo.SetActive(false);

            if (gameManager != null) gameManager.GameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (gameManager != null) gameManager.GameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Loading) Show();
            else if (active) Hide();
        }

        private void Show()
        {
            active = true;
            displayTimer = minDisplayTime;
            canvasGo.SetActive(true);
            fillBar.fillAmount = 0f;
            tipLabel.text = Tips[Random.Range(0, Tips.Length)];
        }

        private void Hide()
        {
            active = false;
            canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (!active) return;
            displayTimer -= Time.unscaledDeltaTime;
        }

        public void SetProgress(float progress)
        {
            if (fillBar != null) fillBar.fillAmount = Mathf.Clamp01(progress);
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.95f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.SetParent(root, false);
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 60f);
            titleRect.sizeDelta = new Vector2(400f, 40f);
            var titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 32;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "LOADING...";

            var barBgGo = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            var barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.SetParent(root, false);
            barBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRect.pivot = new Vector2(0.5f, 0.5f);
            barBgRect.anchoredPosition = new Vector2(0f, -10f);
            barBgRect.sizeDelta = new Vector2(400f, 16f);
            barBgGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var barFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var barFillRect = barFillGo.GetComponent<RectTransform>();
            barFillRect.SetParent(barBgRect, false);
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            fillBar = barFillGo.GetComponent<Image>();
            fillBar.color = new Color(0.3f, 0.7f, 1f);

            var tipGo = new GameObject("Tip", typeof(RectTransform), typeof(Text));
            var tipRect = tipGo.GetComponent<RectTransform>();
            tipRect.SetParent(root, false);
            tipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tipRect.pivot = new Vector2(0.5f, 0.5f);
            tipRect.anchoredPosition = new Vector2(0f, -80f);
            tipRect.sizeDelta = new Vector2(600f, 40f);
            tipLabel = tipGo.GetComponent<Text>();
            tipLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tipLabel.fontSize = 16;
            tipLabel.alignment = TextAnchor.MiddleCenter;
            tipLabel.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }
}
