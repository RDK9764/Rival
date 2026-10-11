using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Enemy;

namespace Rival.UI
{
    /// <summary>
    /// Displays wave number, difficulty modifiers, and streak info during
    /// endless mode. Subscribes to EndlessModeManager events and shows
    /// timed overlays. Runtime-built, no prefab dependencies.
    /// </summary>
    public class WaveAnnouncementUI : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 3f)] private float waveDisplayDuration = 1.5f;

        private EndlessModeManager endlessManager;
        private GameManager gameManager;
        private GameObject canvasGo;
        private Text waveLabel;
        private Text statsLabel;
        private Text bestLabel;
        private float displayTimer;
        private bool displaying;

        private void Start()
        {
            gameManager = GameManager.Instance;
            endlessManager = FindObjectOfType<EndlessModeManager>();

            BuildUi();
            canvasGo.SetActive(false);

            if (endlessManager != null)
            {
                endlessManager.WaveStarted += OnWaveStarted;
                endlessManager.WaveEnded += OnWaveEnded;
                endlessManager.MatchComplete += OnMatchComplete;
            }
        }

        private void OnDestroy()
        {
            if (endlessManager != null)
            {
                endlessManager.WaveStarted -= OnWaveStarted;
                endlessManager.WaveEnded -= OnWaveEnded;
                endlessManager.MatchComplete -= OnMatchComplete;
            }
        }

        private void Update()
        {
            if (!displaying) return;
            displayTimer -= Time.unscaledDeltaTime;
            if (displayTimer <= 0f)
            {
                displaying = false;
                canvasGo.SetActive(false);
            }
        }

        private void OnWaveStarted(int wave)
        {
            waveLabel.text = $"WAVE {wave}";
            var dmgPercent = Mathf.RoundToInt((endlessManager.CurrentDamageScale - 1f) * 100f);
            var spdPercent = Mathf.RoundToInt((endlessManager.CurrentSpeedScale - 1f) * 100f);
            statsLabel.text = dmgPercent > 0 || spdPercent > 0
                ? $"Rival: +{dmgPercent}% DMG  +{spdPercent}% SPD"
                : "Rival: Normal";
            bestLabel.text = $"Best: Wave {endlessManager.BestWave}";
            canvasGo.SetActive(true);
            displayTimer = waveDisplayDuration;
            displaying = true;
        }

        private void OnWaveEnded(int wave, bool playerWon)
        {
            var best = endlessManager != null ? endlessManager.BestWave : 0;
            bestLabel.text = $"Best: Wave {best}";

            if (!playerWon)
            {
                // Player fell: show defeat briefly; the game-over screen follows.
                waveLabel.text = $"DEFEATED AT WAVE {wave}";
                statsLabel.text = $"You fell at wave {wave}.";
                displayTimer = 2.5f;
            }
            else
            {
                waveLabel.text = $"WAVE {wave} CLEARED";
                statsLabel.text = "Next wave approaching...";
                displayTimer = waveDisplayDuration + 0.5f;
            }

            canvasGo.SetActive(true);
            displaying = true;
        }

        private void OnMatchComplete(int finalWave)
        {
            waveLabel.text = $"DEFEATED AT WAVE {finalWave}";
            statsLabel.text = $"Best: Wave {endlessManager.BestWave}";
            bestLabel.text = "";
            canvasGo.SetActive(true);
            displayTimer = 2.5f;
            displaying = true;
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("WaveCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            waveLabel = CreateText(root, "Wave", "WAVE 1", 48, new Vector2(0f, 60f));
            statsLabel = CreateText(root, "Stats", "", 22, new Vector2(0f, 0f));
            bestLabel = CreateText(root, "Best", "", 18, new Vector2(0f, -40f));
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
