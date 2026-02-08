using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Right-side HUD panel showing active quests with progress.
    /// Auto-subscribes to QuestLog events. Fades completed quests out.
    /// </summary>
    public class QuestTrackerUI : MonoBehaviour
    {
        [Header("Style")]
        public Color panelBackground = new Color(0.02f, 0.05f, 0.08f, 0.85f);
        public Color headerColor = new Color(0.0f, 0.8f, 0.8f, 1f);
        public Color titleColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        public Color progressColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public Color completedColor = new Color(0.4f, 0.9f, 0.4f, 1f);
        public Color barBackground = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        public Color barFill = new Color(0.0f, 0.7f, 0.7f, 1f);
        public Color barCompleteFill = new Color(0.3f, 0.9f, 0.4f, 1f);

        private const float PanelWidth = 260f;
        private const float EntryHeight = 50f;
        private const float HeaderHeight = 24f;
        private const float Padding = 8f;
        private const float RightMargin = 12f;
        private const float TopMargin = 12f;

        private QuestLog _questLog;
        private GameObject _canvas;
        private GameObject _panel;
        private Text _headerText;
        private readonly List<QuestEntry> _entries = new List<QuestEntry>();
        private Font _font;

        private struct QuestEntry
        {
            public GameObject root;
            public Text titleText;
            public Text progressText;
            public Image barFillImage;
            public string questId;
        }

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                _questLog = player.GetComponent<QuestLog>();

            if (_questLog == null)
            {
                Debug.LogWarning("[QuestTrackerUI] No QuestLog found.");
                enabled = false;
                return;
            }

            _font = Font.CreateDynamicFontFromOSFont("Courier New", 14);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateUI();

            _questLog.OnQuestAdded += OnQuestChanged;
            _questLog.OnQuestUpdated += OnQuestChanged;

            // Build initial entries
            RebuildEntries();
        }

        private void OnDestroy()
        {
            if (_questLog != null)
            {
                _questLog.OnQuestAdded -= OnQuestChanged;
                _questLog.OnQuestUpdated -= OnQuestChanged;
            }
        }

        private void CreateUI()
        {
            _canvas = new GameObject("QuestTrackerCanvas");
            _canvas.transform.SetParent(transform);

            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 85; // Below most HUD elements

            CanvasScaler scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel (top-right)
            _panel = new GameObject("QuestPanel");
            _panel.transform.SetParent(_canvas.transform, false);

            Image panelBg = _panel.AddComponent<Image>();
            panelBg.color = panelBackground;
            panelBg.raycastTarget = false;

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-RightMargin, -TopMargin);
            panelRect.sizeDelta = new Vector2(PanelWidth, HeaderHeight + Padding);

            // Header
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(_panel.transform, false);

            _headerText = headerGO.AddComponent<Text>();
            _headerText.font = _font;
            _headerText.fontSize = 13;
            _headerText.fontStyle = FontStyle.Bold;
            _headerText.color = headerColor;
            _headerText.text = "QUESTS";
            _headerText.alignment = TextAnchor.MiddleLeft;
            _headerText.raycastTarget = false;

            RectTransform headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -2);
            headerRect.sizeDelta = new Vector2(-Padding * 2, HeaderHeight);
            headerRect.offsetMin = new Vector2(Padding, headerRect.offsetMin.y);
            headerRect.offsetMax = new Vector2(-Padding, headerRect.offsetMax.y);
        }

        private void OnQuestChanged(QuestLog.QuestEntry entry)
        {
            RebuildEntries();
        }

        private void RebuildEntries()
        {
            // Clear old entries
            foreach (var entry in _entries)
            {
                if (entry.root != null)
                    Destroy(entry.root);
            }
            _entries.Clear();

            if (_questLog == null) return;

            int visibleCount = 0;
            foreach (var logEntry in _questLog.Entries)
            {
                // Show active and recently completed, skip turned-in
                if (logEntry.state == QuestState.TurnedIn) continue;

                CreateEntry(logEntry, visibleCount);
                visibleCount++;
            }

            // Resize panel to fit
            float panelHeight = HeaderHeight + Padding + visibleCount * EntryHeight;
            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(PanelWidth, panelHeight);

            // Hide panel if no quests
            _panel.SetActive(visibleCount > 0);
        }

        private void CreateEntry(QuestLog.QuestEntry logEntry, int index)
        {
            var def = logEntry.definition;
            if (def == null) return;

            QuestEntry entry = new QuestEntry();
            entry.questId = logEntry.questId;

            // Root container
            entry.root = new GameObject($"Quest_{index}");
            entry.root.transform.SetParent(_panel.transform, false);

            RectTransform rootRect = entry.root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0, 1);
            rootRect.anchorMax = new Vector2(1, 1);
            rootRect.pivot = new Vector2(0.5f, 1);
            float yPos = -(HeaderHeight + Padding + index * EntryHeight);
            rootRect.anchoredPosition = new Vector2(0, yPos);
            rootRect.sizeDelta = new Vector2(0, EntryHeight);
            rootRect.offsetMin = new Vector2(Padding, rootRect.offsetMin.y);
            rootRect.offsetMax = new Vector2(-Padding, rootRect.offsetMax.y);

            bool isComplete = logEntry.state == QuestState.Completed;

            // Quest title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(entry.root.transform, false);
            entry.titleText = titleGO.AddComponent<Text>();
            entry.titleText.font = _font;
            entry.titleText.fontSize = 12;
            entry.titleText.color = isComplete ? completedColor : titleColor;
            entry.titleText.text = isComplete ? $"✓ {def.title}" : def.title;
            entry.titleText.alignment = TextAnchor.UpperLeft;
            entry.titleText.raycastTarget = false;

            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.5f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(0, 0);
            titleRect.offsetMax = Vector2.zero;

            // Progress text (e.g. "3/5 goblins")
            int required = Mathf.Max(1, def.requiredCount);
            int current = Mathf.Min(logEntry.currentCount, required);

            GameObject progGO = new GameObject("Progress");
            progGO.transform.SetParent(entry.root.transform, false);
            entry.progressText = progGO.AddComponent<Text>();
            entry.progressText.font = _font;
            entry.progressText.fontSize = 10;
            entry.progressText.color = isComplete ? completedColor : progressColor;
            entry.progressText.text = isComplete ? "Complete!" : $"{current}/{required} — {def.objectiveHint}";
            entry.progressText.alignment = TextAnchor.UpperLeft;
            entry.progressText.raycastTarget = false;

            RectTransform progRect = progGO.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0, 0.2f);
            progRect.anchorMax = new Vector2(1, 0.5f);
            progRect.offsetMin = Vector2.zero;
            progRect.offsetMax = Vector2.zero;

            // Progress bar background
            GameObject barBgGO = new GameObject("BarBg");
            barBgGO.transform.SetParent(entry.root.transform, false);
            Image barBgImg = barBgGO.AddComponent<Image>();
            barBgImg.color = barBackground;
            barBgImg.raycastTarget = false;

            RectTransform barBgRect = barBgGO.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0, 0);
            barBgRect.anchorMax = new Vector2(1, 0);
            barBgRect.pivot = new Vector2(0, 0);
            barBgRect.anchoredPosition = new Vector2(0, 2);
            barBgRect.sizeDelta = new Vector2(0, 6);

            // Progress bar fill
            GameObject barFillGO = new GameObject("BarFill");
            barFillGO.transform.SetParent(barBgGO.transform, false);
            entry.barFillImage = barFillGO.AddComponent<Image>();
            entry.barFillImage.color = isComplete ? barCompleteFill : barFill;
            entry.barFillImage.raycastTarget = false;

            float progress = required > 0 ? (float)current / required : 0f;
            RectTransform fillRect = barFillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            _entries.Add(entry);
        }
    }
}
