using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Displays player health as a heart icon + fraction text (e.g. ❤ 850/1000)
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("References")]
        public PlayerController player;
        public Sprite heartSprite;
        // Legacy references expected by TestMapBuilder; optional use.
        public Sprite heartFull;
        public Sprite heartEmpty;

        [Header("Layout")]
        public float heartSize = 0.4f;
        public float textOffset = 0.25f;
        public Vector2 screenOffset = new Vector2(0.5f, 0.5f);

        private SpriteRenderer _heart;
        private TextMesh _hpText;
        private Camera _camera;
        private int _lastHealth = -1;
        private int _lastMaxHealth = -1;
        private Vector3 _lastCamPos;

        private void Start()
        {
            _camera = Camera.main;
            
            if (player == null)
                player = FindFirstObjectByType<PlayerController>();

            CreateUI();
            
            if (player != null)
                UpdateHealth();
        }

        private void CreateUI()
        {
            // Heart icon
            GameObject heartGO = new GameObject("HeartIcon");
            heartGO.transform.SetParent(transform);
            _heart = heartGO.AddComponent<SpriteRenderer>();
            _heart.sprite = heartSprite;
            _heart.sortingOrder = 100;
            _heart.color = new Color(1f, 0.3f, 0.3f, 1f); // Red tint
            
            // HP text
            GameObject textGO = new GameObject("HPText");
            textGO.transform.SetParent(transform);
            _hpText = textGO.AddComponent<TextMesh>();
            _hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_hpText.font == null)
                _hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_hpText.font == null)
                _hpText.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            _hpText.fontSize = 32;
            _hpText.characterSize = 0.04f; // smaller so the fraction doesn’t fill the screen
            _hpText.anchor = TextAnchor.MiddleLeft;
            _hpText.alignment = TextAlignment.Left;
            _hpText.color = Color.white;

            var textRenderer = textGO.GetComponent<MeshRenderer>();
            if (textRenderer != null)
                textRenderer.sortingOrder = 100;

            // Scale down the text object to a sensible world-space size
            textGO.transform.localScale = Vector3.one * 0.02f;
        }

        private void LateUpdate()
        {
            if (player == null) return;

            // Update text if health changed
            if (player.currentHealth != _lastHealth || player.MaxHealth != _lastMaxHealth)
            {
                UpdateHealth();
            }

            // Only reposition if camera moved
            if (_camera != null && _camera.transform.position != _lastCamPos)
            {
                PositionUI();
                _lastCamPos = _camera.transform.position;
            }
        }

        private void UpdateHealth()
        {
            int hp = player.currentHealth;
            int maxHp = player.MaxHealth;
            
            if (_hpText != null)
                _hpText.text = $"{hp}/{maxHp}";
            
            _lastHealth = hp;
            _lastMaxHealth = maxHp;
        }

        private void PositionUI()
        {
            if (_camera == null) return;

            // Get top-left corner in world space
            Vector3 topLeft = _camera.ViewportToWorldPoint(new Vector3(0, 1, _camera.nearClipPlane));
            topLeft.z = 0;
            Vector3 startPos = topLeft + new Vector3(screenOffset.x, -screenOffset.y, 0);

            // Position heart
            if (_heart != null)
            {
                _heart.transform.position = startPos;
                _heart.transform.localScale = Vector3.one * heartSize;
            }
            
            // Position text to the right of heart
            if (_hpText != null)
            {
                _hpText.transform.position = startPos + new Vector3(textOffset, 0, 0);
                // Ensure scale stays consistent even if parent scales
                _hpText.transform.localScale = Vector3.one * 0.02f;
            }
        }
    }
}
