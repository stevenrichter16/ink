using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Scales merchant stock quantities based on district prosperity.
    /// High prosperity = merchants carry more goods; low prosperity = scarce stock.
    /// Pure-logic helper — stateless and testable.
    /// </summary>
    public static class MerchantStockScaler
    {
        /// <summary>Minimum stock multiplier (worst-case prosperity).</summary>
        public const float MinStockMultiplier = 0.4f;

        /// <summary>Maximum stock multiplier (best-case prosperity).</summary>
        public const float MaxStockMultiplier = 2.0f;

        /// <summary>
        /// Returns a stock quantity multiplier for the given prosperity value.
        /// Prosperity 1.0 = baseline (mult 1.0). Linear scale clamped to [Min, Max].
        /// </summary>
        public static float GetStockMultiplier(float prosperity)
        {
            // Linear: prosperity 0.1 → 0.4, prosperity 1.0 → 1.0, prosperity 2.0 → 2.0
            float mult = Mathf.Lerp(0f, MaxStockMultiplier, prosperity / 2f);
            return Mathf.Clamp(mult, MinStockMultiplier, MaxStockMultiplier);
        }

        /// <summary>
        /// Scale a base stock quantity by prosperity. Always returns at least 1.
        /// </summary>
        public static int ScaleQuantity(int baseQuantity, float prosperity)
        {
            float mult = GetStockMultiplier(prosperity);
            int scaled = Mathf.RoundToInt(baseQuantity * mult);
            return Mathf.Max(1, scaled);
        }
    }
}
