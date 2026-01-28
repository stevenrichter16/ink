using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Displays equipped items as overlay sprites on the player.
    /// </summary>
    public class EquipmentVisuals : MonoBehaviour
    {
        [Header("References")]
        public Equipment equipment;
        public SpriteRenderer weaponOverlay;
        public SpriteRenderer armorOverlay;
        public SpriteRenderer accessoryOverlay;

        [Header("Sprite Source")]
        public Sprite[] tileSprites; // All tile sprites for lookup

        [Header("Settings")]
        public Vector3 weaponOffset = new Vector3(0.15f, 0f, 0f);
        public Vector3 armorOffset = new Vector3(0f, 0f, 0f);
        public Vector3 accessoryOffset = new Vector3(-0.1f, 0.1f, 0f);
        public float overlayScale = 0.8f;

        private void Start()
        {
            if (equipment == null)
                equipment = GetComponent<Equipment>();

            // Create overlay objects if not assigned
            if (weaponOverlay == null)
                weaponOverlay = CreateOverlay("WeaponOverlay", 12);
            if (armorOverlay == null)
                armorOverlay = CreateOverlay("ArmorOverlay", 11);
            if (accessoryOverlay == null)
                accessoryOverlay = CreateOverlay("AccessoryOverlay", 13);

            // Subscribe to equipment changes
            if (equipment != null)
                equipment.OnChanged += RefreshVisuals;

            RefreshVisuals();
        }

        private SpriteRenderer CreateOverlay(string name, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * overlayScale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            sr.enabled = false;

            return sr;
        }

        private void RefreshVisuals()
        {
            UpdateOverlay(weaponOverlay, equipment?.weapon, weaponOffset);
            UpdateOverlay(armorOverlay, equipment?.armor, armorOffset);
            UpdateOverlay(accessoryOverlay, equipment?.accessory, accessoryOffset);
        }

        private void UpdateOverlay(SpriteRenderer overlay, ItemInstance item, Vector3 offset)
        {
            if (overlay == null) return;

            if (item != null && item.data != null && tileSprites != null)
            {
                int tileIndex = item.data.tileIndex;
                if (tileIndex >= 0 && tileIndex < tileSprites.Length)
                {
                    overlay.sprite = tileSprites[tileIndex];
                    overlay.enabled = true;
                    overlay.transform.localPosition = offset;
                    return;
                }
            }

            // No item or invalid - hide overlay
            overlay.enabled = false;
        }

        /// <summary>
        /// Set the tile sprites array (called by TestMapBuilder).
        /// </summary>
        public void SetTileSprites(Sprite[] sprites)
        {
            tileSprites = sprites;
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (equipment != null)
                equipment.OnChanged -= RefreshVisuals;
        }
    }
}
