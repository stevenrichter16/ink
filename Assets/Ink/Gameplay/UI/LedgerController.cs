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

        private LedgerPanel _panel;

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
            else if (kb.escapeKey.wasPressedThisFrame && _panel != null && _panel.IsVisible)
            {
                Hide();
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

            _panel = canvasGO.AddComponent<LedgerPanel>();
            _panel.Initialize(Hide);
            _panel.Hide();
        }

        private void Toggle()
        {
            if (_panel == null) return;
            if (_panel.IsVisible) Hide();
            else Show();
        }

        private void Show()
        {
            _panel.Show();
        }

        private void Hide()
        {
            _panel.Hide();
        }
    }
}
