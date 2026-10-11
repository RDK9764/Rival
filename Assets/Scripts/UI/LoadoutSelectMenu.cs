using System;
using UnityEngine;
using UnityEngine.UI;
using Rival.Combat;
using Rival.Enemy;
using Rival.Input;
using Rival.Utilities;

namespace Rival.UI
{
    /// <summary>
    /// Runtime-built loadout menu. Lists every weapon in the WeaponCatalog,
    /// shows locked/unlocked state and unlock cost, and lets the player equip
    /// an unlocked weapon or spend currency to unlock a locked one. Reads and
    /// drives PlayerLoadoutManager through the ServiceLocator so the UI never
    /// touches the SaveSystem layer directly. Opened from the pause menu.
    /// </summary>
    public class LoadoutSelectMenu : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.6f)] private float navigationRepeatDelay = 0.2f;

        private IMenuInput menuInput;
        private PlayerLoadoutManager loadout;
        private Action onClosed;

        private GameObject canvasGo;
        private Text[] weaponLabels;
        private Text backLabel;
        private Text currencyLabel;
        private Text detailLabel;
        private int selection;
        private float navigationTimer;
        private bool active;

        private void Awake()
        {
            canvasGo = new GameObject("LoadoutCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.SetActive(false);
        }

        private void OnDestroy() => Close();

        public bool Open(Action onClosedCallback)
        {
            if (!ServiceLocator.TryGet(out menuInput)) return false;
            if (!ServiceLocator.TryGet(out loadout)) return false;

            onClosed = onClosedCallback;
            active = true;
            selection = 0;
            BuildWeaponList();
            menuInput.SetMenuActive(true);
            menuInput.ConfirmPressed += OnConfirmPressed;
            menuInput.BackPressed += OnBackPressed;
            canvasGo.SetActive(true);
            Refresh();
            return true;
        }

        public void Close()
        {
            if (!active) return;
            active = false;
            if (menuInput != null)
            {
                menuInput.ConfirmPressed -= OnConfirmPressed;
                menuInput.BackPressed -= OnBackPressed;
                menuInput.SetMenuActive(false);
            }
            if (canvasGo != null) canvasGo.SetActive(false);
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }

        private void OnBackPressed()
        {
            if (active) Close();
        }

        private void Update()
        {
            if (!active) return;

            var navigate = menuInput.Navigate;
            var total = TotalItems();
            navigationTimer -= Time.unscaledDeltaTime;

            if (navigationTimer <= 0f)
            {
                if (navigate.y > 0.5f && selection > 0)
                {
                    selection--;
                    navigationTimer = navigationRepeatDelay;
                    Refresh();
                }
                else if (navigate.y < -0.5f && selection < total - 1)
                {
                    selection++;
                    navigationTimer = navigationRepeatDelay;
                    Refresh();
                }
                else if (Mathf.Abs(navigate.y) <= 0.5f)
                {
                    navigationTimer = 0f;
                }
            }
        }

        private int TotalItems()
        {
            var weapons = loadout != null && loadout.AllCatalogWeapons() != null ? System.Linq.Enumerable.Count(loadout.AllCatalogWeapons()) : 0;
            return weapons + 1; // +1 for the BACK entry
        }

        private void OnConfirmPressed()
        {
            if (!active || loadout == null) return;

            var weapons = loadout.AllCatalogWeapons();
            var weaponCount = weapons != null ? System.Linq.Enumerable.Count(weapons) : 0;

            // The last item is BACK → return to the pause menu.
            if (selection >= weaponCount)
            {
                Close();
                return;
            }

            var i = 0;
            foreach (var entry in weapons)
            {
                if (i == selection)
                {
                    if (loadout.IsUnlocked(entry.id))
                        loadout.TryEquip(entry.id);
                    else
                        loadout.TryUnlock(entry.id);
                    Refresh();
                    return;
                }
                i++;
            }
        }

        private void BuildWeaponList()
        {
            var root = canvasGo.GetComponent<RectTransform>();

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            var backdropRect = backdropGo.GetComponent<RectTransform>();
            backdropRect.SetParent(root, false);
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            backdropGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

            // destroy previous content explicitly
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child != backdropGo.transform) Destroy(child.gameObject);
            }

            CreateText(root, "Title", "WEAPON LOADOUT", 42, new Vector2(0f, 280f));
            currencyLabel = CreateText(root, "Currency", "", 22, new Vector2(0f, 220f));
            currencyLabel.color = new Color(1f, 0.85f, 0.3f);

            var entries = loadout != null ? loadout.AllCatalogWeapons() : null;
            var count = entries != null ? System.Linq.Enumerable.Count(entries) : 0;
            weaponLabels = new Text[count];
            var idx = 0;
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    weaponLabels[idx] = CreateText(root, "Weapon" + idx, "", 26, new Vector2(0f, 150f - idx * 52f));
                    idx++;
                }
            }

            backLabel = CreateText(root, "Back", "Back", 26, new Vector2(0f, 150f - idx * 52f));

            detailLabel = CreateText(root, "Detail", "", 18, new Vector2(0f, 150f - idx * 52f - 40f));

            CreateText(root, "Hint", "Up/Down to choose — Enter to equip/unlock — Esc to return", 16, new Vector2(0f, -460f));
        }

        private void Refresh()
        {
            currencyLabel.text = $"CURRENCY: {loadout.Currency}";

            var weaponCount = loadout.AllCatalogWeapons() != null ? System.Linq.Enumerable.Count(loadout.AllCatalogWeapons()) : 0;
            var idx = 0;
            foreach (var entry in loadout.AllCatalogWeapons())
            {
                var isEquipped = string.Equals(loadout.EquippedWeaponId, entry.id, System.StringComparison.OrdinalIgnoreCase);
                var unlocked = loadout.IsUnlocked(entry.id);
                var prefix = idx == selection ? "> " : "  ";
                var state = isEquipped ? " [EQUIPPED]" : (unlocked ? " [READY]" : $" [LOCKED — {loadout.UnlockCostFor(entry.id)}]");
                weaponLabels[idx].text = prefix + entry.weapon.WeaponName + state;
                idx++;
            }

            if (backLabel != null)
                backLabel.text = selection >= weaponCount ? "> BACK" : "  BACK";

            if (selection >= weaponCount)
            {
                detailLabel.text = "Return to the pause menu.";
                return;
            }

            var selEntry = GetSelectedEntry();
            if (selEntry != null && selEntry.weapon != null)
            {
                var w = selEntry.weapon;
                detailLabel.text =
                    $"DMG {w.BaseDamage:F0}  |  RANGE {w.AttackRange:F1}m  |  CRIT {w.CritChance * 100:F0}%  |  " +
                    (loadout.IsUnlocked(selEntry.id) && string.Equals(loadout.EquippedWeaponId, selEntry.id, System.StringComparison.OrdinalIgnoreCase)
                        ? "Equipped"
                        : loadout.IsUnlocked(selEntry.id) ? "Press Enter to equip" : $"Press Enter to unlock for {loadout.UnlockCostFor(selEntry.id)}");
            }
        }

        private WeaponCatalogEntry GetSelectedEntry()
        {
            if (loadout == null) return null;
            var idx = 0;
            foreach (var entry in loadout.AllCatalogWeapons())
            {
                if (idx == selection) return entry;
                idx++;
            }
            return null;
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
            rect.sizeDelta = new Vector2(700f, 40f);

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
