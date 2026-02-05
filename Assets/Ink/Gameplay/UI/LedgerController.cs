using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Listens for the toggle key (B) and shows the Ledger UI.
    /// Runtime-creates its own canvas/panel so no prefab setup is required.
    /// </summary>
    public class LedgerController : MonoBehaviour
    {
        public static LedgerController Instance { get; private set; }

        [Tooltip("Sorting order for the Ledger canvas so it renders above other UI.")]
        public int sortingOrder = 1000;

        public enum LedgerTab { Factions, Economy }

        private LedgerPanel _factionPanel;
        private LedgerEconomyPanel _economyPanel;
        private LedgerTab _currentTab = LedgerTab.Factions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("LedgerController");
                go.AddComponent<LedgerController>();
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.bKey.wasPressedThisFrame)
            {
                Toggle();
            }
            else if (kb.escapeKey.wasPressedThisFrame && IsVisible())
            {
                Hide();
            }
            else if ((kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) && IsVisible())
            {
                SwitchTab();
            }
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("LedgerCanvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            _factionPanel = canvasGO.AddComponent<LedgerPanel>();
            _factionPanel.Initialize(Hide);
            _factionPanel.Hide();

            _economyPanel = canvasGO.AddComponent<LedgerEconomyPanel>();
            _economyPanel.Initialize(Hide);
            _economyPanel.Hide();
        }

        private void Toggle()
        {
            if (!IsReady()) return;
            if (IsVisible()) Hide();
            else Show();
        }

        private void Show()
        {
            ShowCurrentTab();
        }

        private void Hide()
        {
            if (_factionPanel != null) _factionPanel.Hide();
            if (_economyPanel != null) _economyPanel.Hide();
        }

        private void SwitchTab()
        {
            _currentTab = _currentTab == LedgerTab.Factions ? LedgerTab.Economy : LedgerTab.Factions;
            ShowCurrentTab();
        }

        private void ShowCurrentTab()
        {
            if (_factionPanel != null) _factionPanel.Hide();
            if (_economyPanel != null) _economyPanel.Hide();

            if (_currentTab == LedgerTab.Factions && _factionPanel != null)
                _factionPanel.Show();
            else if (_currentTab == LedgerTab.Economy && _economyPanel != null)
                _economyPanel.Show();
        }

        private bool IsReady()
        {
            return _factionPanel != null || _economyPanel != null;
        }

        private bool IsVisible()
        {
            return (_factionPanel != null && _factionPanel.IsVisible) || (_economyPanel != null && _economyPanel.IsVisible);
        }
    }
}
