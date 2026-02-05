using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    [System.Serializable]
    public class FactionEconomicPolicy
    {
        public TradePhilosophy philosophy;  // Mercantile, Isolationist, Aggressive, etc.
        [Range(0f, 0.5f)] public float preferredTaxRate;
        [Range(0f, 0.5f)] public float importTariffRate;
        [Range(0f, 0.5f)] public float exportTariffRate;
        public List<string> producedItems = new List<string>();
        public List<string> desiredItems = new List<string>();
        public List<string> bannedItems = new List<string>();
    }

    public enum TradePhilosophy { Mercantile, Isolationist, Aggressive, Cooperative, Exploitative }
}
