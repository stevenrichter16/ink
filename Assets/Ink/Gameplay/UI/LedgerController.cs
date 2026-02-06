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
        public bool IsLedgerVisible => IsVisible();

        [Tooltip("Sorting order for the Ledger canvas so it renders above other UI.")]
        public int sortingOrder = 1000;
        [Tooltip("Logs ledger input and tab switching events for debugging.")]
        public bool debugInputLogs = true;

        public enum LedgerTab { Factions, Economy }

        private LedgerPanel _factionPanel;
        private LedgerEconomyPanel _economyPanel;
        private LedgerTab _currentTab = LedgerTab.Factions;
        private bool _shiftWasDown;
        private bool _loggedMissingKeyboard;

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
            if (kb == null)
            {
                if (debugInputLogs && !_loggedMissingKeyboard)
                {
                    Debug.LogWarning("[LedgerController] Keyboard.current is null; input events are unavailable.");
                    _loggedMissingKeyboard = true;
                }
                return;
            }

            if (_loggedMissingKeyboard && debugInputLogs)
            {
                Debug.Log("[LedgerController] Keyboard.current restored.");
                _loggedMissingKeyboard = false;
            }

            bool visible = IsVisible();
            bool shiftDown = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            bool shiftPressed = shiftDown && !_shiftWasDown;
            _shiftWasDown = shiftDown;

            if (kb.bKey.wasPressedThisFrame)
            {
                if (debugInputLogs)
                    Debug.Log($"[LedgerController] B pressed. ready={IsReady()} visible={visible} currentTab={_currentTab}");
                Toggle();
            }
            else if (kb.lKey.wasPressedThisFrame && debugInputLogs)
            {
                Debug.Log($"[LedgerController] L pressed, but ledger toggle is bound to B. visible={visible} currentTab={_currentTab}");
            }
            else if (kb.escapeKey.wasPressedThisFrame && IsVisible())
            {
                if (debugInputLogs)
                    Debug.Log("[LedgerController] Escape pressed while ledger visible. Hiding ledger.");
                Hide();
            }
            else if (shiftPressed && IsVisible())
            {
                if (debugInputLogs)
                    Debug.Log($"[LedgerController] Shift edge detected. visible={visible} currentTab={_currentTab}. Switching tab.");
                SwitchTab();
            }
            else if (shiftPressed && debugInputLogs)
            {
                Debug.Log($"[LedgerController] Shift edge detected but ledger not visible. Ignoring tab switch. currentTab={_currentTab}");
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
            if (!IsReady())
            {
                if (debugInputLogs)
                    Debug.LogWarning("[LedgerController] Toggle requested but panels are not ready.");
                return;
            }

            if (IsVisible()) Hide();
            else Show();
        }

        private void Show()
        {
            if (debugInputLogs)
                Debug.Log($"[LedgerController] Show requested. tab={_currentTab}");
            ShowCurrentTab();
        }

        private void Hide()
        {
            if (debugInputLogs)
                Debug.Log($"[LedgerController] Hide requested. tab={_currentTab}");
            if (_factionPanel != null) _factionPanel.Hide();
            if (_economyPanel != null) _economyPanel.Hide();
        }

        private void SwitchTab()
        {
            var previous = _currentTab;
            _currentTab = _currentTab == LedgerTab.Factions ? LedgerTab.Economy : LedgerTab.Factions;
            if (debugInputLogs)
                Debug.Log($"[LedgerController] SwitchTab: {previous} -> {_currentTab}");
            ShowCurrentTab();
        }

        private void ShowCurrentTab()
        {
            if (_factionPanel != null) _factionPanel.Hide();
            if (_economyPanel != null) _economyPanel.Hide();

            if (_currentTab == LedgerTab.Factions && _factionPanel != null)
            {
                _factionPanel.Show();
                if (debugInputLogs)
                    Debug.Log("[LedgerController] Showing Factions tab.");
            }
            else if (_currentTab == LedgerTab.Economy && _economyPanel != null)
            {
                _economyPanel.Show();
                if (debugInputLogs)
                    Debug.Log("[LedgerController] Showing Economy tab.");
            }
            else if (debugInputLogs)
            {
                Debug.LogWarning($"[LedgerController] Could not show tab={_currentTab}. factionPanelNull={_factionPanel == null} economyPanelNull={_economyPanel == null}");
            }
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
