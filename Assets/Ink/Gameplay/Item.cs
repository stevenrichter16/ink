using UnityEngine;
using System;

namespace InkSim
{
    /// <summary>
    /// Base class for pickupable items.
    /// </summary>
    public class Item : MonoBehaviour
    {
        [Header("Item Settings")]
        public ItemType itemType = ItemType.None;
        public int value = 1;

        [Header("Grid Position")]
        public int gridX;
        public int gridY;

        public enum ItemType
        {
            None,
            Potion,
            Coin,
            Key,
            Gem,
            Weapon
        }

        public static event Action<Item, PlayerController> OnItemPickedUp;

        private void Start()
        {
            // Register with GridWorld
            if (GridWorld.Instance != null)
            {
                GridWorld.Instance.SetItem(gridX, gridY, this);
            }
        }

        public virtual void Pickup(PlayerController player)
        {
            OnItemPickedUp?.Invoke(this, player);
            
            switch (itemType)
            {
                case ItemType.Potion:
                    player.Heal(value);
                    Debug.Log($"[Item] Picked up potion! Healed {value} HP.");
                    break;

                case ItemType.Coin:
                    player.AddCoins(value);
                    Debug.Log($"[Item] Picked up {value} coin(s)!");
                    break;

                case ItemType.Key:
                    player.AddKeys(value);
                    Debug.Log($"[Item] Picked up key!");
                    break;

                case ItemType.Gem:
                    player.AddCoins(value * 10);
                    Debug.Log($"[Item] Picked up gem worth {value * 10}!");
                    break;

                case ItemType.Weapon:
                    player.UpgradeAttack(value);
                    Debug.Log($"[Item] Attack upgraded by {value}!");
                    break;
            }

            // Clear from grid and destroy
            if (GridWorld.Instance != null)
                GridWorld.Instance.ClearItem(gridX, gridY);
                
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (GridWorld.Instance != null)
                GridWorld.Instance.ClearItem(gridX, gridY);
        }
    }
}
