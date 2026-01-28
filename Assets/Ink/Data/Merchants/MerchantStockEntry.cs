using System;

namespace InkSim
{
    /// <summary>
    /// Entry for merchant stock - what items they sell and in what quantity.
    /// </summary>
    [Serializable]
    public class MerchantStockEntry
    {
        public string itemId;
        public int quantity = 1;
        
        public MerchantStockEntry() { }
        
        public MerchantStockEntry(string itemId, int quantity = 1)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
        
        /// <summary>
        /// Clone for runtime stock (so we don't modify the SO).
        /// </summary>
        public MerchantStockEntry Clone()
        {
            return new MerchantStockEntry(itemId, quantity);
        }
    }
}
