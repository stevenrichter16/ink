using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Bottom-center spell bar showing equipped spells with cooldown overlays,
    /// ink costs, and hotkey labels. Auto-finds SpellSystem.
    /// </summary>
    public class SpellBarUI : MonoBehaviour
    {
        [Header("Style")]
        public Color slotBackground = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        public Color slotBorder = new Color(0.0f, 0.6f, 0.6f, 0.8f);
        public Color cooldownOverlay = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        public Color readyColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        public Color cooldownTextColor = new Color(1f, 0.7f, 0.3f, 1f);
        public Color inkCostColor = new Color(0.3f, 0.6f, 1f, 1f);
        public Color insufficientInkColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        private const float SlotSize = 56f;
        private const float SlotSpacing = 6f;
        private const float BottomPadding = 60f; // Above XP bar

        private SpellSystem _spellSystem;
        private PlayerController _player;
        private GameObject _canvas;
        private readonly List<SpellSlot> _slots = new List<SpellSlot>();
        private Font _font;

        private struct SpellSlot
        {
            public GameObject root;
            public Image background;
            public Image cooldownFill;
            public Text nameText;
            public Text hotkeyText;
            public Text cooldownText;
            public Text inkCostText;
        }

        private void Start()
        {
            _spellSystem = FindFirstObjectByType<SpellSystem>();
            _player = FindFirstObjectByType<PlayerController>();

            if (_spellSystem == null)
            {
                Debug.LogWarning("[SpellBarUI] No SpellSystem found.");
                enabled = false;
                return;
            }

            _font = Font.CreateDynamicFontFromOSFont("Courier New", 14);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateUI();
        }

        private void CreateUI()
        {
            _canvas = new GameObject("SpellBarCanvas");
            _canvas.transform.SetParent(transform);

            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95; // Above XP (90), below HealthUI (100)

            CanvasScaler scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _canvas.AddComponent<GraphicRaycaster>();

            RebuildSlots();
        }

        private void RebuildSlots()
        {
            // Clear existing slots
            foreach (var slot in _slots)
            {
                if (slot.root != null)
                    Destroy(slot.root);
            }
            _slots.Clear();

            int count = _spellSystem.equippedSpells.Count;
            if (count == 0) return;

            float totalWidth = count * SlotSize + (count - 1) * SlotSpacing;
            float startX = -totalWidth / 2f + SlotSize / 2f;

            for (int i = 0; i < count; i++)
            {
                var slot = CreateSlot(i, startX + i * (SlotSize + SlotSpacing));
                _slots.Add(slot);
            }
        }

        private SpellSlot CreateSlot(int index, float xPos)
        {
            var spell = _spellSystem.equippedSpells[index];
            SpellSlot slot = new SpellSlot();

            // Slot root
            slot.root = new GameObject($"SpellSlot_{index}");
            slot.root.transform.SetParent(_canvas.transform, false);

            RectTransform rootRect = slot.root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0);
            rootRect.anchorMax = new Vector2(0.5f, 0);
            rootRect.pivot = new Vector2(0.5f, 0);
            rootRect.anchoredPosition = new Vector2(xPos, BottomPadding);
            rootRect.sizeDelta = new Vector2(SlotSize, SlotSize);

            // Background
            slot.background = slot.root.AddComponent<Image>();
            slot.background.color = slotBackground;

            // Border (outline)
            Outline outline = slot.root.AddComponent<Outline>();
            outline.effectColor = slotBorder;
            outline.effectDistance = new Vector2(2, -2);

            // Spell name (short, centered)
            GameObject nameGO = CreateTextChild(slot.root, "Name");
            slot.nameText = nameGO.GetComponent<Text>();
            slot.nameText.text = TruncateName(spell.spellName, 6);
            slot.nameText.fontSize = 11;
            slot.nameText.color = readyColor;
            slot.nameText.alignment = TextAnchor.MiddleCenter;
            RectTransform nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(2, 14);
            nameRect.offsetMax = new Vector2(-2, -2);

            // Cooldown overlay (fills from bottom up)
            GameObject cdFillGO = new GameObject("CooldownFill");
            cdFillGO.transform.SetParent(slot.root.transform, false);
            slot.cooldownFill = cdFillGO.AddComponent<Image>();
            slot.cooldownFill.color = cooldownOverlay;
            slot.cooldownFill.raycastTarget = false;
            RectTransform cdRect = cdFillGO.GetComponent<RectTransform>();
            cdRect.anchorMin = new Vector2(0, 0);
            cdRect.anchorMax = new Vector2(1, 0); // Will scale up
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;

            // Cooldown timer text (center)
            GameObject cdTextGO = CreateTextChild(slot.root, "CooldownText");
            slot.cooldownText = cdTextGO.GetComponent<Text>();
            slot.cooldownText.text = "";
            slot.cooldownText.fontSize = 16;
            slot.cooldownText.fontStyle = FontStyle.Bold;
            slot.cooldownText.color = cooldownTextColor;
            slot.cooldownText.alignment = TextAnchor.MiddleCenter;
            RectTransform cdTextRect = cdTextGO.GetComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.offsetMin = Vector2.zero;
            cdTextRect.offsetMax = Vector2.zero;

            // Hotkey label (top-left corner)
            GameObject hkGO = CreateTextChild(slot.root, "Hotkey");
            slot.hotkeyText = hkGO.GetComponent<Text>();
            slot.hotkeyText.text = $"{index + 1}";
            slot.hotkeyText.fontSize = 10;
            slot.hotkeyText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            slot.hotkeyText.alignment = TextAnchor.UpperLeft;
            RectTransform hkRect = hkGO.GetComponent<RectTransform>();
            hkRect.anchorMin = Vector2.zero;
            hkRect.anchorMax = Vector2.one;
            hkRect.offsetMin = new Vector2(3, 0);
            hkRect.offsetMax = new Vector2(0, -2);

            // Ink cost (bottom center)
            GameObject inkGO = CreateTextChild(slot.root, "InkCost");
            slot.inkCostText = inkGO.GetComponent<Text>();
            slot.inkCostText.text = spell.manaCost > 0 ? $"{spell.manaCost}" : "";
            slot.inkCostText.fontSize = 10;
            slot.inkCostText.color = inkCostColor;
            slot.inkCostText.alignment = TextAnchor.LowerCenter;
            RectTransform inkRect = inkGO.GetComponent<RectTransform>();
            inkRect.anchorMin = Vector2.zero;
            inkRect.anchorMax = Vector2.one;
            inkRect.offsetMin = new Vector2(0, 2);
            inkRect.offsetMax = new Vector2(0, 0);

            return slot;
        }

        private void Update()
        {
            if (_spellSystem == null) return;

            // Rebuild if spell count changed
            if (_slots.Count != _spellSystem.equippedSpells.Count)
                RebuildSlots();

            for (int i = 0; i < _slots.Count && i < _spellSystem.equippedSpells.Count; i++)
            {
                var slot = _slots[i];
                var spell = _spellSystem.equippedSpells[i];
                float cd = _spellSystem.cooldownTimers[i];
                float maxCd = spell.cooldown;

                // Cooldown fill height (0 = ready, 1 = full cooldown)
                float ratio = maxCd > 0 ? Mathf.Clamp01(cd / maxCd) : 0f;
                RectTransform cdRect = slot.cooldownFill.GetComponent<RectTransform>();
                cdRect.anchorMax = new Vector2(1, ratio);

                // Cooldown text
                if (cd > 0)
                {
                    slot.cooldownText.text = cd < 1f ? $".{(int)(cd * 10)}" : $"{cd:F0}";
                    slot.nameText.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                }
                else
                {
                    slot.cooldownText.text = "";
                    slot.nameText.color = readyColor;
                }

                // Ink cost color — red when can't afford
                if (spell.manaCost > 0 && _player != null)
                {
                    bool canAfford = InkPool.HasInk(_player.currentInk, spell.manaCost);
                    slot.inkCostText.color = canAfford ? inkCostColor : insufficientInkColor;
                }
            }
        }

        private GameObject CreateTextChild(GameObject parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            Text text = go.AddComponent<Text>();
            text.font = _font;
            text.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }

        private static string TruncateName(string name, int maxLen)
        {
            if (string.IsNullOrEmpty(name)) return "???";
            return name.Length <= maxLen ? name : name.Substring(0, maxLen);
        }
    }
}
