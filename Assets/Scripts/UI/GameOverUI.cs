using UnityEngine;
using UnityEngine.UI;
using Rival.Core;
using Rival.Input;
using Rival.Enemy;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Runtime-built game-over overlay. Shows VICTORY or DEFEAT, match score
    /// (player wins / rival wins), and lifetime stats. Offers REMATCH and
    /// MAIN MENU. Activates when GameManager enters GameOver state.
    /// Uses unscaled time so it works while time is frozen.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Tooltip("Delay before the overlay becomes interactive.")]
        [SerializeField, Range(0.1f, 3f)] private float displayDelay = 1.0f;

        private IMenuInput menuInput;
        private ISceneLoader sceneLoader;
        private IStatsTracker statsTracker;
        private PlayerLoadoutManager loadout;
        private GameManager gameManager;
        private MatchController matchController;
        private EndlessModeManager endlessManager;
        private RivalGameMode gameMode = RivalGameMode.Match;

        private GameObject canvasGo;
        private Text resultLabel;
        private Text scoreLabel;
        private Text statsLabel;
        private Text rewardLabel;
        private Text rematchLabel;
        private Text menuLabel;
        private int selection;
        private bool active;
        private float delayTimer;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out menuInput)) return;
            if (!ServiceLocator.TryGet(out sceneLoader)) return;
            ServiceLocator.TryGet(out statsTracker);
            ServiceLocator.TryGet(out loadout);
            gameManager = GameManager.Instance;
            matchController = FindObjectOfType<MatchController>();
            endlessManager = FindObjectOfType<EndlessModeManager>();
            if (ServiceLocator.TryGet<IMatchConfig>(out var matchConfig)) gameMode = matchConfig.GameMode;

            BuildUi();
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
            if (current == GameState.GameOver)
            {
                delayTimer = displayDelay;
                active = true;
                menuInput.SetMenuActive(true);
                canvasGo.SetActive(true);
                menuInput.ConfirmPressed += OnConfirmPressed;
                menuInput.BackPressed += OnBackPressed;
                Refresh();
            }
        }

        private void Update()
        {
            if (!active) return;

            if (delayTimer > 0f)
            {
                delayTimer -= Time.unscaledDeltaTime;
                return;
            }

            var navigate = menuInput.Navigate;
            if (navigate.y > 0.5f)
            {
                selection = Mathf.Max(0, selection - 1);
                Refresh();
            }
            else if (navigate.y < -0.5f)
            {
                selection = Mathf.Min(1, selection + 1);
                Refresh();
            }
        }

        private void OnConfirmPressed()
        {
            if (!active || delayTimer > 0f) return;
            Close();

            if (selection == 0)
            {
                if (gameManager != null) gameManager.SetState(GameState.Loading);
                sceneLoader.LoadAsync(SceneRegistry.Gameplay, onComplete: () =>
                {
                    if (gameManager != null) gameManager.SetState(GameState.Playing);
                });
            }
            else
            {
                GoToMainMenu();
            }
        }

        private void OnBackPressed()
        {
            if (!active || delayTimer > 0f) return;
            GoToMainMenu();
        }

        private void GoToMainMenu()
        {
            Close();
            if (gameManager != null) gameManager.SetState(GameState.MainMenu);
            sceneLoader.LoadAsync(SceneRegistry.MainMenu);
        }

        private void Close()
        {
            active = false;
            menuInput.ConfirmPressed -= OnConfirmPressed;
            menuInput.BackPressed -= OnBackPressed;
            menuInput.SetMenuActive(false);
            canvasGo.SetActive(false);
        }

        private void Refresh()
        {
            if (gameMode == RivalGameMode.Endless)
            {
                RefreshEndless();
                return;
            }

            bool playerWon = matchController != null && matchController.PlayerWonMatch;
            resultLabel.text = playerWon ? "VICTORY" : "DEFEAT";
            resultLabel.color = playerWon ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);

            if (matchController != null)
            {
                scoreLabel.text = $"Player {matchController.PlayerWins} — {matchController.RivalWins} Rival";
            }

            if (statsTracker != null)
            {
                statsLabel.text = $"Lifetime: {statsTracker.Wins}W / {statsTracker.Losses}L  |  Streak: {statsTracker.WinStreak}  |  Best: {statsTracker.BestWinStreak}";
            }

            if (loadout != null)
            {
                var hint = "All weapons unlocked!";
                if (loadout.TryGetCheapestLocked(out var nextId, out var nextCost))
                    hint = $"Next unlock: {nextId} — {nextCost} tokens";
                rewardLabel.text = $"+{loadout.SessionEarnings} tokens  |  Balance: {loadout.Currency}  |  {hint}";
            }

            rematchLabel.text = selection == 0 ? "> REMATCH" : "  REMATCH";
            menuLabel.text = selection == 1 ? "> MAIN MENU" : "  MAIN MENU";
        }

        private void RefreshEndless()
        {
            resultLabel.text = "THE WAVES WIN";
            resultLabel.color = new Color(0.9f, 0.2f, 0.2f);

            var wave = endlessManager != null ? endlessManager.CurrentWave : 0;
            var best = endlessManager != null ? endlessManager.BestWave : 0;
            scoreLabel.text = $"Wave {wave} reached  |  Best: {best}";

            if (statsTracker != null)
            {
                statsLabel.text = $"Lifetime: {statsTracker.Wins}W / {statsTracker.Losses}L  |  Streak: {statsTracker.WinStreak}  |  Best: {statsTracker.BestWinStreak}";
            }

            if (loadout != null)
            {
                var hint = "All weapons unlocked!";
                if (loadout.TryGetCheapestLocked(out var nextId, out var nextCost))
                    hint = $"Next unlock: {nextId} — {nextCost} tokens";
                rewardLabel.text = $"+{loadout.SessionEarnings} tokens  |  Balance: {loadout.Currency}  |  {hint}";
            }

            rematchLabel.text = selection == 0 ? "> REMATCH (ENDLESS)" : "  REMATCH (ENDLESS)";
            menuLabel.text = selection == 1 ? "> MAIN MENU" : "  MAIN MENU";
        }

        private void BuildUi()
        {
            canvasGo = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler));
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

            resultLabel = CreateText(root, "Result", "VICTORY", 56, new Vector2(0f, 140f));
            scoreLabel = CreateText(root, "Score", "Player 0 — 0 Rival", 24, new Vector2(0f, 70f));
            statsLabel = CreateText(root, "Stats", "", 18, new Vector2(0f, 30f));
            rewardLabel = CreateText(root, "Rewards", "", 18, new Vector2(0f, -5f));
            rewardLabel.color = new Color(1f, 0.85f, 0.3f);
            rematchLabel = CreateText(root, "Rematch", "  REMATCH", 28, new Vector2(0f, -70f));
            menuLabel = CreateText(root, "Menu", "  MAIN MENU", 28, new Vector2(0f, -110f));
            CreateText(root, "Hint", "WASD / Arrows to select — Enter / Space to confirm", 16, new Vector2(0f, -250f));
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
