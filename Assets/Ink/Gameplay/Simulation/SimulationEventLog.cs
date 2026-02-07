using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Shows world simulation events as floating toast messages near the player.
    /// Events include raids, territory contests, inscription changes, quest generation,
    /// NPC behavior changes, and economic shifts.
    ///
    /// Two modes:
    /// 1. WorldToast — floating text at a world position (like DamageNumber but for events)
    /// 2. BannerMessage — screen-space banner at top of screen for major events
    /// </summary>
    public class SimulationEventLog : MonoBehaviour
    {
        public static SimulationEventLog Instance { get; private set; }

        // Color palette for event types
        public static readonly Color ColorRaid = new Color(1f, 0.3f, 0.2f, 1f);        // Red — raids, attacks
        public static readonly Color ColorContest = new Color(1f, 0.6f, 0.1f, 1f);     // Orange — territory contests
        public static readonly Color ColorInscription = new Color(0.4f, 0.8f, 1f, 1f); // Light blue — inscriptions
        public static readonly Color ColorQuest = new Color(1f, 0.9f, 0.3f, 1f);       // Gold — quests
        public static readonly Color ColorDiplomacy = new Color(0.7f, 0.5f, 1f, 1f);   // Purple — diplomacy
        public static readonly Color ColorSpawn = new Color(0.5f, 1f, 0.6f, 1f);       // Green — reinforcements
        public static readonly Color ColorEconomy = new Color(0.3f, 0.9f, 0.9f, 1f);   // Cyan — economic changes
        public static readonly Color ColorFlee = new Color(0.9f, 0.9f, 0.4f, 1f);      // Yellow — fleeing NPCs

        // Toast pooling
        private readonly Queue<WorldEventToast> _toastPool = new Queue<WorldEventToast>();
        private Transform _toastRoot;
        private Font _font;

        // Banner system
        private GameObject _bannerCanvas;
        private Text _bannerText;
        private float _bannerTimer;
        private const float BannerDuration = 3.5f;

        // Message log (recent events for debug/reference)
        private readonly List<string> _recentEvents = new List<string>();
        private const int MaxRecentEvents = 20;

        // Throttle: don't spam toasts
        private float _lastToastTime;
        private const float MinToastInterval = 0.3f;
        private readonly Queue<QueuedToast> _toastQueue = new Queue<QueuedToast>();

        private struct QueuedToast
        {
            public string message;
            public Color color;
            public Vector3 position;
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
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _toastRoot = new GameObject("SimulationToasts").transform;
            _toastRoot.SetParent(transform, false);

            _font = Font.CreateDynamicFontFromOSFont("Courier New", 24);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateBanner();
        }

        private void Update()
        {
            // Fade banner
            if (_bannerCanvas != null && _bannerCanvas.activeSelf)
            {
                _bannerTimer -= Time.deltaTime;
                if (_bannerTimer <= 0f)
                {
                    _bannerCanvas.SetActive(false);
                }
                else if (_bannerTimer < 1f)
                {
                    // Fade out
                    Color c = _bannerText.color;
                    c.a = _bannerTimer;
                    _bannerText.color = c;
                }
            }

            // Process queued toasts
            if (_toastQueue.Count > 0 && Time.time - _lastToastTime >= MinToastInterval)
            {
                var queued = _toastQueue.Dequeue();
                SpawnToastImmediate(queued.message, queued.color, queued.position);
            }
        }

        // === PUBLIC API ===

        /// <summary>
        /// Show a floating world-space toast at a specific position.
        /// Used for localized events (e.g., "Skeleton reinforcements!" at a district).
        /// </summary>
        public static void Toast(string message, Color color, Vector3 worldPos)
        {
            if (Instance == null) return;

            if (Time.time - Instance._lastToastTime < MinToastInterval)
            {
                Instance._toastQueue.Enqueue(new QueuedToast { message = message, color = color, position = worldPos });
            }
            else
            {
                Instance.SpawnToastImmediate(message, color, worldPos);
            }

            Instance.LogEvent(message);
        }

        /// <summary>
        /// Show a floating toast at the player's position.
        /// Used for player-relevant events.
        /// </summary>
        public static void ToastAtPlayer(string message, Color color)
        {
            var player = FindObjectOfType<PlayerController>();
            Vector3 pos = player != null
                ? player.transform.position + Vector3.up * 0.5f
                : Vector3.zero;
            Toast(message, color, pos);
        }

        /// <summary>
        /// Show a large screen-space banner for major events.
        /// Used for raids, skirmishes, major territory changes.
        /// </summary>
        public static void Banner(string message, Color color)
        {
            if (Instance == null) return;
            Instance.ShowBanner(message, color);
            Instance.LogEvent($"[BANNER] {message}");
        }

        /// <summary>
        /// Show a toast at a grid position (converts grid → world coords).
        /// </summary>
        public static void ToastAtGrid(string message, Color color, int gridX, int gridY)
        {
            var gw = GridWorld.Instance;
            if (gw == null) return;
            Vector3 worldPos = gw.GridToWorld(gridX, gridY) + Vector3.up * 0.3f;
            Toast(message, color, worldPos);
        }

        /// <summary>
        /// Log an event without showing a toast or banner. Used for ambient events
        /// like NPC conversations that don't need visual indicators.
        /// </summary>
        public static void LogSilent(string message)
        {
            if (Instance == null) return;
            Instance.LogEvent(message);
        }

        /// <summary>
        /// Get recent event messages for display/debug.
        /// </summary>
        public static IReadOnlyList<string> RecentEvents => Instance?._recentEvents;

        // === INTERNAL ===

        private void SpawnToastImmediate(string message, Color color, Vector3 worldPos)
        {
            _lastToastTime = Time.time;
            WorldEventToast toast = GetToast();
            toast.Play(message, color, worldPos);
        }

        private WorldEventToast GetToast()
        {
            if (_toastPool.Count > 0)
            {
                var toast = _toastPool.Dequeue();
                toast.transform.SetParent(_toastRoot, false);
                return toast;
            }

            GameObject go = new GameObject("WorldEventToast");
            go.transform.SetParent(_toastRoot, false);
            var toastComp = go.AddComponent<WorldEventToast>();
            toastComp.Initialize(this, _font);
            return toastComp;
        }

        internal void RecycleToast(WorldEventToast toast)
        {
            if (toast == null) return;
            toast.gameObject.SetActive(false);
            _toastPool.Enqueue(toast);
        }

        private void CreateBanner()
        {
            _bannerCanvas = new GameObject("SimBannerCanvas");
            _bannerCanvas.transform.SetParent(transform, false);

            Canvas canvas = _bannerCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;

            _bannerCanvas.AddComponent<CanvasScaler>();

            GameObject textGO = new GameObject("BannerText");
            textGO.transform.SetParent(_bannerCanvas.transform, false);

            _bannerText = textGO.AddComponent<Text>();
            _bannerText.font = _font;
            _bannerText.fontSize = 22;
            _bannerText.fontStyle = FontStyle.Bold;
            _bannerText.alignment = TextAnchor.UpperCenter;
            _bannerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bannerText.verticalOverflow = VerticalWrapMode.Overflow;
            _bannerText.raycastTarget = false;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0, -40);
            textRect.sizeDelta = new Vector2(800, 60);

            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);

            // Add shadow for extra readability
            Shadow shadow = textGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(3, -3);

            _bannerCanvas.SetActive(false);
        }

        private void ShowBanner(string message, Color color)
        {
            if (_bannerText == null) return;

            _bannerText.text = message;
            _bannerText.color = color;
            _bannerTimer = BannerDuration;
            _bannerCanvas.SetActive(true);
        }

        private void LogEvent(string message)
        {
            _recentEvents.Add($"[Day {DistrictControlService.Instance?.CurrentDay ?? 0}] {message}");
            while (_recentEvents.Count > MaxRecentEvents)
                _recentEvents.RemoveAt(0);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// Individual floating toast — rises and fades like DamageNumber but for text messages.
    /// Lasts longer than damage numbers (2.5s vs 0.8s) and uses larger text.
    /// </summary>
    public class WorldEventToast : MonoBehaviour
    {
        private Text _text;
        private float _elapsed;
        private Vector3 _startPos;
        private Color _baseColor;
        private SimulationEventLog _owner;

        private const float RiseSpeed = 0.8f;
        private const float Duration = 2.5f;

        public void Initialize(SimulationEventLog owner, Font font)
        {
            _owner = owner;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 210;

            RectTransform canvasRect = GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(5f, 1.5f);
            canvasRect.localScale = Vector3.one * 0.008f;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(transform, false);

            _text = textGO.AddComponent<Text>();
            _text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 24;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(500, 80);
            textRect.anchoredPosition = Vector2.zero;

            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
        }

        public void Play(string message, Color color, Vector3 worldPos)
        {
            if (_text == null) return;

            _text.text = message;
            _baseColor = color;
            _text.color = color;
            _elapsed = 0f;
            _startPos = worldPos;
            transform.position = worldPos;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position = _startPos + Vector3.up * (RiseSpeed * _elapsed);

            // Hold opaque for 1.5s, then fade over 1s
            float alpha;
            if (_elapsed < 1.5f)
                alpha = 1f;
            else
                alpha = 1f - ((_elapsed - 1.5f) / (Duration - 1.5f));

            Color c = _baseColor;
            c.a = Mathf.Max(0f, alpha);
            _text.color = c;

            if (_elapsed >= Duration)
                _owner?.RecycleToast(this);
        }
    }
}
