using System;

namespace InkSim
{
    public enum BuildingType
    {
        TownCenter,
        House,
        Shop,
        Workshop,
        Barracks
    }

    /// <summary>
    /// Describes a single NPC spawn point within a building or roaming in the town.
    /// </summary>
    [Serializable]
    public class NpcSlot
    {
        public TownRole role;
        /// <summary>Merchant profile ID (only relevant for Shopkeeper role).</summary>
        public string merchantId;
        /// <summary>Override faction ID (null = use town faction).</summary>
        public string factionOverrideId;
        /// <summary>Override rank (null = use role default).</summary>
        public string rankOverride;
    }

    /// <summary>
    /// Describes a building footprint and the NPCs it contains.
    /// </summary>
    [Serializable]
    public class BuildingSlot
    {
        public BuildingType type;
        public string label;
        public int minWidth;
        public int maxWidth;
        public int minHeight;
        public int maxHeight;
        public NpcSlot[] npcSlots;
        /// <summary>Tile indices to place as decoration inside the building.</summary>
        public int[] interiorDecorations;
    }
}
