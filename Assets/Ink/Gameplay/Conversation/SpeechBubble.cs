using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Floating speech bubble above an entity's head. Anchors to the entity's position,
    /// persists for a number of game turns, then fades out. Pooled by SpeechBubblePool.
    ///
    /// Modeled after WorldEventToast but stays in place instead of rising,
    /// and includes a semi-transparent background panel for readability.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        private Text _text;
        private Image _background;
        private RectTransform _bgRect; // Cached to avoid GetComponent<RectTransform>() in Show()
        private GridEntity _anchor;
        private Vector3 _offset;
        private Color _baseColor;
        private SpeechBubblePool _owner;

        // Turn-based lifetime: bubble stays opaque for TurnLifetime game turns,
        // then fades out over FadeDuration real-time seconds.
        private const int TurnLifetime = 2;
        private const float FadeDuration = 0.5f;
        private const float BubbleScale = 0.008f;

        private int _shownOnTurn;
        private bool _fading;
        private float _fadeElapsed;

        public void Initialize(SpeechBubblePool owner, Font font)
        {
            _owner = owner;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 215; // Above WorldEventToast (210), ReputationToast (205)

            RectTransform canvasRect = GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(6f, 1.5f);
            canvasRect.localScale = Vector3.one * BubbleScale;

            // Background panel for readability
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(transform, false);

            _background = bgGO.AddComponent<Image>();
            _background.color = new Color(0f, 0f, 0f, 0.55f);
            _background.raycastTarget = false;

            _bgRect = bgGO.GetComponent<RectTransform>();
            _bgRect.sizeDelta = new Vector2(520, 70);
            _bgRect.anchoredPosition = Vector2.zero;

            // Text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(transform, false);

            _text = textGO.AddComponent<Text>();
            _text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 20;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(500, 60);
            textRect.anchoredPosition = Vector2.zero;

            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.9f);
            outline.effectDistance = new Vector2(1, -1);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the bubble above the given entity with the given text and color.
        /// </summary>
        public void Show(GridEntity anchor, string message, Color color)
        {
            if (_text == null) return;

            _anchor = anchor;
            _text.text = message;
            _baseColor = color;
            _text.color = color;

            // Capture current turn for turn-based lifetime
            _shownOnTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
            _fading = false;
            _fadeElapsed = 0f;

            // Offset above entity head
            float tileSize = GridWorld.Instance != null ? GridWorld.Instance.tileSize : 0.5f;
            _offset = Vector3.up * tileSize * 1.4f;

            if (_anchor != null)
                transform.position = _anchor.transform.position + _offset;

            // Scale background to text width (approximate) — uses cached RectTransform
            if (_background != null && _bgRect != null)
            {
                float approxWidth = Mathf.Min(message.Length * 12f + 20f, 520f);
                _bgRect.sizeDelta = new Vector2(approxWidth, 50f);
                // Reset alpha in case recycled bubble was mid-fade
                _background.color = new Color(0f, 0f, 0f, 0.55f);
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            // Follow anchor entity
            if (_anchor != null && _anchor.gameObject.activeInHierarchy)
            {
                transform.position = _anchor.transform.position + _offset;
            }
            // else: anchor died — just let it stay/fade at last known position

            // Phase A: Waiting for turns to elapse (bubble stays fully opaque)
            if (!_fading)
            {
                int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 0;
                if (currentTurn < _shownOnTurn + TurnLifetime)
                    return; // Still within turn lifetime, stay opaque

                // Turn threshold reached — begin fade
                _fading = true;
                _fadeElapsed = 0f;
            }

            // Phase B: Fading out over real-time
            _fadeElapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(_fadeElapsed / FadeDuration);

            Color textColor = _baseColor;
            textColor.a = alpha;
            _text.color = textColor;

            if (_background != null)
            {
                Color bgColor = _background.color;
                bgColor.a = 0.55f * alpha;
                _background.color = bgColor;
            }

            if (_fadeElapsed >= FadeDuration)
            {
                _owner?.Recycle(this);
            }
        }
    }
}
