using UnityEngine;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// World object representing a pickupable item.
    /// Player walks over it to collect.
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        // Static registry for efficient lookup (avoids FindObjectsOfType every frame)
        private static readonly List<ItemPickup> _activePickups = new List<ItemPickup>();
        public static IReadOnlyList<ItemPickup> ActivePickups => _activePickups;

        [Header("Item")]
        public string itemId;
        public int quantity = 1;

        [Header("Grid Position")]
        public int gridX;
        public int gridY;

        private SpriteRenderer _spriteRenderer;

        private void OnEnable()
        {
            if (!_activePickups.Contains(this))
                _activePickups.Add(this);
        }

        private void OnDisable()
        {
            _activePickups.Remove(this);
        }

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Auto-set sprite from item database if not already set
            if (_spriteRenderer != null && _spriteRenderer.sprite == null)
            {
                var data = ItemDatabase.Get(itemId);
                if (data != null)
                {
                    // Would need access to tile sprites - handled by TestMapBuilder instead
                }
            }
        }

        /// <summary>
        /// Called when player walks onto this pickup.
        /// </summary>
        public void Pickup(PlayerController player)
        {
            if (player == null || player.inventory == null) return;

            var data = ItemDatabase.Get(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[ItemPickup] Unknown item: {itemId}");
                return;
            }

            if (player.inventory.AddItem(itemId, quantity))
            {
                Debug.Log($"[ItemPickup] Picked up {quantity}x {data.name}");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("[ItemPickup] Inventory full!");
            }
        }

        /// <summary>
        /// Create a pickup at a position.
        /// </summary>
        public static ItemPickup Create(string itemId, int gridX, int gridY, int quantity, Sprite sprite, Transform parent, float tileSize)
        {
            var data = ItemDatabase.Get(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[ItemPickup] Cannot create pickup for unknown item: {itemId}");
                return null;
            }

            GameObject go = new GameObject($"Pickup_{itemId}_{gridX}_{gridY}");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(gridX * tileSize, gridY * tileSize, 0);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            ItemPickup pickup = go.AddComponent<ItemPickup>();
            pickup.itemId = itemId;
            pickup.quantity = quantity;
            pickup.gridX = gridX;
            pickup.gridY = gridY;

            return pickup;
        }

        /// <summary>
        /// Create a pickup using SpriteLibrary (simpler API for runtime spawning).
        /// </summary>
        public static ItemPickup CreateFromLoot(string itemId, int gridX, int gridY, int quantity, float tileSize)
        {
            var data = ItemDatabase.Get(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[ItemPickup] Cannot create pickup for unknown item: {itemId}");
                return null;
            }

            Sprite sprite = SpriteLibrary.Instance?.GetSprite(data.tileIndex);
            if (sprite == null)
            {
                Debug.LogWarning($"[ItemPickup] No sprite for item: {itemId} (tile {data.tileIndex})");
                return null;
            }

            GameObject go = new GameObject($"Loot_{itemId}_{gridX}_{gridY}");
            go.transform.localPosition = new Vector3(gridX * tileSize, gridY * tileSize, 0);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            ItemPickup pickup = go.AddComponent<ItemPickup>();
            pickup.itemId = itemId;
            pickup.quantity = quantity;
            pickup.gridX = gridX;
            pickup.gridY = gridY;

            return pickup;
        }
    }
}
