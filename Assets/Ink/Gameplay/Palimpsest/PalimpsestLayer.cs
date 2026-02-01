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

        public List<string> tokens = new List<string>();
    }
}
