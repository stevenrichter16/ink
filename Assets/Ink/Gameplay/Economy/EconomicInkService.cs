using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Reads and spends player ink for economic inscriptions.
    /// </summary>
    public static class EconomicInkService
    {
        public const string InkItemId = "ink";

        private static PlayerController _cachedPlayer;

        private static PlayerController GetPlayer()
        {
            if (_cachedPlayer == null)
                _cachedPlayer = Object.FindObjectOfType<PlayerController>();
            return _cachedPlayer;
        }

        public static int GetInkBalance()
        {
            var player = GetPlayer();
            if (player == null || player.inventory == null) return 0;
            return player.inventory.CountItem(InkItemId);
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            var player = GetPlayer();
            if (player == null || player.inventory == null) return false;

            int current = player.inventory.CountItem(InkItemId);
            if (current < amount) return false;
            return player.inventory.RemoveItem(InkItemId, amount);
        }
    }
}
