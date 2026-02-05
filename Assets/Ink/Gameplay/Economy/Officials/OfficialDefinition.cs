using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    public enum OfficialJurisdiction
    {
        District,
        Faction,
        Market,
        Guild
    }

    public enum OfficialDisposition
    {
        Greedy,
        Fair,
        Strict,
        Corrupt,
        Zealous
    }

    [CreateAssetMenu(menuName = "Ink/Economy/Official Definition")]
    public class OfficialDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string title;              // "Tax Collector"
        public string displayName;        // "Marcus the Tax Collector"

        [Header("Jurisdiction")]
        public OfficialJurisdiction jurisdiction;  // District, Faction, Market, Guild
        public string districtId;
        public string factionId;

        [Header("Economic Controls")]
        public List<TaxType> controlledTaxTypes = new List<TaxType>();

        [Header("Influence")]
        [Range(0f, 1f)] public float corruptibility;
        public int baseBribeCost;
        public int influenceDecayPerDay;

        [Header("Personality")]
        public OfficialDisposition disposition;  // Greedy, Fair, Strict, Corrupt, Zealous
    }
}
