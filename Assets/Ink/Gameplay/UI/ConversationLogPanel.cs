using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace InkSim
{
    /// <summary>
    /// Expandable/collapsible conversation log panel anchored at bottom-right.
    /// Logs all NPC-to-NPC dialogue lines with faction-colored speaker names.
    /// Clicking a log entry highlights the speaker entity for 5 seconds.
    ///
    /// Public API: ConversationLogPanel.PushLine(speaker, speakerName, listener, listenerName, text, color)
    /// Called by ConversationManager whenever a dialogue line is delivered.
    /// </summary>
    public class ConversationLogPanel : MonoBehaviour
    {
        public static ConversationLogPanel Instance { get; private set; }

        private const int MaxEntries = 50;
        private const float PanelWidth = 560f;
        private const float CollapsedHeight = 48f;
        private const float ExpandedHeight = 520f;
        private const float EntryMinHeight = 44f;
        private const float HighlightDuration = 5f;

        // Data
        private struct ConversationLogEntry
        {
            public string speakerName;
            public string listenerName;
            public string text;
            public Color color;
            public GridEntity speaker;
            public GridEntity listener;
            public int turnNumber;
        }

        private readonly List<ConversationLogEntry> _entries = new List<ConversationLogEntry>();

        // UI references
        private GameObject _canvasGO;
        private RectTransform _panelRect;
        private GameObject _scrollGO;
        private ScrollRect _scrollRect;
        private Transform _contentTransform;
        private Text _toggleText;
        private Font _font;
        private bool _expanded;
        private int _unreadCount;
        private Text _badgeText;
        private GameObject _badgeGO;

        // Entry pool (List-based for index access)
        private readonly List<PooledEntry> _entryPool = new List<PooledEntry>();

        // Highlight tracking — prevents stacking coroutines on same entity
        private Coroutine _activeHighlight;
        private GridEntity _highlightedEntity;
        private Color _highlightOriginalColor;

        private class PooledEntry
        {
            public GameObject gameObject;
            public Button button;
            public Text text;
            public Image background;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildUI();
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Add a conversation line to the log.
        /// Called by ConversationManager.DeliverLine().
        /// </summary>
        public static void PushLine(GridEntity speaker, string speakerName,
                                    GridEntity listener, string listenerName,
                                    string text, Color color)
        {
            if (Instance == null) return;
            Instance.PushLineInternal(speaker, speakerName, listener, listenerName, text, color);
        }

        private void PushLineInternal(GridEntity speaker, string speakerName,
                                      GridEntity listener, string listenerName,
                                      string text, Color color)
        {
            int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;

            _entries.Add(new ConversationLogEntry
            {
                speakerName = speakerName,
                listenerName = listenerName,
                text = text,
                color = color,
                speaker = speaker,
                listener = listener,
                turnNumber = turn
            });

            // Prune oldest if over limit
            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            // Refresh if expanded, otherwise track unread
            if (_expanded)
            {
                RefreshEntryList();
            }
            else
            {
                _unreadCount++;
                UpdateBadge();
            }
        }

        // =====================================================================
        // UI CONSTRUCTION
        // =====================================================================

        private void BuildUI()
        {
            _font = GetFont();

            // Canvas (Screen-Space Overlay)
            _canvasGO = new GameObject("ConversationLogCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasGO.transform.SetParent(transform, false);

            var canvas = _canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            var scaler = _canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Ensure EventSystem exists
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            // Panel (anchored bottom-right)
            var panelGO = new GameObject("Panel", typeof(Image));
            panelGO.transform.SetParent(_canvasGO.transform, false);

            var panelImg = panelGO.GetComponent<Image>();
            panelImg.color = new Color(0.04f, 0.06f, 0.08f, 0.92f);

            _panelRect = panelGO.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(1f, 0f); // bottom-right
            _panelRect.anchorMax = new Vector2(1f, 0f);
            _panelRect.pivot = new Vector2(1f, 0f);
            _panelRect.anchoredPosition = new Vector2(-10f, 10f);
            _panelRect.sizeDelta = new Vector2(PanelWidth, CollapsedHeight);

            var panelLayout = panelGO.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(6, 6, 4, 4);
            panelLayout.spacing = 4;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childAlignment = TextAnchor.UpperLeft;

            // Toggle button
            var toggleGO = new GameObject("ToggleButton", typeof(Image), typeof(Button));
            toggleGO.transform.SetParent(panelGO.transform, false);

            var toggleImg = toggleGO.GetComponent<Image>();
            toggleImg.color = new Color(0.1f, 0.15f, 0.2f, 0.95f);

            var toggleBtn = toggleGO.GetComponent<Button>();
            toggleBtn.onClick.AddListener(Toggle);

            var toggleLE = toggleGO.AddComponent<LayoutElement>();
            toggleLE.preferredHeight = 40f;
            toggleLE.minHeight = 40f;

            // Toggle text
            var toggleTextGO = new GameObject("Text", typeof(Text));
            toggleTextGO.transform.SetParent(toggleGO.transform, false);

            _toggleText = toggleTextGO.GetComponent<Text>();
            _toggleText.font = _font;
            _toggleText.fontSize = 20;
            _toggleText.fontStyle = FontStyle.Bold;
            _toggleText.color = new Color(0.7f, 0.85f, 0.9f, 1f);
            _toggleText.alignment = TextAnchor.MiddleCenter;
            _toggleText.text = "Conversations \u25B6"; // ▶

            var toggleTextRT = toggleTextGO.GetComponent<RectTransform>();
            toggleTextRT.anchorMin = Vector2.zero;
            toggleTextRT.anchorMax = Vector2.one;
            toggleTextRT.offsetMin = Vector2.zero;
            toggleTextRT.offsetMax = Vector2.zero;

            // Unread badge (small circle with count, top-right of toggle button)
            _badgeGO = new GameObject("Badge", typeof(Image));
            _badgeGO.transform.SetParent(toggleGO.transform, false);

            var badgeImg = _badgeGO.GetComponent<Image>();
            badgeImg.color = new Color(0.9f, 0.3f, 0.2f, 1f); // Red badge

            var badgeRT = _badgeGO.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(1f, 1f);
            badgeRT.anchorMax = new Vector2(1f, 1f);
            badgeRT.pivot = new Vector2(1f, 1f);
            badgeRT.anchoredPosition = new Vector2(-4f, -2f);
            badgeRT.sizeDelta = new Vector2(28f, 22f);

            var badgeTextGO = new GameObject("Text", typeof(Text));
            badgeTextGO.transform.SetParent(_badgeGO.transform, false);

            _badgeText = badgeTextGO.GetComponent<Text>();
            _badgeText.font = _font;
            _badgeText.fontSize = 14;
            _badgeText.fontStyle = FontStyle.Bold;
            _badgeText.color = Color.white;
            _badgeText.alignment = TextAnchor.MiddleCenter;

            var badgeTextRT = badgeTextGO.GetComponent<RectTransform>();
            badgeTextRT.anchorMin = Vector2.zero;
            badgeTextRT.anchorMax = Vector2.one;
            badgeTextRT.offsetMin = Vector2.zero;
            badgeTextRT.offsetMax = Vector2.zero;

            _badgeGO.SetActive(false); // Hidden until unread messages

            // ScrollRect (hidden when collapsed)
            _scrollGO = new GameObject("ScrollView", typeof(Image), typeof(ScrollRect));
            _scrollGO.transform.SetParent(panelGO.transform, false);

            var scrollImg = _scrollGO.GetComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.25f);

            var scrollLE = _scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredHeight = ExpandedHeight - CollapsedHeight - 12f;
            scrollLE.flexibleHeight = 0;

            var scrollRT = _scrollGO.GetComponent<RectTransform>();
            scrollRT.sizeDelta = new Vector2(0, ExpandedHeight - CollapsedHeight - 12f);

            // Viewport
            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(_scrollGO.transform, false);

            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();

            // Content (vertical list)
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            _contentTransform = contentGO.transform;

            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0, 1);
            contentRT.offsetMin = new Vector2(4, 0);
            contentRT.offsetMax = new Vector2(-4, 0);

            var contentVL = contentGO.GetComponent<VerticalLayoutGroup>();
            contentVL.spacing = 2;
            contentVL.childControlWidth = true;
            contentVL.childForceExpandWidth = true;
            contentVL.childControlHeight = true;
            contentVL.childForceExpandHeight = false;
            contentVL.childAlignment = TextAnchor.UpperLeft;

            var contentFitter = contentGO.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Wire ScrollRect
            _scrollRect = _scrollGO.GetComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.viewport = viewportRT;
            _scrollRect.content = contentRT;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Start collapsed
            _expanded = false;
            _scrollGO.SetActive(false);
        }

        // =====================================================================
        // TOGGLE
        // =====================================================================

        private void Toggle()
        {
            _expanded = !_expanded;

            _scrollGO.SetActive(_expanded);
            _panelRect.sizeDelta = new Vector2(PanelWidth, _expanded ? ExpandedHeight : CollapsedHeight);
            _toggleText.text = _expanded ? "Conversations \u25BC" : "Conversations \u25B6"; // ▼ / ▶

            if (_expanded)
            {
                _unreadCount = 0;
                UpdateBadge();
                RefreshEntryList();
                StartCoroutine(ScrollToBottomNextFrame());
            }
        }

        private void UpdateBadge()
        {
            if (_badgeGO == null) return;

            if (_unreadCount <= 0)
            {
                _badgeGO.SetActive(false);
                return;
            }

            _badgeGO.SetActive(true);
            _badgeText.text = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
        }

        // =====================================================================
        // ENTRY LIST
        // =====================================================================

        private void RefreshEntryList()
        {
            // Deactivate all pooled entries
            for (int i = 0; i < _entryPool.Count; i++)
            {
                if (_entryPool[i].gameObject != null)
                    _entryPool[i].gameObject.SetActive(false);
            }

            // Populate from entries
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var pooled = GetOrCreateEntry(i);

                // Build rich text: colored speaker name + dimmed listener name + dialogue text
                string colorHex = ColorUtility.ToHtmlStringRGB(entry.color);
                if (!string.IsNullOrEmpty(entry.listenerName))
                    pooled.text.text = $"<color=#{colorHex}>{entry.speakerName}</color> \u2192 <color=#AAAAAA>{entry.listenerName}</color>: {entry.text}";
                else
                    pooled.text.text = $"<color=#{colorHex}>{entry.speakerName}</color>: {entry.text}";

                // Wire click handler
                int capturedIndex = i;
                pooled.button.onClick.RemoveAllListeners();
                pooled.button.onClick.AddListener(() => OnEntryClicked(capturedIndex));

                pooled.gameObject.SetActive(true);
                pooled.gameObject.transform.SetAsLastSibling();
            }

            // Auto-scroll to bottom
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private PooledEntry GetOrCreateEntry(int index)
        {
            // Return existing if available
            if (index < _entryPool.Count)
                return _entryPool[index];

            // Create new entry
            var entryGO = new GameObject($"Entry_{index}", typeof(Image), typeof(Button), typeof(LayoutElement));
            entryGO.transform.SetParent(_contentTransform, false);

            var bg = entryGO.GetComponent<Image>();
            bg.color = index % 2 == 0
                ? new Color(0.08f, 0.1f, 0.12f, 0.6f)
                : new Color(0.05f, 0.07f, 0.09f, 0.6f);

            var btn = entryGO.GetComponent<Button>();

            var le = entryGO.GetComponent<LayoutElement>();
            le.minHeight = EntryMinHeight;

            // Text child
            var textGO = new GameObject("Text", typeof(Text), typeof(ContentSizeFitter));
            textGO.transform.SetParent(entryGO.transform, false);

            var text = textGO.GetComponent<Text>();
            text.font = _font;
            text.fontSize = 18;
            text.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;

            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = new Vector2(8, 2);
            textRT.offsetMax = new Vector2(-8, -2);

            var textFitter = textGO.GetComponent<ContentSizeFitter>();
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var pooled = new PooledEntry
            {
                gameObject = entryGO,
                button = btn,
                text = text,
                background = bg
            };

            _entryPool.Add(pooled);
            return pooled;
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            // Wait one frame for layout rebuild
            yield return null;
            if (_scrollRect != null)
                _scrollRect.normalizedPosition = Vector2.zero; // Bottom
        }

        // =====================================================================
        // CLICK-TO-HIGHLIGHT
        // =====================================================================

        private void OnEntryClicked(int index)
        {
            if (index < 0 || index >= _entries.Count) return;

            var entry = _entries[index];

            // Check if speaker is still alive (Unity null check)
            if (entry.speaker == null || !entry.speaker.gameObject.activeInHierarchy)
                return;

            // Stop any existing highlight and restore its color
            CancelActiveHighlight();

            _activeHighlight = StartCoroutine(HighlightEntity(entry.speaker));
        }

        private void CancelActiveHighlight()
        {
            if (_activeHighlight != null)
            {
                StopCoroutine(_activeHighlight);
                _activeHighlight = null;

                // Restore the previously highlighted entity's color
                if (_highlightedEntity != null)
                {
                    var sr = _highlightedEntity.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = _highlightOriginalColor;
                }
                _highlightedEntity = null;
            }
        }

        private IEnumerator HighlightEntity(GridEntity entity)
        {
            if (entity == null) yield break;

            var sr = entity.GetComponent<SpriteRenderer>();
            if (sr == null) yield break;

            // Track for cancellation
            _highlightedEntity = entity;
            _highlightOriginalColor = sr.color;

            Color highlight = new Color(1f, 0.95f, 0.2f, 1f); // Yellow highlight
            float elapsed = 0f;

            while (elapsed < HighlightDuration)
            {
                // Check entity still alive each frame
                if (entity == null || !entity.gameObject.activeInHierarchy)
                {
                    if (sr != null) sr.color = _highlightOriginalColor;
                    _activeHighlight = null;
                    _highlightedEntity = null;
                    yield break;
                }

                // Pulse: smoothly oscillate between original and highlight
                float t = Mathf.PingPong(elapsed * 3f, 1f);
                sr.color = Color.Lerp(_highlightOriginalColor, highlight, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore original color
            if (entity != null && sr != null)
                sr.color = _highlightOriginalColor;

            _activeHighlight = null;
            _highlightedEntity = null;
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        private Font GetFont()
        {
            string[] candidates = { "Menlo", "Consolas", "Courier New", "Lucida Console" };
            foreach (var name in candidates)
            {
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, 14);
                    if (f != null) return f;
                }
                catch { }
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
