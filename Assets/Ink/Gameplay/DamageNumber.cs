using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Floating damage number that rises and fades.
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        private const int MaxPoolSize = 64;

        [Header("Animation")]
        public float riseSpeed = 1.5f;
        public float duration = 0.8f;

        // Preset colors
        public static readonly Color ColorNormal = new Color(1f, 1f, 1f, 1f);      // White
        public static readonly Color ColorPlayerHit = new Color(1f, 0.3f, 0.3f, 1f); // Red
        public static readonly Color ColorHeal = new Color(0.3f, 1f, 0.5f, 1f);    // Green
        public static readonly Color ColorCrit = new Color(1f, 0.9f, 0.3f, 1f);    // Yellow/Gold

        private static readonly Queue<DamageNumber> _pool = new Queue<DamageNumber>();
        private static Font _monoFont;

        private Text _text;
        private RectTransform _rect;
        private float _elapsed;
        private Vector3 _startPos;

        /// <summary>
        /// Spawn a damage number at world position with default white color.
        /// </summary>
        public static DamageNumber Spawn(Vector3 worldPos, int damage)
        {
            return Spawn(worldPos, damage, ColorNormal);
        }

        /// <summary>
        /// Spawn a damage number at world position with specified color.
        /// </summary>
        public static DamageNumber Spawn(Vector3 worldPos, int damage, Color color, bool large = false)
        {
            DamageNumber dn = GetPooled();
            dn.Prepare(worldPos, damage, color, large);
            return dn;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // Rise
            transform.position = _startPos + Vector3.up * (riseSpeed * _elapsed);

            // Fade
            float alpha = 1f - (_elapsed / duration);
            Color c = _text.color;
            c.a = Mathf.Max(0, alpha);
            _text.color = c;

            // Destroy when done
            if (_elapsed >= duration)
                Recycle();
        }

        private static DamageNumber GetPooled()
        {
            if (_pool.Count > 0)
            {
                var dn = _pool.Dequeue();
                dn.gameObject.SetActive(true);
                return dn;
            }

            return CreateNew();
        }

        private static DamageNumber CreateNew()
        {
            GameObject canvasGO = new GameObject("DamageNumber");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 1f);
            canvasRect.localScale = Vector3.one * 0.01f;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(canvasGO.transform, false);

            Text text = textGO.AddComponent<Text>();
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            text.font = GetMonoFont();

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 100);
            textRect.anchoredPosition = Vector2.zero;

            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            DamageNumber dn = canvasGO.AddComponent<DamageNumber>();
            dn._text = text;
            dn._rect = canvasRect;

            return dn;
        }

        private void Prepare(Vector3 worldPos, int damage, Color color, bool large)
        {
            _elapsed = 0f;
            _text.text = damage.ToString();
            _text.fontSize = large ? 48 : 36;
            _text.color = color;
            _text.font = GetMonoFont();

            Vector3 pos = worldPos + Vector3.up * 0.3f;
            _startPos = pos;
            if (_rect != null)
                _rect.position = pos;
            else
                transform.position = pos;
        }

        private void Recycle()
        {
            if (_pool.Count < MaxPoolSize)
            {
                gameObject.SetActive(false);
                _pool.Enqueue(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private static Font GetMonoFont()
        {
            if (_monoFont != null) return _monoFont;

            _monoFont = Font.CreateDynamicFontFromOSFont("Courier New", 36);
            if (_monoFont == null)
                _monoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _monoFont;
        }
    }
}
