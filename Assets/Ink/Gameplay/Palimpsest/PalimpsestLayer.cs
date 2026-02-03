using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Minimal palimpsest layer data for the MVP: social overrides only.
    /// </summary>
    public class PalimpsestLayer
    {
        public int id;
        public Vector2Int center;
        public int radius = 5;
        public int priority = 0;
        public int turnsRemaining = 10;

        // Derived flags parsed from tokens
        public bool truce;
        public string allyFactionId;   // e.g., "faction_inkbound" or "PLAYER"
        public string huntFactionId;   // e.g., "faction_ghost"

        // Economic modifiers (stubbed for TDD)
        public float taxDelta = 0f;             // additive tax change (e.g., +0.05 = +5%)
        public float priceMultiplier = 1f;      // multiplicative price bias (e.g., 0.9 = 10% cheaper)

        public List<string> tokens = new List<string>();
    }
}
