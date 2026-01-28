using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Floating reputation toast that rises and fades.
    /// </summary>
    public class ReputationToast : MonoBehaviour
    {
        [Header("Animation")]
        public float riseSpeed = 1.2f;
        public float duration = 1.0f;

        private Text _text;
        private float _elapsed;
        private Vector3 _startPos;
        private Color _baseColor;
        private ReputationToastManager _owner;

        public void Initialize(ReputationToastManager owner, Font font)
        {
            _owner = owner;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 205;

            RectTransform canvasRect = GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(3f, 1f);
            canvasRect.localScale = Vector3.one * 0.01f;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(transform, false);

            _text = textGO.AddComponent<Text>();
            _text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 28;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(300, 80);
            textRect.anchoredPosition = Vector2.zero;

            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
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

            transform.position = _startPos + Vector3.up * (riseSpeed * _elapsed);

            float alpha = 1f - (_elapsed / duration);
            Color c = _baseColor;
            c.a = Mathf.Max(0f, alpha);
            _text.color = c;

            if (_elapsed >= duration)
            {
                _owner?.Recycle(this);
            }
        }
    }
}
