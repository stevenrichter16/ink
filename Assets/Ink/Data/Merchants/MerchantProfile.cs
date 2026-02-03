using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Defines a merchant's shop configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMerchant", menuName = "InkSim/Merchant Profile")]
    public class MerchantProfile : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName = "Merchant";
        [Tooltip("Faction this merchant belongs to (for reputation-based pricing)")]
        public string factionId;
        
        [Header("Pricing")]
        [Tooltip("Multiplier for buy prices (1.0 = item value, 1.5 = 50% markup)")]
        public float buyMultiplier = 1.0f;
        
        [Tooltip("Multiplier for sell prices (0.5 = half value)")]
        public float sellMultiplier = 0.5f;
        
        [Header("Stock")]
        [Tooltip("If true, stock resets to initial quantities each time shop opens")]
        public bool restockEachVisit = true;
        
        public List<MerchantStockEntry> stock = new List<MerchantStockEntry>();
        
        /// <summary>
        /// Calculate buy price for an item (what player pays).
        /// </summary>
        public int GetBuyPrice(string itemId, Vector2Int? position = null)
        {
            return EconomicPriceResolver.ResolveBuyPrice(itemId, this, position);
        }
        
        /// <summary>
        /// Calculate sell price for an item (what player receives).
        /// </summary>
        public int GetSellPrice(string itemId, Vector2Int? position = null)
        {
            return EconomicPriceResolver.ResolveSellPrice(itemId, this, position);
        }
    }
}
