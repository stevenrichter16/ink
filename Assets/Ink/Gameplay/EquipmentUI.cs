using UnityEngine;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Corner UI showing equipped items as icons.
    /// </summary>
    public class EquipmentUI : MonoBehaviour
    {
        [Header("References")]
        public PlayerController player;
        public SpriteLibrary spriteLibrary;

        [Header("Layout")]
        public float iconSize = 0.25f;
        public float iconSpacing = 0.3f;
        public Vector2 screenOffset = new Vector2(0.5f, 0.9f); // Bottom-left area

        [Header("Colors")]
        public Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public Color leatherColor = new Color(0.6f, 0.4f, 0.2f, 1f);
        public Color ironColor = new Color(0.7f, 0.7f, 0.75f, 1f);
        public Color steelColor = new Color(0.5f, 0.6f, 0.9f, 1f);

        private List<SpriteRenderer> _slotIcons = new List<SpriteRenderer>();
        private List<SpriteRenderer> _itemIcons = new List<SpriteRenderer>();
        private Camera _camera;
        private string[] _slotLabels = { "W", "A", "R" }; // Weapon, Armor, Ring
        private Dictionary<string, Color> _armorColors;

        private void Start()
        {
            _camera = Camera.main;

            if (player == null)
                player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();

            if (spriteLibrary == null)
                spriteLibrary = SpriteLibrary.Instance;

            _armorColors = new Dictionary<string, Color>
            {
                { "leather_armor", leatherColor },
                { "iron_armor", ironColor },
                { "steel_armor", steelColor }
            };

            CreateSlots();

            // Subscribe to equipment changes
            if (player?.equipment != null)
            {
                player.equipment.OnChanged += RefreshIcons;
                RefreshIcons();
            }
        }

        private void CreateSlots()
        {
            for (int i = 0; i < 3; i++)
            {
                // Background slot
                GameObject slotGO = new GameObject($"EquipSlot_{i}");
                slotGO.transform.SetParent(transform);

                SpriteRenderer slotSR = slotGO.AddComponent<SpriteRenderer>();
                slotSR.sprite = CreateSquareSprite();
                slotSR.color = emptySlotColor;
                slotSR.sortingOrder = 98;

                _slotIcons.Add(slotSR);

                // Item icon (on top)
                GameObject itemGO = new GameObject($"EquipItem_{i}");
                itemGO.transform.SetParent(slotGO.transform);
                itemGO.transform.localPosition = Vector3.zero;

                SpriteRenderer itemSR = itemGO.AddComponent<SpriteRenderer>();
                itemSR.sortingOrder = 99;
                itemSR.enabled = false;

                _itemIcons.Add(itemSR);
            }
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D tex = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    // Border
                    if (x == 0 || x == 15 || y == 0 || y == 15)
                        pixels[y * 16 + x] = Color.white;
                    else
                        pixels[y * 16 + x] = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 16);
        }

        private void LateUpdate()
        {
            PositionSlots();
        }

        private void PositionSlots()
        {
            if (_camera == null) return;

            Vector3 basePos = _camera.ViewportToWorldPoint(new Vector3(screenOffset.x, screenOffset.y, _camera.nearClipPlane));
            basePos.z = 0;

            for (int i = 0; i < _slotIcons.Count; i++)
            {
                Vector3 pos = basePos + new Vector3(i * iconSpacing, 0, 0);
                _slotIcons[i].transform.position = pos;
                _slotIcons[i].transform.localScale = Vector3.one * iconSize;
            }
        }

        private void RefreshIcons()
        {
            if (player?.equipment == null || spriteLibrary == null) return;

            // Weapon slot (index 0)
            UpdateSlotIcon(0, player.equipment.weapon);

            // Armor slot (index 1)
            UpdateArmorSlotIcon(1, player.equipment.armor);

            // Accessory slot (index 2)
            UpdateSlotIcon(2, player.equipment.accessory);
        }

        private void UpdateSlotIcon(int slotIndex, ItemInstance item)
        {
            if (slotIndex >= _itemIcons.Count) return;

            SpriteRenderer itemSR = _itemIcons[slotIndex];

            if (item == null)
            {
                itemSR.enabled = false;
                _slotIcons[slotIndex].color = emptySlotColor;
                return;
            }

            Sprite sprite = spriteLibrary.GetSprite(item.data.tileIndex);
            if (sprite != null)
            {
                itemSR.sprite = sprite;
                itemSR.color = Color.white;
                itemSR.enabled = true;
                _slotIcons[slotIndex].color = new Color(0.2f, 0.4f, 0.3f, 0.7f); // Filled slot color
            }
            else
            {
                itemSR.enabled = false;
            }
        }

        private void UpdateArmorSlotIcon(int slotIndex, ItemInstance armor)
        {
            if (slotIndex >= _itemIcons.Count) return;

            SpriteRenderer itemSR = _itemIcons[slotIndex];

            if (armor == null)
            {
                itemSR.enabled = false;
                _slotIcons[slotIndex].color = emptySlotColor;
                return;
            }

            Sprite sprite = spriteLibrary.GetSprite(armor.data.tileIndex);
            if (sprite != null)
            {
                itemSR.sprite = sprite;

                // Apply tier-based tint
                if (_armorColors.TryGetValue(armor.data.id, out Color tint))
                    itemSR.color = tint;
                else
                    itemSR.color = Color.white;

                itemSR.enabled = true;
                _slotIcons[slotIndex].color = new Color(0.2f, 0.4f, 0.3f, 0.7f);
            }
            else
            {
                itemSR.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (player?.equipment != null)
                player.equipment.OnChanged -= RefreshIcons;
        }
    }
}
