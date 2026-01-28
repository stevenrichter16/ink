namespace InkSim
{
    /// <summary>
    /// Template definition for an item type.
    /// Immutable data shared by all instances of the same item.
    /// </summary>
    [System.Serializable]
    public class ItemData
    {
        public string id;           // Unique identifier: "sword_iron", "potion_health"
        public string name;         // Display name: "Iron Sword"
        public ItemType type;       // Category
        public int tileIndex;       // Sprite index from tileset
        
        // Equipment stats (only set for equippable items)
        public int attackBonus;
        public int defenseBonus;
        public int healthBonus;     // Max HP increase when equipped
        public int speedBonus;      // Dodge/move related bonus when equipped
        
        // Consumable stats
        public int healAmount;      // HP restored when used
        
        // Stacking
        public bool stackable;
        public int maxStack;
        
        // Value (for selling, scoring)
        public int value;

        public ItemData() { }

        public ItemData(string id, string name, ItemType type, int tileIndex)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.tileIndex = tileIndex;
            this.stackable = false;
            this.maxStack = 1;
        }

        /// <summary>
        /// Can this item be equipped?
        /// </summary>
        public bool IsEquippable => type == ItemType.Weapon || 
                                    type == ItemType.Armor || 
                                    type == ItemType.Accessory;

        /// <summary>
        /// Can this item be used/consumed?
        /// </summary>
        public bool IsUsable => type == ItemType.Consumable;
    }
}
