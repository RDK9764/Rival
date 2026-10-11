using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Input;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Runtime-built main menu. Shows the game title, the roster list so the
    /// player can browse rival identities, and a FIGHT prompt to launch the
    /// match. Writes the selected archetype index into IMatchConfig before
    /// loading the Gameplay scene. Drives GameManager through the
    /// MainMenu → Loading → Playing flow.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Tooltip("The roster to display. Must be assigned since this scene has no RivalMatchSetup.")]
        [SerializeField] private RivalRoster roster;
        [Tooltip("Seconds to wait before navigation repeats while a direction is held.")]
        [SerializeField, Range(0.05f, 0.6f)] private float navigationRepeatDelay = 0.22f;

        private IMenuInput menuInput;
        private ISceneLoader sceneLoader;
        private IMatchConfig matchConfig;
        private ITimeService timeService;
        private GameManager gameManager;
        private MainMenuStatsDisplay statsDisplay;

        private int selectedIndex;
        private int modeIndex;
        private float navigationTimer;
        private bool active;
        private bool inputBound = true;
        private SettingsMenuUI settingsMenu;

        private GameObject canvasGo;
        private Text[] rosterLabels;
        private Text modeLabel;
        private Text settingsLabel;
        private Text quitLabel;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out menuInput)) return;
            if (!ServiceLocator.TryGet(out sceneLoader)) return;
            if (!ServiceLocator.TryGet(out matchConfig)) return;
            if (!ServiceLocator.TryGet(out timeService)) return;
            gameManager = GameManager.Instance;

            if (roster == null || roster.Archetypes.Count == 0) return;
            statsDisplay = GetComponentInChildren<MainMenuStatsDisplay>(true);
            settingsMenu = gameObject.GetComponent<SettingsMenuUI>();
            if (settingsMenu == null) settingsMenu = gameObject.AddComponent<SettingsMenuUI>();

            BuildUi();
            Open();
        }

        private void OnDestroy() => Close();

        private void Open()
        {
            active = true;
            inputBound = true;
            menuInput.SetMenuActive(true);
            timeService?.SetScale(0f);
            if (gameManager != null) gameManager.SetState(GameState.MainMenu);
            canvasGo.SetActive(true);
            menuInput.ConfirmPressed += OnConfirmPressed;
            menuInput.BackPressed += OnBackPressed;
            Refresh();
            statsDisplay?.Show();
        }

        private void Close()
        {
            if (!active) return;
            active = false;
            menuInput.ConfirmPressed -= OnConfirmPressed;
            menuInput.BackPressed -= OnBackPressed;
            menuInput.SetMenuActive(false);
            timeService?.SetScale(1f);
            if (canvasGo != null) canvasGo.SetActive(false);
            statsDisplay?.Hide();
        }

        private void SetInputBound(bool bound)
        {
            inputBound = bound;
            if (bound)
            {
                menuInput.ConfirmPressed += OnConfirmPressed;
                menuInput.BackPressed += OnBackPressed;
                menuInput.SetMenuActive(true);
            }
            else
            {
                menuInput.ConfirmPressed -= OnConfirmPressed;
                menuInput.BackPressed -= OnBackPressed;
            }
        }

        private void OnConfirmPressed()
        {
            if (!active || !inputBound) return;

            // Roster rows launch the chosen game mode.
            if (selectedIndex < roster.Archetypes.Count)
            {
                matchConfig.SelectedIndex = selectedIndex;
                matchConfig.GameMode = modeIndex == 0 ? RivalGameMode.Match : RivalGameMode.Endless;
                Close();
                if (gameManager != null) gameManager.SetState(GameState.Loading);
                sceneLoader.LoadAsync(SceneRegistry.Gameplay, onComplete: () =>
                {
                    if (gameManager != null) gameManager.SetState(GameState.Playing);
                });
                return;
            }

            // Last two rows: Settings and Quit.
            if (selectedIndex == roster.Archetypes.Count)
            {
                SetInputBound(false);
                settingsMenu.Open(() => SetInputBound(true));
            }
            else
            {
                QuitGame();
            }
        }

        private void OnBackPressed()
        {
            if (!active || !inputBound) return;
            QuitGame();
        }

        private static void QuitGame() => GameQuit.Quit();

        private void Update()
        {
            if (!active || !inputBound) return;

            var maxRow = roster.Archetypes.Count + 1; // + SETTINGS + QUIT
            var navigate = menuInput.Navigate;
            navigationTimer -= Time.unscaledDeltaTime;

            if (navigationTimer <= 0f)
            {
                if (navigate.y > 0.5f && selectedIndex > 0)
                {
                    selectedIndex--;
                    navigationTimer = navigationRepeatDelay;
                    Refresh();
                }
                else if (navigate.y < -0.5f && selectedIndex < maxRow)
                {
                    selectedIndex++;
                    navigationTimer = navigationRepeatDelay;
                    Refresh();
                }
                else if ((navigate.x > 0.5f || navigate.x < -0.5f) && selectedIndex < roster.Archetypes.Count)
                {
                    modeIndex = modeIndex == 0 ? 1 : 0;
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
            var onRoster = selectedIndex < roster.Archetypes.Count;
            for (var i = 0; i < rosterLabels.Length; i++)
            {
                var name = roster.Archetypes[i] != null ? roster.Archetypes[i].DisplayName : string.Empty;
                rosterLabels[i].text = (onRoster && i == selectedIndex ? "> " : "  ") + name;
            }

            if (modeLabel != null)
            {
                var label = "MODE: " + (modeIndex == 0 ? "MATCH" : "ENDLESS");
                if (onRoster) label += "  ◄ ►";
                modeLabel.text = label;
            }

            if (settingsLabel != null)
                settingsLabel.text = selectedIndex == roster.Archetypes.Count ? "> SETTINGS" : "  SETTINGS";
            if (quitLabel != null)
                quitLabel.text = selectedIndex == roster.Archetypes.Count + 1 ? "> QUIT" : "  QUIT";
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

            CreateText(root, "Title", "RIVAL", 64, new Vector2(0f, 300f));

            CreateText(root, "Subtitle", "CHOOSE YOUR RIVAL", 24, new Vector2(0f, 200f));

            rosterLabels = new Text[roster.Archetypes.Count];
            for (var i = 0; i < rosterLabels.Length; i++)
            {
                rosterLabels[i] = CreateText(root, "Rival" + i, string.Empty, 28, new Vector2(0f, 100f - i * 52f));
            }

            modeLabel = CreateText(root, "Mode", string.Empty, 24, new Vector2(0f, 100f - roster.Archetypes.Count * 52f - 40f));
            settingsLabel = CreateText(root, "Settings", "  SETTINGS", 22, new Vector2(0f, 100f - roster.Archetypes.Count * 52f - 96f));
            quitLabel = CreateText(root, "Quit", "  QUIT", 22, new Vector2(0f, 100f - roster.Archetypes.Count * 52f - 148f));

            CreateText(root, "Hint", "Vertical: choose rival / settings / quit   |   Horizontal: MATCH / ENDLESS   |   Enter: confirm   |   Esc: quit", 18, new Vector2(0f, -460f));
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
