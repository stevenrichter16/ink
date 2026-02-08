using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Optional component on NpcAI that enables trading.
    /// Maintains runtime stock that can be modified during gameplay.
    /// </summary>
    public class Merchant : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Merchant profile ID from MerchantDatabase")]
        public string profileId;
        
        [Header("Runtime")]
        [SerializeField] private List<MerchantStockEntry> _runtimeStock = new List<MerchantStockEntry>();
        
        private MerchantProfile _profile;
        private bool _stockInitialized;
        
        /// <summary>
        /// The merchant's profile (cached).
        /// </summary>
        public MerchantProfile Profile
        {
            get
            {
                if (_profile == null && !string.IsNullOrEmpty(profileId))
                    _profile = MerchantDatabase.Get(profileId);
                return _profile;
            }
        }
        
        /// <summary>
        /// Display name for UI.
        /// </summary>
        public string DisplayName => Profile?.displayName ?? "Merchant";
        
        /// <summary>
        /// Current stock (mutable during gameplay).
        /// </summary>
        public List<MerchantStockEntry> Stock => _runtimeStock;
        
        /// <summary>
        /// Initialize or restock from profile.
        /// </summary>
        public void InitializeStock(bool forceRestock = false)
        {
            if (Profile == null)
            {
                Debug.LogWarning($"[Merchant] No profile found for '{profileId}'");
                return;
            }
            
            if (!_stockInitialized || forceRestock)
            {
                _runtimeStock.Clear();

                // Scale stock quantities by district prosperity
                float prosperity = 1f;
                var dcs = DistrictControlService.Instance;
                if (dcs != null)
                {
                    var pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
                    var state = dcs.GetStateByPosition(pos.x, pos.y);
                    if (state != null)
                        prosperity = state.prosperity;
                }

                foreach (var entry in Profile.stock)
                {
                    var clone = entry.Clone();
                    clone.quantity = MerchantStockScaler.ScaleQuantity(clone.quantity, prosperity);
                    _runtimeStock.Add(clone);
                }
                _stockInitialized = true;
            }
        }
        
        /// <summary>
        /// Called when player opens the shop.
        /// </summary>
        public void OnShopOpened()
        {
            if (Profile != null && Profile.restockEachVisit)
            {
                InitializeStock(forceRestock: true);
            }
            else
            {
                InitializeStock(forceRestock: false);
            }
        }
        
        /// <summary>
        /// Get stock entry for an item (or null if not in stock).
        /// </summary>
        public MerchantStockEntry GetStockEntry(string itemId)
        {
            return _runtimeStock.Find(e => e.itemId == itemId);
        }
        
        /// <summary>
        /// Check if merchant has item in stock.
        /// </summary>
        public bool HasInStock(string itemId, int quantity = 1)
        {
            var entry = GetStockEntry(itemId);
            return entry != null && entry.quantity >= quantity;
        }
        
        /// <summary>
        /// Remove quantity from stock (after sale to player).
        /// </summary>
        public void RemoveFromStock(string itemId, int quantity)
        {
            var entry = GetStockEntry(itemId);
            if (entry != null)
            {
                entry.quantity -= quantity;
                if (entry.quantity <= 0)
                    _runtimeStock.Remove(entry);
            }
        }
        
        /// <summary>
        /// Add quantity to stock (when player sells to merchant).
        /// </summary>
        public void AddToStock(string itemId, int quantity)
        {
            var entry = GetStockEntry(itemId);
            if (entry != null)
            {
                entry.quantity += quantity;
            }
            else
            {
                _runtimeStock.Add(new MerchantStockEntry(itemId, quantity));
            }
        }
        
        /// <summary>
        /// Get buy price (what player pays).
        /// </summary>
        public int GetBuyPrice(string itemId)
        {
            var pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            return Profile?.GetBuyPrice(itemId, pos) ?? 0;
        }
        
        /// <summary>
        /// Get sell price (what player receives).
        /// </summary>
        public int GetSellPrice(string itemId)
        {
            var pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            return Profile?.GetSellPrice(itemId, pos) ?? 0;
        }
    }
}
