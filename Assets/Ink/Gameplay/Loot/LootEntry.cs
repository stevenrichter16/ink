namespace InkSim
{
    /// <summary>
    /// Defines a single possible drop from a loot table.
    /// </summary>
    [System.Serializable]
    public class LootEntry
    {
        public string itemId;       // Item to drop
        public float dropChance;    // 0.0 - 1.0 (0.5 = 50%)
        public int minQuantity;     // Minimum if dropped
        public int maxQuantity;     // Maximum if dropped

        public LootEntry(string itemId, float dropChance, int minQty = 1, int maxQty = 1)
        {
            this.itemId = itemId;
            this.dropChance = dropChance;
            this.minQuantity = minQty;
            this.maxQuantity = maxQty;
        }
    }
}
