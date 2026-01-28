using UnityEngine;
using System;

namespace InkSim
{
    /// <summary>
    /// Equipment slots for gear. Attach to Player.
    /// </summary>
    public class Equipment : MonoBehaviour
    {
        [Header("Equipment Slots")]
        public ItemInstance weapon;
        public ItemInstance armor;
        public ItemInstance accessory;

        // Events
        public event Action OnChanged;
        public event Action<ItemInstance, ItemType> OnEquipped;
        public event Action<ItemInstance, ItemType> OnUnequipped;

        /// <summary>
        /// Total attack bonus from all equipped items.
        /// </summary>
        public int TotalAttackBonus
        {
            get
            {
                int total = 0;
                if (weapon?.data != null) total += weapon.data.attackBonus;
                if (armor?.data != null) total += armor.data.attackBonus;
                if (accessory?.data != null) total += accessory.data.attackBonus;
                return total;
            }
        }

        /// <summary>
        /// Total defense bonus from all equipped items.
        /// </summary>
        public int TotalDefenseBonus
        {
            get
            {
                int total = 0;
                if (weapon?.data != null) total += weapon.data.defenseBonus;
                if (armor?.data != null) total += armor.data.defenseBonus;
                if (accessory?.data != null) total += accessory.data.defenseBonus;
                return total;
            }
        }

        /// <summary>
        /// Total max health bonus from all equipped items.
        /// </summary>
        public int TotalHealthBonus
        {
            get
            {
                int total = 0;
                if (weapon?.data != null) total += weapon.data.healthBonus;
                if (armor?.data != null) total += armor.data.healthBonus;
                if (accessory?.data != null) total += accessory.data.healthBonus;
                return total;
            }
        }

        /// <summary>
        /// Total speed bonus from all equipped items.
        /// </summary>
        public int TotalSpeedBonus
        {
            get
            {
                int total = 0;
                if (weapon?.data != null) total += weapon.data.speedBonus;
                if (armor?.data != null) total += armor.data.speedBonus;
                if (accessory?.data != null) total += accessory.data.speedBonus;
                return total;
            }
        }

        /// <summary>
        /// Equip an item from inventory.
        /// Returns the previously equipped item (or null).
        /// </summary>
public ItemInstance Equip(ItemInstance item, Inventory inventory)
        {
            if (item == null || item.data == null) return null;
            if (!item.data.IsEquippable) return null;

            Debug.Log($"[Equipment] Attempting to equip {item.Name}, type={item.data.type}, defenseBonus={item.data.defenseBonus}");

            ItemInstance oldItem = null;

            // Remove from inventory
            inventory.RemoveItem(item);

            // Swap into appropriate slot
            switch (item.data.type)
            {
                case ItemType.Weapon:
                    oldItem = weapon;
                    weapon = item;
                    break;

                case ItemType.Armor:
                    oldItem = armor;
                    armor = item;
                    Debug.Log($"[Equipment] Armor slot now contains: {armor.Name} with defense {armor.data.defenseBonus}");
                    break;

                case ItemType.Accessory:
                    oldItem = accessory;
                    accessory = item;
                    break;
            }

            // Put old item back in inventory
            if (oldItem != null)
            {
                inventory.AddItem(oldItem);
                OnUnequipped?.Invoke(oldItem, oldItem.data.type);
            }

            OnEquipped?.Invoke(item, item.data.type);
            OnChanged?.Invoke();

            Debug.Log($"[Equipment] Equipped {item.Name}. TotalDefenseBonus is now {TotalDefenseBonus}");
            return oldItem;
        }

        /// <summary>
        /// Unequip item from a slot and put in inventory.
        /// </summary>
        public bool Unequip(ItemType slot, Inventory inventory)
        {
            ItemInstance item = GetSlot(slot);
            if (item == null) return false;

            if (inventory.IsFull)
            {
                Debug.Log("[Equipment] Inventory full, cannot unequip.");
                return false;
            }

            // Clear slot
            switch (slot)
            {
                case ItemType.Weapon: weapon = null; break;
                case ItemType.Armor: armor = null; break;
                case ItemType.Accessory: accessory = null; break;
            }

            // Add to inventory
            inventory.AddItem(item);
            OnUnequipped?.Invoke(item, slot);
            OnChanged?.Invoke();

            Debug.Log($"[Equipment] Unequipped {item.Name}");
            return true;
        }

        /// <summary>
        /// Get item in a slot.
        /// </summary>
        public ItemInstance GetSlot(ItemType slot)
        {
            return slot switch
            {
                ItemType.Weapon => weapon,
                ItemType.Armor => armor,
                ItemType.Accessory => accessory,
                _ => null
            };
        }

        /// <summary>
        /// Check if a slot is empty.
        /// </summary>
        public bool IsSlotEmpty(ItemType slot)
        {
            return GetSlot(slot) == null;
        }

        /// <summary>
        /// Unequip all items into inventory.
        /// </summary>
        public void UnequipAll(Inventory inventory)
        {
            Unequip(ItemType.Weapon, inventory);
            Unequip(ItemType.Armor, inventory);
            Unequip(ItemType.Accessory, inventory);
        }
    }
}
