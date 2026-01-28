namespace InkSim
{
    /// <summary>
    /// A runtime instance of an item.
    /// Combines immutable ItemData with mutable state (quantity).
    /// </summary>
    [System.Serializable]
    public class ItemInstance
    {
        public ItemData data;
        public int quantity;

        public ItemInstance(ItemData data, int quantity = 1)
        {
            this.data = data;
            this.quantity = quantity;
        }

        /// <summary>
        /// Shortcut to item ID.
        /// </summary>
        public string Id => data?.id;

        /// <summary>
        /// Shortcut to item name.
        /// </summary>
        public string Name => data?.name ?? "Unknown";

        /// <summary>
        /// Shortcut to item type.
        /// </summary>
        public ItemType Type => data?.type ?? ItemType.Consumable;

        /// <summary>
        /// Can more be added to this stack?
        /// </summary>
        public bool CanStack => data != null && data.stackable && quantity < data.maxStack;

        /// <summary>
        /// How many more can fit in this stack?
        /// </summary>
        public int StackSpace => data != null && data.stackable ? data.maxStack - quantity : 0;

        /// <summary>
        /// Add to stack, returns overflow amount.
        /// </summary>
        public int AddToStack(int amount)
        {
            if (data == null || !data.stackable)
                return amount;

            int canAdd = data.maxStack - quantity;
            int toAdd = System.Math.Min(amount, canAdd);
            quantity += toAdd;
            return amount - toAdd;
        }

        /// <summary>
        /// Remove from stack, returns actual amount removed.
        /// </summary>
        public int RemoveFromStack(int amount)
        {
            int toRemove = System.Math.Min(amount, quantity);
            quantity -= toRemove;
            return toRemove;
        }

        public override string ToString()
        {
            return data.stackable ? $"{Name} x{quantity}" : Name;
        }
    }
}
