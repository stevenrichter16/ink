using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Serializable game state for save/load system.
    /// Increment VERSION when making breaking changes to the format.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int VERSION = 2;
        
        public int version = VERSION;
        public string timestamp;
        public string displayTimestamp; // Human-readable for UI
        
        public PlayerSaveData player;
        public List<EnemySaveData> enemies = new List<EnemySaveData>();
        public List<ItemPickupSaveData> groundItems = new List<ItemPickupSaveData>();
        public List<QuestSaveData> quests = new List<QuestSaveData>();
        
        /// <summary>
        /// Create a new GameState with current timestamp.
        /// </summary>
        public static GameState Create()
        {
            return new GameState
            {
                version = VERSION,
                timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601
                displayTimestamp = DateTime.Now.ToString("MMM dd, HH:mm:ss")
            };
        }
        
        /// <summary>
        /// Check if this save is compatible with current version.
        /// </summary>
        public bool IsCompatible()
        {
            return version == VERSION;
        }
    }
    
    [Serializable]
    public class PlayerSaveData
    {
        // Position
        public int gridX;
        public int gridY;
        
        // Stats
        public int currentHealth;
        public int maxHealth;
        public int baseAttack;
        public int baseDefense;
        public int coins;
        public int keys;
        
        // Leveling
        public int level = 1;
        public int xp = 0;
        
        // Inventory (list of item IDs with quantities)
        public List<InventorySlotData> inventory = new List<InventorySlotData>();
        
        // Equipment (item IDs, empty string if nothing equipped)
        public string equippedWeapon = "";
        public string equippedArmor = "";
        public string equippedAccessory = "";
    }
    
    [Serializable]
    public class InventorySlotData
    {
        public string itemId;
        public int quantity;
        
        public InventorySlotData() { }
        
        public InventorySlotData(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }
    
    [Serializable]
    public class EnemySaveData
    {
        public string enemyId; // Key from EnemyDatabase
        public int gridX;
        public int gridY;
        public int currentHealth;
        public int level = 1;
        public string state; // EnemyState as string for readability
        
        public EnemySaveData() { }
        
        public EnemySaveData(string enemyId, int x, int y, int currentHp, int level, string state)
        {
            this.enemyId = enemyId;
            this.gridX = x;
            this.gridY = y;
            this.currentHealth = currentHp;
            this.level = level;
            this.state = state;
        }
    }

    
    [Serializable]
    public class ItemPickupSaveData
    {
        public string itemId;
        public int gridX;
        public int gridY;
        public int quantity;
        
        public ItemPickupSaveData() { }
        
        public ItemPickupSaveData(string itemId, int x, int y, int quantity)
        {
            this.itemId = itemId;
            this.gridX = x;
            this.gridY = y;
            this.quantity = quantity;
        }
    }

    [Serializable]
    public class QuestSaveData
    {
        public string questId;
        public QuestState state;
        public int currentCount;

        public QuestSaveData() { }

        public QuestSaveData(string questId, QuestState state, int currentCount)
        {
            this.questId = questId;
            this.state = state;
            this.currentCount = currentCount;
        }
    }
}
