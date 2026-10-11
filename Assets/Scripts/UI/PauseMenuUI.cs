using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Input;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Runtime-built pause overlay. Freezes time via ITimeService, shows RESUME
    /// and MAIN MENU options. Activates when GameManager enters Paused state.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        private IMenuInput menuInput;
        private ITimeService timeService;
        private ISceneLoader sceneLoader;
        private GameManager gameManager;

        private GameObject canvasGo;
        private Text resumeLabel;
        private Text settingsLabel;
        private Text loadoutLabel;
        private Text menuLabel;
        private Text quitLabel;
        private SettingsMenuUI settingsMenu;
        private LoadoutSelectMenu loadoutMenu;
        private int selection;
        private bool active;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out menuInput)) return;
            if (!ServiceLocator.TryGet(out timeService)) return;
            if (!ServiceLocator.TryGet(out sceneLoader)) return;
            gameManager = GameManager.Instance;

            BuildUi();
            settingsMenu = gameObject.GetComponent<SettingsMenuUI>();
            if (settingsMenu == null) settingsMenu = gameObject.AddComponent<SettingsMenuUI>();
            loadoutMenu = gameObject.GetComponent<LoadoutSelectMenu>();
            if (loadoutMenu == null) loadoutMenu = gameObject.AddComponent<LoadoutSelectMenu>();
            canvasGo.SetActive(false);

            if (gameManager != null) gameManager.GameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (gameManager != null) gameManager.GameStateChanged -= OnGameStateChanged;
            if (active) Close();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Paused) Open();
            else if (active) Close();
        }

        /// <summary>Re-opens this pause overlay after a sub-menu (settings/loadout) closes.</summary>
        public void OnReturnToPause()
        {
            if (!active && gameManager != null && gameManager.CurrentState == GameState.Paused)
            {
                menuInput.SetMenuActive(true);
                Open();
            }
        }

        private void Open()
        {
            active = true;
            selection = 0;
            menuInput.SetMenuActive(true);
            canvasGo.SetActive(true);
            menuInput.ConfirmPressed += OnConfirmPressed;
            menuInput.BackPressed += OnBackPressed;
            Refresh();
        }

        private void Close()
        {
            if (!active) return;
            active = false;
            menuInput.ConfirmPressed -= OnConfirmPressed;
            menuInput.BackPressed -= OnBackPressed;
            menuInput.SetMenuActive(false);
            canvasGo.SetActive(false);
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
        }

        private void OnConfirmPressed()
        {
            if (!active) return;

            if (selection == 0)
            {
                Close();
                if (gameManager != null) gameManager.TogglePause();
            }
            else if (selection == 1)
            {
                Close();
                settingsMenu.Open();
            }
            else if (selection == 2)
            {
                Close();
                var opened = loadoutMenu.Open(() => { menuInput.SetMenuActive(true); Open(); });
                if (!opened)
                {
                    menuInput.SetMenuActive(true);
                    Open();
                }
            }
            else if (selection == 3)
            {
                Close();
                if (gameManager != null && gameManager.CurrentState == GameState.Paused)
                    gameManager.SetState(GameState.Playing);
                sceneLoader.LoadAsync(SceneRegistry.MainMenu);
            }
            else
            {
                Close();
                QuitGame();
            }
        }

        private void OnBackPressed()
        {
            if (!active) return;
            if (selection == 4)
            {
                QuitGame();
                return;
            }
            Close();
            if (gameManager != null) gameManager.TogglePause();
        }

        private static void QuitGame() => GameQuit.Quit();

        private void Refresh()
        {
            resumeLabel.text = selection == 0 ? "> RESUME" : "  RESUME";
            settingsLabel.text = selection == 1 ? "> SETTINGS" : "  SETTINGS";
            loadoutLabel.text = selection == 2 ? "> LOADOUT" : "  LOADOUT";
            menuLabel.text = selection == 3 ? "> MAIN MENU" : "  MAIN MENU";
            quitLabel.text = selection == 4 ? "> QUIT" : "  QUIT";
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            CreateText(root, "Title", "PAUSED", 48, new Vector2(0f, 100f));
            resumeLabel = CreateText(root, "Resume", "  RESUME", 28, new Vector2(0f, 20f));
            settingsLabel = CreateText(root, "Settings", "  SETTINGS", 28, new Vector2(0f, -20f));
            loadoutLabel = CreateText(root, "Loadout", "  LOADOUT", 28, new Vector2(0f, -60f));
            menuLabel = CreateText(root, "Menu", "  MAIN MENU", 28, new Vector2(0f, -100f));
            quitLabel = CreateText(root, "Quit", "  QUIT", 28, new Vector2(0f, -140f));
            CreateText(root, "Hint", "WASD / Arrows to select — Enter to confirm — Esc to resume", 16, new Vector2(0f, -220f));
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
