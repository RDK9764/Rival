using System;
using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Input;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Runtime-built round announcement overlay. Shows "ROUND N", "FIGHT!",
    /// round result, and match result. Uses unscaled time so it works
    /// while time is frozen between rounds.
    /// </summary>
    public class RoundAnnouncementUI : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 2f)] private float announceDuration = 1.2f;
        [SerializeField, Range(0.3f, 1f)] private float fightDuration = 0.8f;

        private GameManager gameManager;
        private GameObject canvasGo;
        private Text announcementLabel;
        private float displayTimer;
        private bool displaying;
        private Action onDisplayed;

        private void Start()
        {
            gameManager = GameManager.Instance;
            BuildUi();
            canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (!displaying) return;

            displayTimer -= Time.unscaledDeltaTime;
            if (displayTimer <= 0f)
            {
                displaying = false;
                canvasGo.SetActive(false);
                onDisplayed?.Invoke();
                onDisplayed = null;
            }
        }

        public void ShowAnnouncement(string text, float duration, Action onComplete = null)
        {
            announcementLabel.text = text;
            announcementLabel.fontSize = 48;
            displayTimer = duration;
            displaying = true;
            onDisplayed = onComplete;
            canvasGo.SetActive(true);
        }

        public void ShowRoundStart(int roundNumber, Action onComplete = null)
        {
            ShowAnnouncement($"ROUND {roundNumber}", announceDuration, () =>
            {
                ShowAnnouncement("FIGHT!", fightDuration, onComplete);
            });
        }

        public void ShowRoundResult(bool playerWon, Action onComplete = null)
        {
            var text = playerWon ? "YOU WIN THE ROUND" : "RIVAL WINS THE ROUND";
            ShowAnnouncement(text, announceDuration, onComplete);
        }

        public void ShowMatchResult(bool playerWon, Action onComplete = null)
        {
            var text = playerWon ? "VICTORY" : "DEFEAT";
            ShowAnnouncement(text, announceDuration * 1.5f, onComplete);
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("RoundCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rect = labelGo.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 80f);

            announcementLabel = labelGo.GetComponent<Text>();
            announcementLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            announcementLabel.fontSize = 48;
            announcementLabel.alignment = TextAnchor.MiddleCenter;
            announcementLabel.color = Color.white;
        }
    }
}
