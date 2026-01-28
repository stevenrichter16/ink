using UnityEngine;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Shows equipped items as visual overlays on the player.
    /// Attach to Player GameObject.
    /// </summary>
    public class EquipmentVisual : MonoBehaviour
    {
        [Header("References")]
        public Equipment equipment;
        public SpriteLibrary spriteLibrary;

        [Header("Overlay Settings")]
        public Vector3 weaponOffset = new Vector3(0.15f, 0f, 0f);
        public Vector3 armorOffset = new Vector3(0f, 0f, 0f);
        public Vector3 accessoryOffset = new Vector3(-0.1f, 0.1f, 0f);
        public float overlayScale = 0.8f;

        [Header("Armor Tint Colors")]
        public Color leatherColor = new Color(0.6f, 0.4f, 0.2f, 1f);   // Brown
        public Color ironColor = new Color(0.7f, 0.7f, 0.75f, 1f);     // Silver
        public Color steelColor = new Color(0.5f, 0.6f, 0.9f, 1f);     // Blue-steel

        private SpriteRenderer _weaponOverlay;
        private SpriteRenderer _armorOverlay;
        private SpriteRenderer _accessoryOverlay;
        private Dictionary<string, Color> _armorColors;

        private void Start()
        {
            if (equipment == null)
                equipment = GetComponent<Equipment>();

            if (spriteLibrary == null)
                spriteLibrary = FindObjectOfType<SpriteLibrary>();

            // Setup armor color mapping
            _armorColors = new Dictionary<string, Color>
            {
                { "leather_armor", leatherColor },
                { "iron_armor", ironColor },
                { "steel_armor", steelColor }
            };

            // Create overlay sprites
            _weaponOverlay = CreateOverlay("WeaponOverlay", weaponOffset, 11);
            _armorOverlay = CreateOverlay("ArmorOverlay", armorOffset, 9);
            _accessoryOverlay = CreateOverlay("AccessoryOverlay", accessoryOffset, 12);

            // Subscribe to equipment changes
            if (equipment != null)
            {
                equipment.OnChanged += RefreshVisuals;
                RefreshVisuals();
            }
        }

        private SpriteRenderer CreateOverlay(string name, Vector3 offset, int sortOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = offset;
            go.transform.localScale = Vector3.one * overlayScale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortOrder;
            sr.enabled = false;

            return sr;
        }

        private void RefreshVisuals()
        {
            // Update weapon overlay
            UpdateOverlay(_weaponOverlay, equipment?.weapon, weaponOffset);

            // Update armor overlay with tint
            UpdateArmorOverlay();

            // Update accessory overlay
            UpdateOverlay(_accessoryOverlay, equipment?.accessory, accessoryOffset);
        }

        private void UpdateOverlay(SpriteRenderer overlay, ItemInstance item, Vector3 offset)
        {
            if (overlay == null) return;

            if (item == null || spriteLibrary == null)
            {
                overlay.enabled = false;
                return;
            }

            Sprite sprite = spriteLibrary.GetSprite(item.data.tileIndex);
            if (sprite != null)
            {
                overlay.sprite = sprite;
                overlay.color = Color.white;
                overlay.enabled = true;
            }
            else
            {
                overlay.enabled = false;
            }
        }

        private void UpdateArmorOverlay()
        {
            if (_armorOverlay == null) return;

            ItemInstance armor = equipment?.armor;
            if (armor == null || spriteLibrary == null)
            {
                _armorOverlay.enabled = false;
                return;
            }

            Sprite sprite = spriteLibrary.GetSprite(armor.data.tileIndex);
            if (sprite != null)
            {
                _armorOverlay.sprite = sprite;

                // Apply tier-based color tint
                if (_armorColors.TryGetValue(armor.data.id, out Color tint))
                    _armorOverlay.color = tint;
                else
                    _armorOverlay.color = Color.white;

                _armorOverlay.enabled = true;
            }
            else
            {
                _armorOverlay.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (equipment != null)
                equipment.OnChanged -= RefreshVisuals;
        }
    }

    /// <summary>
    /// Simple sprite library for accessing tiles by index.
    /// Attach to a GameObject and populate via TestMapBuilder.
    /// </summary>
    public class SpriteLibrary : MonoBehaviour
    {
        public static SpriteLibrary Instance { get; private set; }
        
        private Sprite[] _sprites;

        private void Awake()
        {
            Instance = this;
        }

        public void SetSprites(Sprite[] sprites)
        {
            _sprites = sprites;
        }

        public Sprite GetSprite(int index)
        {
            if (_sprites == null || index < 0 || index >= _sprites.Length)
                return null;
            return _sprites[index];
        }
    }
}
