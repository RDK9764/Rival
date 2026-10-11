using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Displays lifetime stats on the main menu. Shows wins, losses,
    /// win streak, best streak, fights played, and best endless wave.
    /// Runtime-built. Subscribes to no events — just reads IStatsTracker
    /// once on open.
    /// </summary>
    public class MainMenuStatsDisplay : MonoBehaviour
    {
        private IStatsTracker stats;
        private Text statsLabel;
        private GameObject canvasGo;
        private bool built;

        public void Show()
        {
            if (!built) BuildUi();
            ServiceLocator.TryGet(out stats);
            Refresh();
            canvasGo.SetActive(true);
        }

        public void Hide()
        {
            if (canvasGo != null) canvasGo.SetActive(false);
        }

        private void Refresh()
        {
            if (stats == null || statsLabel == null) return;
            var bestWave = PlayerPrefs.GetInt("Rival_Endless_BestWave", 0);
            statsLabel.text =
                $"Fights: {stats.FightsPlayed}\n" +
                $"Wins: {stats.Wins}  |  Losses: {stats.Losses}\n" +
                $"Win Rate: {(stats.FightsPlayed > 0 ? (float)stats.Wins / stats.FightsPlayed * 100f : 0f):F0}%\n" +
                $"Streak: {stats.WinStreak}  |  Best: {stats.BestWinStreak}\n" +
                $"Best Wave: {bestWave}";
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("StatsCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.GetComponent<RectTransform>();

            var panel = CreatePanel(root, "StatsPanel", new Vector2(-10f, 10f), new Vector2(320f, 180f));
            var panelRect = panel.GetComponent<RectTransform>();

            CreateText(panelRect, "Title", "LIFETIME STATS", 18, new Vector2(0f, 65f)).color = new Color(0.8f, 0.8f, 0.8f);
            statsLabel = CreateText(panelRect, "Stats", "", 15, new Vector2(0f, -10f));
            statsLabel.alignment = TextAnchor.UpperCenter;

            built = true;
        }

        private static GameObject CreatePanel(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
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
            rect.sizeDelta = new Vector2(300f, 140f);

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
