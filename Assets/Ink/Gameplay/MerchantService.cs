using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static helper for merchant transactions.
    /// Handles all buy/sell logic, inventory management, and coin transfers.
    /// </summary>
    public static class MerchantService
    {
        /// <summary>
        /// Attempt to buy an item from merchant.
        /// </summary>
        /// <returns>True if successful, false if failed (insufficient funds, no stock, etc.)</returns>
        public static bool TryBuy(Merchant merchant, PlayerController player, string itemId, int quantity = 1)
        {
            if (merchant == null || player == null || string.IsNullOrEmpty(itemId))
                return false;

            var buyPos = new Vector2Int(Mathf.RoundToInt(merchant.transform.position.x), Mathf.RoundToInt(merchant.transform.position.y));
            if (!EconomicPriceResolver.IsTradeAllowed(itemId, merchant.Profile, buyPos))
            {
                Debug.Log($"[MerchantService] Buy blocked by trade restrictions: {itemId}");
                return false;
            }
            
            // Check merchant has stock
            if (!merchant.HasInStock(itemId, quantity))
            {
                Debug.Log($"[MerchantService] Buy failed: {merchant.DisplayName} doesn't have {quantity}x {itemId}");
                return false;
            }
            
            // Calculate cost
            int unitPrice = merchant.GetBuyPrice(itemId);
            int totalCost = unitPrice * quantity;
            
            // Check player has enough coins
            int playerCoins = player.inventory?.CountItem("coin") ?? 0;
            if (playerCoins < totalCost)
            {
                Debug.Log($"[MerchantService] Buy failed: Need {totalCost} coins, have {playerCoins}");
                return false;
            }
            
            // Check player can receive item (stack limits, etc.)
            var itemData = ItemDatabase.Get(itemId);
            if (itemData == null)
            {
                Debug.Log($"[MerchantService] Buy failed: Unknown item '{itemId}'");
                return false;
            }
            
            // Execute transaction
            player.inventory.RemoveItem("coin", totalCost);
            player.inventory.AddItem(itemId, quantity);
            merchant.RemoveFromStock(itemId, quantity);
            
            Debug.Log($"[MerchantService] {player.name} bought {quantity}x {itemId} for {totalCost} coins from {merchant.DisplayName}");
            return true;
        }
        
        /// <summary>
        /// Attempt to sell an item to merchant.
        /// </summary>
        /// <returns>True if successful, false if failed.</returns>
        public static bool TrySell(PlayerController player, Merchant merchant, string itemId, int quantity = 1)
        {
            if (merchant == null || player == null || string.IsNullOrEmpty(itemId))
                return false;

            var sellPos = new Vector2Int(Mathf.RoundToInt(merchant.transform.position.x), Mathf.RoundToInt(merchant.transform.position.y));
            if (!EconomicPriceResolver.IsTradeAllowed(itemId, merchant.Profile, sellPos))
            {
                Debug.Log($"[MerchantService] Sell blocked by trade restrictions: {itemId}");
                return false;
            }
            
            // Don't allow selling coins or keys
            if (itemId == "coin" || itemId == "key")
            {
                Debug.Log($"[MerchantService] Sell failed: Cannot sell {itemId}");
                return false;
            }
            
            // Check player has the item
            int playerQty = player.inventory?.CountItem(itemId) ?? 0;
            if (playerQty < quantity)
            {
                Debug.Log($"[MerchantService] Sell failed: Player doesn't have {quantity}x {itemId}");
                return false;
            }
            
            // Calculate payment
            int unitPrice = merchant.GetSellPrice(itemId);
            int totalPayment = unitPrice * quantity;
            
            // Execute transaction
            player.inventory.RemoveItem(itemId, quantity);
            player.inventory.AddItem("coin", totalPayment);
            merchant.AddToStock(itemId, quantity);
            
            Debug.Log($"[MerchantService] {player.name} sold {quantity}x {itemId} for {totalPayment} coins to {merchant.DisplayName}");
            return true;
        }
        
        /// <summary>
        /// Check if player can afford an item.
        /// </summary>
        public static bool CanAfford(PlayerController player, Merchant merchant, string itemId, int quantity = 1)
        {
            int totalCost = merchant.GetBuyPrice(itemId) * quantity;
            int playerCoins = player.inventory?.CountItem("coin") ?? 0;
            return playerCoins >= totalCost;
        }
        
        /// <summary>
        /// Check if item can be sold (not coins/keys).
        /// </summary>
        public static bool CanSell(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemId != "coin" && itemId != "key";
        }
    }
}
