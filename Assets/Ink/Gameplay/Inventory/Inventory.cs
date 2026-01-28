using UnityEngine;
using System;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Bag storage for items. Attach to Player.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Header("Settings")]
        public int maxSlots = 20;

        [Header("State")]
        public List<ItemInstance> items = new List<ItemInstance>();

        // Events
        public event Action OnChanged;
        public event Action<ItemInstance> OnItemAdded;
        public event Action<ItemInstance> OnItemRemoved;

        /// <summary>
        /// Current number of slots used.
        /// </summary>
        public int SlotCount => items.Count;

        /// <summary>
        /// Is inventory full?
        /// </summary>
        public bool IsFull => items.Count >= maxSlots;

        /// <summary>
        /// Add an item by ID.
        /// </summary>
        public bool AddItem(string itemId, int quantity = 1)
        {
            var data = ItemDatabase.Get(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[Inventory] Unknown item: {itemId}");
                return false;
            }

            return AddItem(data, quantity);
        }

        /// <summary>
        /// Add an item by data.
        /// </summary>
        public bool AddItem(ItemData data, int quantity = 1)
        {
            if (data == null) return false;

            int remaining = quantity;

            // Try to stack with existing items first
            if (data.stackable)
            {
                foreach (var item in items)
                {
                    if (item.data.id == data.id && item.CanStack)
                    {
                        remaining = item.AddToStack(remaining);
                        if (remaining <= 0) break;
                    }
                }
            }

            // Create new stacks for remainder
            while (remaining > 0)
            {
                if (IsFull)
                {
                    Debug.Log("[Inventory] Full! Could not add all items.");
                    OnChanged?.Invoke();
                    return quantity != remaining; // Partial success
                }

                int stackSize = data.stackable ? Mathf.Min(remaining, data.maxStack) : 1;
                var newItem = new ItemInstance(data, stackSize);
                items.Add(newItem);
                OnItemAdded?.Invoke(newItem);
                remaining -= stackSize;
            }

            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Add an existing item instance.
        /// </summary>
        public bool AddItem(ItemInstance item)
        {
            if (item == null) return false;
            return AddItem(item.data, item.quantity);
        }

        /// <summary>
        /// Remove quantity of item by ID.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            int remaining = quantity;

            for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (items[i].data.id == itemId)
                {
                    int removed = items[i].RemoveFromStack(remaining);
                    remaining -= removed;

                    if (items[i].quantity <= 0)
                    {
                        var removedItem = items[i];
                        items.RemoveAt(i);
                        OnItemRemoved?.Invoke(removedItem);
                    }
                }
            }

            if (remaining < quantity)
            {
                OnChanged?.Invoke();
                return remaining == 0;
            }

            return false;
        }

        /// <summary>
        /// Remove a specific item instance.
        /// </summary>
        public bool RemoveItem(ItemInstance item)
        {
            if (items.Remove(item))
            {
                OnItemRemoved?.Invoke(item);
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Count total quantity of an item.
        /// </summary>
        public int CountItem(string itemId)
        {
            int total = 0;
            foreach (var item in items)
            {
                if (item.data.id == itemId)
                    total += item.quantity;
            }
            return total;
        }

        /// <summary>
        /// Check if have enough of an item.
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return CountItem(itemId) >= quantity;
        }

        /// <summary>
        /// Get first item instance by ID.
        /// </summary>
        public ItemInstance GetItem(string itemId)
        {
            return items.Find(i => i.data.id == itemId);
        }

        /// <summary>
        /// Get item at slot index.
        /// </summary>
        public ItemInstance GetItemAt(int index)
        {
            if (index >= 0 && index < items.Count)
                return items[index];
            return null;
        }

        /// <summary>
        /// Clear all items.
        /// </summary>
        public void Clear()
        {
            items.Clear();
            OnChanged?.Invoke();
        }
    }
}
