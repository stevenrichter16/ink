using System.Collections.Generic;

namespace InkSim
{
    [System.Serializable]
    public struct TaxPolicy
    {
        public string id;
        public TaxType type;           // Sales, Import, Export, Luxury, Guild, War, Tithe
        public float rate;             // 0.0 to 1.0
        public string jurisdictionId;  // District or faction ID
        public List<string> exemptFactions;
        public List<string> exemptItems;
        public List<string> targetItems;
        public int turnsRemaining;     // -1 for permanent
        public string sourceLayerId;   // If from palimpsest
    }

    public enum TaxType { Sales, Import, Export, Luxury, Guild, War, Tithe }
}
