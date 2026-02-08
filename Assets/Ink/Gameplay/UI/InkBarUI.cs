using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Displays player ink (mana) as a droplet icon + fraction text (e.g. 💧 42/50).
    /// Positioned just below HealthUI in the top-left corner.
    /// Follows the same world-space camera-following pattern as HealthUI.
    /// </summary>
    public class InkBarUI : MonoBehaviour
    {
        [Header("References")]
        public PlayerController player;

        [Header("Layout")]
        public float iconSize = 0.3f;
        public float textOffset = 0.2f;
        /// <summary>Offset from top-left viewport corner (x right, y down).</summary>
        public Vector2 screenOffset = new Vector2(0.5f, 0.9f);

        [Header("Colors")]
        public Color inkColor = new Color(0.3f, 0.6f, 1f, 1f); // Blue ink
        public Color lowInkColor = new Color(1f, 0.4f, 0.4f, 1f); // Red when low
        public float lowInkThreshold = 0.25f;

        private SpriteRenderer _icon;
        private TextMesh _inkText;
        private Camera _camera;
        private int _lastInk = -1;
        private int _lastMaxInk = -1;
        private Vector3 _lastCamPos;

        private void Start()
        {
            _camera = Camera.main;

            if (player == null)
                player = FindFirstObjectByType<PlayerController>();

            CreateUI();

            if (player != null)
                UpdateInk();
        }

        private void CreateUI()
        {
            // Ink droplet icon (procedural circle sprite with blue tint)
            GameObject iconGO = new GameObject("InkIcon");
            iconGO.transform.SetParent(transform);
            _icon = iconGO.AddComponent<SpriteRenderer>();
            _icon.sprite = CreateCircleSprite();
            _icon.sortingOrder = 100;
            _icon.color = inkColor;

            // Ink text
            GameObject textGO = new GameObject("InkText");
            textGO.transform.SetParent(transform);
            _inkText = textGO.AddComponent<TextMesh>();
            _inkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_inkText.font == null)
                _inkText.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            _inkText.fontSize = 32;
            _inkText.characterSize = 0.04f;
            _inkText.anchor = TextAnchor.MiddleLeft;
            _inkText.alignment = TextAlignment.Left;
            _inkText.color = Color.white;

            var textRenderer = textGO.GetComponent<MeshRenderer>();
            if (textRenderer != null)
                textRenderer.sortingOrder = 100;

            textGO.transform.localScale = Vector3.one * 0.02f;
        }

        private void LateUpdate()
        {
            if (player == null) return;

            if (player.currentInk != _lastInk || player.maxInk != _lastMaxInk)
                UpdateInk();

            if (_camera != null && _camera.transform.position != _lastCamPos)
            {
                PositionUI();
                _lastCamPos = _camera.transform.position;
            }
        }

        private void UpdateInk()
        {
            int ink = player.currentInk;
            int maxInk = player.maxInk;

            if (_inkText != null)
                _inkText.text = $"{ink}/{maxInk}";

            // Flash red when low on ink
            float ratio = maxInk > 0 ? (float)ink / maxInk : 0f;
            if (_icon != null)
                _icon.color = ratio <= lowInkThreshold ? lowInkColor : inkColor;

            _lastInk = ink;
            _lastMaxInk = maxInk;
        }

        private void PositionUI()
        {
            if (_camera == null) return;

            Vector3 topLeft = _camera.ViewportToWorldPoint(new Vector3(0, 1, _camera.nearClipPlane));
            topLeft.z = 0;
            Vector3 startPos = topLeft + new Vector3(screenOffset.x, -screenOffset.y, 0);

            if (_icon != null)
            {
                _icon.transform.position = startPos;
                _icon.transform.localScale = Vector3.one * iconSize;
            }

            if (_inkText != null)
            {
                _inkText.transform.position = startPos + new Vector3(textOffset, 0, 0);
                _inkText.transform.localScale = Vector3.one * 0.02f;
            }
        }

        /// <summary>Create a small filled circle sprite for the ink icon.</summary>
        private static Sprite CreateCircleSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float radius = center - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01((radius - dist) * 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
