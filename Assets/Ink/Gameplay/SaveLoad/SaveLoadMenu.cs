using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Save/Load menu UI. Created dynamically, toggled via Tab key.
    /// </summary>
    public class SaveLoadMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        
        [Header("Style")]
        public Color backgroundColor = new Color(0.02f, 0.05f, 0.08f, 0.95f);
        public Color panelColor = new Color(0.05f, 0.1f, 0.12f, 0.98f);
        public Color borderColor = new Color(0.0f, 0.6f, 0.6f, 1f);
        public Color headerColor = new Color(0.0f, 0.8f, 0.8f, 1f);
        public Color textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        public Color buttonColor = new Color(0.1f, 0.3f, 0.3f, 1f);
        public Color buttonHoverColor = new Color(0.15f, 0.4f, 0.4f, 1f);
        public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
        private GameObject _canvas;
        private CanvasGroup _canvasGroup;
        private Text _statusText;
        private Text _errorText;
        private Button _saveButton;
        private Button _loadButton;
        private Font _font;
        
        private void Awake()
        {
            _font = Font.CreateDynamicFontFromOSFont("Courier New", 16);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            CreateUI();
            Hide();
        }
        
        private void CreateUI()
        {
            // Canvas with overlay dimming
            _canvas = new GameObject("SaveLoadCanvas");
            _canvas.transform.SetParent(transform);
            
            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Above other UI
            
            CanvasScaler scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            _canvas.AddComponent<GraphicRaycaster>();
            
            _canvasGroup = _canvas.AddComponent<CanvasGroup>();
            
            // Dim background (click to close)
            GameObject dimBg = new GameObject("DimBackground");
            dimBg.transform.SetParent(_canvas.transform, false);
            Image dimImage = dimBg.AddComponent<Image>();
            dimImage.color = new Color(0, 0, 0, 0.6f);
            dimImage.raycastTarget = true;
            
            RectTransform dimRect = dimBg.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            
            Button dimButton = dimBg.AddComponent<Button>();
            dimButton.onClick.AddListener(Hide);
            
            // Center panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_canvas.transform, false);
            
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = panelColor;
            
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(3, -3);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 280);
            
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Title
            CreateText(panel.transform, "Save / Load", 28, headerColor, TextAnchor.MiddleCenter, 40);
            
            // Separator
            CreateSeparator(panel.transform);
            
            // Status text (shows save info)
            _statusText = CreateText(panel.transform, "Checking save...", 18, textColor, TextAnchor.MiddleCenter, 30);
            
            // Error text (hidden by default)
            _errorText = CreateText(panel.transform, "", 16, new Color(1f, 0.4f, 0.4f, 1f), TextAnchor.MiddleCenter, 24);
            _errorText.gameObject.SetActive(false);
            
            // Button row
            GameObject buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(panel.transform, false);
            
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 16;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;
            
            LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
            buttonRowLayout.preferredHeight = 48;
            
            _saveButton = CreateButton(buttonRow.transform, "Save", OnSaveClicked);
            _loadButton = CreateButton(buttonRow.transform, "Load", OnLoadClicked);
            CreateButton(buttonRow.transform, "Close", Hide);
            
            // Hint text
            CreateText(panel.transform, "Press Tab or Esc to close", 14, new Color(0.5f, 0.5f, 0.5f, 1f), TextAnchor.MiddleCenter, 20);
        }
        
        private Text CreateText(Transform parent, string content, int fontSize, Color color, TextAnchor alignment, float height)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            
            Text text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.color = color;
            text.text = content;
            text.alignment = alignment;
            
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            
            return text;
        }
        
        private void CreateSeparator(Transform parent)
        {
            GameObject sep = new GameObject("Separator");
            sep.transform.SetParent(parent, false);
            
            Image img = sep.AddComponent<Image>();
            img.color = borderColor;
            
            LayoutElement layout = sep.AddComponent<LayoutElement>();
            layout.preferredHeight = 2;
        }
        
        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(parent, false);
            
            Image bg = go.AddComponent<Image>();
            bg.color = buttonColor;
            
            Button btn = go.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonHoverColor;
            colors.disabledColor = disabledColor;
            btn.colors = colors;
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);
            
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1;
            layout.minHeight = 44;
            layout.preferredHeight = 44;
            
            // Button text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            
            Text text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 20;
            text.color = textColor;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return btn;
        }
        
        public void Show()
        {
            if (InventoryUI.IsOpen) return; // Don't open over inventory
            
            RefreshStatus();
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            IsOpen = true;
            
            Debug.Log("[SaveLoadMenu] Opened");
        }
        
        public void Hide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            IsOpen = false;
            
            HideError();
            Debug.Log("[SaveLoadMenu] Closed");
        }
        
        public void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }
        
        private void RefreshStatus()
        {
            if (SaveSystem.SaveExists())
            {
                string timestamp = SaveSystem.GetSaveTimestamp();
                _statusText.text = $"Saved: {timestamp}";
                _loadButton.interactable = true;
            }
            else
            {
                _statusText.text = "No save file";
                _loadButton.interactable = false;
            }
        }
        
        private void ShowError(string message)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }
        
        private void HideError()
        {
            _errorText.gameObject.SetActive(false);
        }
        
        private void OnSaveClicked()
        {
            HideError();
            
            if (GameStateManager.QuickSave())
            {
                RefreshStatus();
                Debug.Log("[SaveLoadMenu] Game saved");
            }
            else
            {
                ShowError("Save failed!");
            }
        }
        
        private void OnLoadClicked()
        {
            HideError();
            
            if (GameStateManager.QuickLoad())
            {
                Hide();
                Debug.Log("[SaveLoadMenu] Game loaded");
            }
            else
            {
                ShowError("Load failed!");
            }
        }
    }
}
