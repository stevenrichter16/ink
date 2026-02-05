using System.Collections.Generic;

namespace InkSim
{
    [System.Serializable]
    public class FactionTradeRelation
    {
        public string sourceFactionId;
        public string targetFactionId;
        public TradeStatus status;        // Open, Restricted, Embargo, Exclusive, Alliance
        public float tariffRate;
        public List<string> bannedItems = new List<string>();
        public List<string> exclusiveItems = new List<string>();
    }

    public enum TradeStatus { Open, Restricted, Embargo, Exclusive, Alliance }
}
