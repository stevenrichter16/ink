using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// ScriptableObject describing a faction settlement: buildings, NPCs, layout rules.
    /// Created at runtime via ScriptableObject.CreateInstance.
    /// </summary>
    public class TownDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string townId;
        public string displayName;
        public string factionId;

        [Header("Grid Bounds")]
        public int boundsMinX;
        public int boundsMaxX;
        public int boundsMinY;
        public int boundsMaxY;

        [Header("Layout")]
        public int edgePadding = 2;
        public int buildingGap = 2;

        [Header("Buildings")]
        [System.NonSerialized] public List<BuildingSlot> buildings = new List<BuildingSlot>();

        [Header("Roaming NPCs")]
        [System.NonSerialized] public List<NpcSlot> roamingNpcs = new List<NpcSlot>();

        [Header("Decorations")]
        [System.NonSerialized] public int[] outdoorDecorationTiles;
        public int outdoorDecorationCount = 8;

        [Header("Floor Tiles")]
        public int interiorFloorTile = 49;  // FloorStone
        public int outdoorFloorTile = 68;   // FloorDirt
    }
}
