using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Pure-logic helpers for the player's ink (mana) resource.
    /// The actual currentInk / maxInk state lives on PlayerController;
    /// this class provides stateless math so it's easily testable.
    /// </summary>
    public static class InkPool
    {
        /// <summary>Starting / default maximum ink.</summary>
        public const int DefaultMaxInk = 50;

        /// <summary>Ink regenerated per player turn.</summary>
        public const int RegenPerTurn = 3;

        /// <summary>Can the player afford to spend <paramref name="cost"/> ink?</summary>
        public static bool HasInk(int currentInk, int cost)
        {
            if (cost <= 0) return true;
            return currentInk >= cost;
        }

        /// <summary>
        /// Returns the new currentInk after spending <paramref name="cost"/>.
        /// Clamps to 0.
        /// </summary>
        public static int SpendInk(int currentInk, int cost)
        {
            return Mathf.Max(0, currentInk - cost);
        }

        /// <summary>
        /// Returns the new currentInk after regenerating <paramref name="amount"/>.
        /// Clamps to maxInk.
        /// </summary>
        public static int RegenInk(int currentInk, int maxInk, int amount)
        {
            return Mathf.Min(maxInk, currentInk + amount);
        }
    }
}
