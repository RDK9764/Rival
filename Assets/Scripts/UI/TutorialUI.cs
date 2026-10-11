using UnityEngine;
using UnityEngine.UI;

namespace Rival.UI
{
    /// <summary>
    /// Minimal tutorial prompt overlay. Displays instruction text at the
    /// bottom-center of the screen. Runtime-built, no prefab dependencies.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        private GameObject canvasGo;
        private Text instructionLabel;
        private Text detailLabel;
        private bool built;

        public void Show(string instruction, string detail)
        {
            if (!built) BuildUi();
            instructionLabel.text = instruction;
            detailLabel.text = detail;
            canvasGo.SetActive(true);
        }

        public void Hide()
        {
            if (canvasGo != null) canvasGo.SetActive(false);
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGo.GetComponent<RectTransform>();

            // Bottom-center panel
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.SetParent(root, false);
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            panelRect.sizeDelta = new Vector2(700f, 80f);
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            var panelChild = panelGo.GetComponent<RectTransform>();

            instructionLabel = CreateText(panelChild, "Instruction", "", 22, new Vector2(0f, 12f));
            detailLabel = CreateText(panelChild, "Detail", "", 16, new Vector2(0f, -16f));
            detailLabel.color = new Color(0.7f, 0.7f, 0.7f);

            built = true;
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
            rect.sizeDelta = new Vector2(660f, 30f);

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
