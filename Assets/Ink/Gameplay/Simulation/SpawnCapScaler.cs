using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Pure-logic helper for scaling spawn caps and raid sizes with district prosperity.
    /// Prosperous districts support more defenders; impoverished ones are raided harder.
    /// </summary>
    public static class SpawnCapScaler
    {
        /// <summary>Base per-faction enemy cap in a district at prosperity 1.0.</summary>
        public const int BaseReinforcementCap = 4;

        /// <summary>Minimum enemies per district-faction, even at worst prosperity.</summary>
        public const int MinCap = 1;

        /// <summary>Maximum enemies per district-faction, even at best prosperity.</summary>
        public const int MaxCap = 8;

        /// <summary>
        /// Returns the reinforcement cap for a faction in a district at given prosperity.
        /// Linear scale: prosperity 0.1→1, 1.0→4, 2.0→8.
        /// </summary>
        public static int GetReinforcementCap(float prosperity)
        {
            int cap = Mathf.RoundToInt(BaseReinforcementCap * prosperity);
            return Mathf.Clamp(cap, MinCap, MaxCap);
        }

        /// <summary>
        /// Returns the raid party size, scaled up for low-prosperity targets.
        /// Low prosperity attracts larger raids (power vacuum).
        /// </summary>
        public static int GetRaidSize(int baseSize, float targetProsperity)
        {
            // Invert prosperity: low prosperity → larger raids
            // prosperity 0.3 → mult ~1.5, prosperity 1.0 → mult 1.0, prosperity 1.5 → mult ~0.85
            float mult = Mathf.Lerp(1.5f, 0.75f, Mathf.InverseLerp(0.1f, 2f, targetProsperity));
            int size = Mathf.RoundToInt(baseSize * mult);
            return Mathf.Max(2, size); // At least 2 raiders
        }
    }
}
