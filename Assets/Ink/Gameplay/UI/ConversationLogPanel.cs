using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace InkSim
{
    /// <summary>
    /// Expandable/collapsible conversation log panel anchored at bottom-right.
    /// Logs all NPC-to-NPC dialogue lines with faction-colored speaker names.
    /// Conversations are grouped by conversationId — click a group header to
    /// expand/collapse the individual lines within that conversation.
    /// Clicking an individual line highlights the speaker entity for 10 seconds
    /// with layered effects (scale pulse, white flash, floating diamond indicator)
    /// and pans the camera to focus on the entity.
    ///
    /// Public API: ConversationLogPanel.PushLine(speaker, speakerName, listener, listenerName, text, color, conversationId)
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
        private const float HeaderMinHeight = 44f;
        private const float HighlightDuration = 10f;

        // =====================================================================
        // DATA MODEL
        // =====================================================================

        private struct ConversationLogEntry
        {
            public string speakerName;
            public string listenerName;
            public string text;
            public Color color;
            public GridEntity speaker;
            public GridEntity listener;
            public int turnNumber;
            public int conversationId;
        }

        private class ConversationGroup
        {
            public int conversationId;
            public string initiatorName;   // First speaker's display name
            public string responderName;   // First listener's display name
            public Color color;            // Faction color from first line
            public int lastActivityTurn;   // Turn when the most recent line was added
            public GridEntity initiator;   // First speaker entity reference
            public GridEntity responder;   // First listener entity reference
            public readonly List<ConversationLogEntry> entries = new List<ConversationLogEntry>();
        }

        // Group-based storage (replaces flat _entries list)
        private readonly List<ConversationGroup> _groups = new List<ConversationGroup>();
        private readonly Dictionary<int, ConversationGroup> _groupLookup = new Dictionary<int, ConversationGroup>();
        private readonly HashSet<int> _expandedGroups = new HashSet<int>();
        private int _totalEntryCount;
        private int _lastSeenTurn;  // Track when user last viewed — groups with activity after this are "new"

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

        // Header pool (for group headers)
        private readonly List<PooledGroupHeader> _headerPool = new List<PooledGroupHeader>();

        // Entry pool (for individual line entries)
        private readonly List<PooledEntry> _entryPool = new List<PooledEntry>();

        // Cached references (avoid FindFirstObjectByType per click)
        private CameraController _cachedCamera;

        // Cached material for highlight indicators (avoid allocating new Material per highlight)
        private Material _highlightMaterial;

        // Reusable StringBuilder to avoid string allocations in ConfigureGroupHeader/ConfigureLineEntry
        private readonly StringBuilder _sb = new StringBuilder(256);

        // Highlight tracking — supports multiple simultaneous highlights (e.g., both entities in a conversation)
        private struct HighlightState
        {
            public Coroutine coroutine;
            public GridEntity entity;
            public Color originalColor;
            public Vector3 originalScale;
            public GameObject indicatorGO;
        }
        private readonly List<HighlightState> _activeHighlights = new List<HighlightState>(2);

        private class PooledGroupHeader
        {
            public GameObject gameObject;
            public Button arrowButton;      // Left-side arrow — toggles expand/collapse
            public Text arrowText;          // ▶ or ▼
            public Button bodyButton;       // Rest of header — camera pan + highlight both entities
            public Text bodyText;           // Colored names + count + NEW tag
            public Image background;
        }

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
            _cachedCamera = UnityEngine.Object.FindFirstObjectByType<CameraController>();

            // Pre-create shared material for highlight indicators
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _highlightMaterial = new Material(shader);
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
                                    string text, Color color, int conversationId)
        {
            if (Instance == null) return;
            Instance.PushLineInternal(speaker, speakerName, listener, listenerName, text, color, conversationId);
        }

        private void PushLineInternal(GridEntity speaker, string speakerName,
                                      GridEntity listener, string listenerName,
                                      string text, Color color, int conversationId)
        {
            int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;

            var entry = new ConversationLogEntry
            {
                speakerName = speakerName,
                listenerName = listenerName,
                text = text,
                color = color,
                speaker = speaker,
                listener = listener,
                turnNumber = turn,
                conversationId = conversationId
            };

            // Find or create group for this conversation
            if (!_groupLookup.TryGetValue(conversationId, out var group))
            {
                group = new ConversationGroup
                {
                    conversationId = conversationId,
                    initiatorName = speakerName,
                    responderName = listenerName,
                    color = color,
                    initiator = speaker,
                    responder = listener
                };
                _groups.Add(group);
                _groupLookup[conversationId] = group;
            }

            group.entries.Add(entry);
            group.lastActivityTurn = turn;
            _totalEntryCount++;

            // Prune oldest if over limit
            while (_totalEntryCount > MaxEntries)
                PruneOldest();

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

        private void PruneOldest()
        {
            if (_groups.Count == 0) return;

            var oldest = _groups[0];
            oldest.entries.RemoveAt(0);
            _totalEntryCount--;

            if (oldest.entries.Count == 0)
            {
                _groups.RemoveAt(0);
                _groupLookup.Remove(oldest.conversationId);
                _expandedGroups.Remove(oldest.conversationId);
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
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
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
                _lastSeenTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
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
        // GROUP EXPAND / COLLAPSE
        // =====================================================================

        private void ToggleGroup(int conversationId)
        {
            if (!_expandedGroups.Remove(conversationId))
                _expandedGroups.Add(conversationId);
            RefreshEntryList();
        }

        // =====================================================================
        // GROUPED ENTRY LIST
        // =====================================================================

        private void RefreshEntryList()
        {
            // Deactivate all pooled headers
            for (int i = 0; i < _headerPool.Count; i++)
            {
                if (_headerPool[i].gameObject != null)
                    _headerPool[i].gameObject.SetActive(false);
            }

            // Deactivate all pooled entries
            for (int i = 0; i < _entryPool.Count; i++)
            {
                if (_entryPool[i].gameObject != null)
                    _entryPool[i].gameObject.SetActive(false);
            }

            int headerIndex = 0;
            int entryIndex = 0;

            for (int g = 0; g < _groups.Count; g++)
            {
                var group = _groups[g];
                bool groupExpanded = _expandedGroups.Contains(group.conversationId);

                // --- Render group header ---
                var header = GetOrCreateHeader(headerIndex++);
                ConfigureGroupHeader(header, group, groupExpanded);

                // --- If expanded, render individual line entries ---
                if (groupExpanded)
                {
                    for (int e = 0; e < group.entries.Count; e++)
                    {
                        var pooled = GetOrCreateEntry(entryIndex++);
                        ConfigureLineEntry(pooled, group.entries[e], e);
                    }
                }
            }

            // Auto-scroll to bottom
            StartCoroutine(ScrollToBottomNextFrame());
        }

        // =====================================================================
        // GROUP HEADER POOL
        // =====================================================================

        private PooledGroupHeader GetOrCreateHeader(int index)
        {
            if (index < _headerPool.Count)
                return _headerPool[index];

            // Create header container with horizontal layout for arrow | body split
            var headerGO = new GameObject($"Header_{index}", typeof(Image), typeof(LayoutElement));
            headerGO.transform.SetParent(_contentTransform, false);

            var bg = headerGO.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.13f, 0.18f, 0.95f);

            var le = headerGO.GetComponent<LayoutElement>();
            le.minHeight = HeaderMinHeight;

            var hlg = headerGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            // --- Arrow button (left, fixed width ~36px) ---
            var arrowGO = new GameObject("Arrow", typeof(Image), typeof(Button), typeof(LayoutElement));
            arrowGO.transform.SetParent(headerGO.transform, false);

            var arrowImg = arrowGO.GetComponent<Image>();
            arrowImg.color = new Color(0.08f, 0.10f, 0.14f, 0.95f); // Slightly darker than header

            var arrowBtn = arrowGO.GetComponent<Button>();

            var arrowLE = arrowGO.GetComponent<LayoutElement>();
            arrowLE.minWidth = 36f;
            arrowLE.preferredWidth = 36f;
            arrowLE.flexibleWidth = 0;

            // Arrow text child
            var arrowTextGO = new GameObject("Text", typeof(Text));
            arrowTextGO.transform.SetParent(arrowGO.transform, false);

            var arrowText = arrowTextGO.GetComponent<Text>();
            arrowText.font = _font;
            arrowText.fontSize = 18;
            arrowText.fontStyle = FontStyle.Bold;
            arrowText.color = new Color(0.7f, 0.85f, 0.9f, 1f);
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.text = "\u25B6"; // ▶

            var arrowTextRT = arrowTextGO.GetComponent<RectTransform>();
            arrowTextRT.anchorMin = Vector2.zero;
            arrowTextRT.anchorMax = Vector2.one;
            arrowTextRT.offsetMin = Vector2.zero;
            arrowTextRT.offsetMax = Vector2.zero;

            // --- Body button (right, flexible width) ---
            var bodyGO = new GameObject("Body", typeof(Image), typeof(Button), typeof(LayoutElement));
            bodyGO.transform.SetParent(headerGO.transform, false);

            var bodyImg = bodyGO.GetComponent<Image>();
            bodyImg.color = new Color(0f, 0f, 0f, 0f); // Transparent — header bg shows through

            var bodyBtn = bodyGO.GetComponent<Button>();

            var bodyLE = bodyGO.GetComponent<LayoutElement>();
            bodyLE.flexibleWidth = 1f;

            // Body text child
            var bodyTextGO = new GameObject("Text", typeof(Text), typeof(ContentSizeFitter));
            bodyTextGO.transform.SetParent(bodyGO.transform, false);

            var bodyText = bodyTextGO.GetComponent<Text>();
            bodyText.font = _font;
            bodyText.fontSize = 18;
            bodyText.fontStyle = FontStyle.Bold;
            bodyText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            bodyText.alignment = TextAnchor.MiddleLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.supportRichText = true;

            var bodyTextRT = bodyTextGO.GetComponent<RectTransform>();
            bodyTextRT.anchorMin = new Vector2(0, 0);
            bodyTextRT.anchorMax = new Vector2(1, 1);
            bodyTextRT.offsetMin = new Vector2(6, 2);
            bodyTextRT.offsetMax = new Vector2(-8, -2);

            var bodyTextFitter = bodyTextGO.GetComponent<ContentSizeFitter>();
            bodyTextFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            bodyTextFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var pooled = new PooledGroupHeader
            {
                gameObject = headerGO,
                arrowButton = arrowBtn,
                arrowText = arrowText,
                bodyButton = bodyBtn,
                bodyText = bodyText,
                background = bg
            };

            _headerPool.Add(pooled);
            return pooled;
        }

        private void ConfigureGroupHeader(PooledGroupHeader pooled, ConversationGroup group, bool groupExpanded)
        {
            bool hasNewActivity = group.lastActivityTurn > _lastSeenTurn;

            // Arrow text (just the indicator)
            pooled.arrowText.text = groupExpanded ? "\u25BC" : "\u25B6"; // ▼ / ▶

            // Body text: colored names + count + optional NEW tag (using StringBuilder)
            _sb.Clear();
            _sb.Append("<color=#");
            _sb.Append(ColorUtility.ToHtmlStringRGB(group.color));
            _sb.Append('>');
            _sb.Append(group.initiatorName);
            _sb.Append("</color> \u2194 <color=#AAAAAA>");
            _sb.Append(group.responderName);
            _sb.Append("</color> <color=#888888>(");
            _sb.Append(group.entries.Count);
            _sb.Append(")</color>");
            if (hasNewActivity && !groupExpanded)
                _sb.Append(" <color=#FFD700>NEW</color>");

            pooled.bodyText.text = _sb.ToString();

            // Header styling: brighter if new activity, darker when expanded
            if (hasNewActivity && !groupExpanded)
                pooled.background.color = new Color(0.16f, 0.18f, 0.10f, 0.95f); // Warm highlight for new activity
            else if (groupExpanded)
                pooled.background.color = new Color(0.14f, 0.17f, 0.22f, 0.95f);
            else
                pooled.background.color = new Color(0.1f, 0.13f, 0.18f, 0.95f);

            // Arrow click → expand/collapse only (no camera pan)
            pooled.arrowButton.onClick.RemoveAllListeners();
            int convId = group.conversationId;
            pooled.arrowButton.onClick.AddListener(() => ToggleGroup(convId));

            // Body click → camera pan to midpoint + highlight BOTH entities (no expand/collapse)
            pooled.bodyButton.onClick.RemoveAllListeners();
            var capturedInitiator = group.initiator;
            var capturedResponder = group.responder;
            pooled.bodyButton.onClick.AddListener(() => OnGroupBodyClicked(capturedInitiator, capturedResponder));

            pooled.gameObject.SetActive(true);
            pooled.gameObject.transform.SetAsLastSibling();
        }

        // =====================================================================
        // LINE ENTRY POOL
        // =====================================================================

        private PooledEntry GetOrCreateEntry(int index)
        {
            // Return existing if available
            if (index < _entryPool.Count)
                return _entryPool[index];

            // Create new entry
            var entryGO = new GameObject($"Entry_{index}", typeof(Image), typeof(Button), typeof(LayoutElement));
            entryGO.transform.SetParent(_contentTransform, false);

            var bg = entryGO.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.10f, 0.6f);

            var btn = entryGO.GetComponent<Button>();

            var le = entryGO.GetComponent<LayoutElement>();
            le.minHeight = EntryMinHeight;

            // Text child — indented to show hierarchy under group header
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
            textRT.offsetMin = new Vector2(28, 2); // 28px left indent (vs 8px for headers)
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

        private void ConfigureLineEntry(PooledEntry pooled, ConversationLogEntry entry, int indexInGroup)
        {
            // Build rich text using StringBuilder to avoid string allocs
            _sb.Clear();
            _sb.Append("<color=#666666>[T");
            _sb.Append(entry.turnNumber);
            _sb.Append("]</color> <color=#");
            _sb.Append(ColorUtility.ToHtmlStringRGB(entry.color));
            _sb.Append('>');
            _sb.Append(entry.speakerName);
            _sb.Append("</color>");
            if (!string.IsNullOrEmpty(entry.listenerName))
            {
                _sb.Append(" \u2192 <color=#AAAAAA>");
                _sb.Append(entry.listenerName);
                _sb.Append("</color>");
            }
            _sb.Append(": ");
            _sb.Append(entry.text);
            pooled.text.text = _sb.ToString();

            // Alternating background for readability
            pooled.background.color = indexInGroup % 2 == 0
                ? new Color(0.06f, 0.08f, 0.10f, 0.6f)
                : new Color(0.04f, 0.06f, 0.08f, 0.6f);

            // Wire click handler — pass entry directly via closure capture
            var capturedEntry = entry;
            pooled.button.onClick.RemoveAllListeners();
            pooled.button.onClick.AddListener(() => OnEntryClicked(capturedEntry));

            pooled.gameObject.SetActive(true);
            pooled.gameObject.transform.SetAsLastSibling();
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

        /// <summary>
        /// Called when the body (names area) of a group header is clicked.
        /// Pans camera to the midpoint between both entities and highlights both.
        /// Does NOT expand/collapse the group.
        /// </summary>
        private void OnGroupBodyClicked(GridEntity initiator, GridEntity responder)
        {
            bool initiatorAlive = initiator != null && initiator.gameObject.activeInHierarchy;
            bool responderAlive = responder != null && responder.gameObject.activeInHierarchy;

            if (!initiatorAlive && !responderAlive)
                return; // Both dead, nothing to do

            // Cancel any existing highlights
            CancelAllHighlights();

            // Highlight each alive entity
            if (initiatorAlive)
                StartHighlight(initiator);
            if (responderAlive)
                StartHighlight(responder);

            // Pan camera to the conversation location (midpoint between both entities)
            Vector3 focusPos;
            if (initiatorAlive && responderAlive)
                focusPos = (initiator.transform.position + responder.transform.position) * 0.5f;
            else if (initiatorAlive)
                focusPos = initiator.transform.position;
            else
                focusPos = responder.transform.position;

            if (_cachedCamera == null)
                _cachedCamera = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (_cachedCamera != null)
                _cachedCamera.FocusOnWorldPosition(focusPos, 10f);
        }

        private void OnEntryClicked(ConversationLogEntry entry)
        {
            // Check if speaker is still alive (Unity null check)
            if (entry.speaker == null || !entry.speaker.gameObject.activeInHierarchy)
                return;

            // Stop any existing highlights
            CancelAllHighlights();

            // Highlight the single speaker entity
            StartHighlight(entry.speaker);

            // Pan camera to speaker and hold for 10 seconds
            if (_cachedCamera == null)
                _cachedCamera = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (_cachedCamera != null)
                _cachedCamera.FocusOnWorldPosition(entry.speaker.transform.position, 10f);
        }

        /// <summary>
        /// Start a highlight on a single entity and track it for cancellation.
        /// </summary>
        private void StartHighlight(GridEntity entity)
        {
            if (entity == null) return;

            var sr = entity.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            var indicatorGO = CreateHighlightIndicator(entity);
            var coroutine = StartCoroutine(HighlightEntity(entity, sr, sr.color, entity.transform.localScale, indicatorGO));

            _activeHighlights.Add(new HighlightState
            {
                coroutine = coroutine,
                entity = entity,
                originalColor = sr.color,
                originalScale = entity.transform.localScale,
                indicatorGO = indicatorGO
            });
        }

        /// <summary>
        /// Cancel all active highlights, restoring original colors/scales and destroying indicators.
        /// </summary>
        private void CancelAllHighlights()
        {
            for (int i = 0; i < _activeHighlights.Count; i++)
            {
                var state = _activeHighlights[i];

                if (state.coroutine != null)
                    StopCoroutine(state.coroutine);

                // Restore entity state
                if (state.entity != null)
                {
                    var sr = state.entity.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = state.originalColor;
                    state.entity.transform.localScale = state.originalScale;
                }

                // Destroy indicator
                if (state.indicatorGO != null)
                    Destroy(state.indicatorGO);
            }

            _activeHighlights.Clear();
        }

        /// <summary>
        /// Coroutine that runs the 3-layer highlight effect on a single entity.
        /// Each coroutine is self-contained with its own state — supports multiple concurrent highlights.
        /// </summary>
        private IEnumerator HighlightEntity(GridEntity entity, SpriteRenderer sr,
            Color originalColor, Vector3 originalScale, GameObject indicatorGO)
        {
            float elapsed = 0f;

            while (elapsed < HighlightDuration)
            {
                // Check entity still alive each frame
                if (entity == null || !entity.gameObject.activeInHierarchy)
                {
                    if (sr != null) sr.color = originalColor;
                    if (entity != null) entity.transform.localScale = originalScale;
                    if (indicatorGO != null) Destroy(indicatorGO);
                    yield break;
                }

                // --- Effect 1: Scale pulse (1.0x -> 1.25x, smooth sine, ~1s period) ---
                float scalePulse = 1f + 0.25f * Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI));
                entity.transform.localScale = originalScale * scalePulse;

                // --- Effect 2: Sharp white flash (every 1.5s, hold white 0.15s, ease back 0.3s) ---
                float flashCycle = elapsed % 1.5f;
                if (flashCycle < 0.15f)
                {
                    sr.color = Color.white;
                }
                else
                {
                    float fadeBack = Mathf.Clamp01((flashCycle - 0.15f) / 0.3f);
                    sr.color = Color.Lerp(Color.white, originalColor, fadeBack);
                }

                // --- Effect 3: Floating diamond indicator follows entity and bobs ---
                if (indicatorGO != null)
                {
                    float tileSize = GridWorld.Instance != null ? GridWorld.Instance.tileSize : 0.5f;
                    float bobOffset = Mathf.Sin(elapsed * 3f) * tileSize * 0.15f;
                    indicatorGO.transform.position =
                        entity.transform.position + Vector3.up * (tileSize * 1.5f + bobOffset);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore original state
            if (entity != null && sr != null)
            {
                sr.color = originalColor;
                entity.transform.localScale = originalScale;
            }

            if (indicatorGO != null)
                Destroy(indicatorGO);
        }

        /// <summary>
        /// Creates a diamond-shaped LineRenderer indicator above the entity.
        /// Uses the same pattern as TileCursor (Sprites/Default shader, LineRenderer).
        /// </summary>
        private GameObject CreateHighlightIndicator(GridEntity entity)
        {
            float tileSize = GridWorld.Instance != null ? GridWorld.Instance.tileSize : 0.5f;

            var go = new GameObject("HighlightIndicator");
            // Not parented to entity — positioned manually each frame for independent cleanup
            go.transform.position = entity.transform.position + Vector3.up * tileSize * 1.5f;

            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 4;
            line.loop = true;
            line.startWidth = 0.03f;
            line.endWidth = 0.03f;
            line.useWorldSpace = false; // positions relative to GO
            line.sortingOrder = 101;    // Just above TileCursor's 100

            if (_highlightMaterial != null)
                line.material = _highlightMaterial;

            Color indicatorColor = new Color(1f, 0.95f, 0.2f, 1f); // Bright yellow
            line.startColor = indicatorColor;
            line.endColor = indicatorColor;

            // Diamond shape (pointing downward like an arrow indicator)
            float size = tileSize * 0.4f;
            line.SetPosition(0, new Vector3(0, size, 0));           // Top
            line.SetPosition(1, new Vector3(size * 0.6f, 0, 0));    // Right
            line.SetPosition(2, new Vector3(0, -size * 0.5f, 0));   // Bottom (pointy)
            line.SetPosition(3, new Vector3(-size * 0.6f, 0, 0));   // Left

            return go;
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
            CancelAllHighlights();

            // Clean up cached material to prevent resource leak
            if (_highlightMaterial != null)
            {
                Destroy(_highlightMaterial);
                _highlightMaterial = null;
            }

            if (Instance == this)
                Instance = null;
        }
    }
}
