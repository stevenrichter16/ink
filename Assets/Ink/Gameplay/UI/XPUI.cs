using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Minimal XP/Level UI display.
    /// Shows level number and XP progress bar.
    /// </summary>
    public class XPUI : MonoBehaviour
    {
        [Header("Target")]
        public Levelable target;
        
        [Header("Style")]
        public Color barBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        public Color barFillColor = new Color(0.2f, 0.6f, 1f, 1f);
        public Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        private GameObject _canvas;
        private Text _levelText;
        private Image _barFill;
        private Font _font;
        
        private void Start()
        {
            // Auto-find player's Levelable if not assigned
            if (target == null)
            {
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                    target = player.GetComponent<Levelable>();
            }
            
            if (target == null)
            {
                Debug.LogWarning("[XPUI] No Levelable target found!");
                enabled = false;
                return;
            }
            
            _font = Font.CreateDynamicFontFromOSFont("Courier New", 14);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            CreateUI();
            
            // Subscribe to events
            target.OnXpChanged += OnXpChanged;
            target.OnLevelUp += OnLevelUp;
            
            // Initial update
            UpdateDisplay();
        }
        
        private void OnDestroy()
        {
            if (target != null)
            {
                target.OnXpChanged -= OnXpChanged;
                target.OnLevelUp -= OnLevelUp;
            }
        }
        
        private void CreateUI()
        {
            // Canvas
            _canvas = new GameObject("XPUICanvas");
            _canvas.transform.SetParent(transform);
            
            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            
            CanvasScaler scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Container (bottom-left corner)
            GameObject container = new GameObject("Container");
            container.transform.SetParent(_canvas.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = new Vector2(20, 20);
            containerRect.sizeDelta = new Vector2(200, 40);
            
            // Level text
            GameObject textGo = new GameObject("LevelText");
            textGo.transform.SetParent(container.transform, false);
            
            _levelText = textGo.AddComponent<Text>();
            _levelText.font = _font;
            _levelText.fontSize = 18;
            _levelText.color = textColor;
            _levelText.text = "Lv 1";
            _levelText.alignment = TextAnchor.MiddleLeft;
            
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.5f);
            textRect.anchorMax = new Vector2(0, 0.5f);
            textRect.pivot = new Vector2(0, 0.5f);
            textRect.anchoredPosition = new Vector2(0, 0);
            textRect.sizeDelta = new Vector2(60, 30);
            
            // XP bar background
            GameObject barBg = new GameObject("BarBackground");
            barBg.transform.SetParent(container.transform, false);
            
            Image bgImage = barBg.AddComponent<Image>();
            bgImage.color = barBackgroundColor;
            
            RectTransform barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0, 0.5f);
            barBgRect.anchorMax = new Vector2(0, 0.5f);
            barBgRect.pivot = new Vector2(0, 0.5f);
            barBgRect.anchoredPosition = new Vector2(55, 0);
            barBgRect.sizeDelta = new Vector2(140, 16);
            
            // XP bar fill
            GameObject barFillGo = new GameObject("BarFill");
            barFillGo.transform.SetParent(barBg.transform, false);
            
            _barFill = barFillGo.AddComponent<Image>();
            _barFill.color = barFillColor;
            
            RectTransform fillRect = barFillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.anchoredPosition = new Vector2(2, 0);
            fillRect.sizeDelta = new Vector2(0, -4); // Will be set by UpdateDisplay
        }
        
        private void OnXpChanged(int currentXp, int xpToNext)
        {
            UpdateDisplay();
        }
        
        private void OnLevelUp(int newLevel)
        {
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (target == null) return;
            
            _levelText.text = $"Lv {target.Level}";
            
            // Update bar fill width
            float progress = target.XpProgress;
            float maxWidth = 136f; // 140 - 4 padding
            
            RectTransform fillRect = _barFill.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(maxWidth * progress, -4);
        }
    }
}
