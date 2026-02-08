using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Result returned by TownBuilder.Build(), containing the placed building interiors
    /// so the caller can register them for floor-tile detection and walkability.
    /// </summary>
    public class TownBuildResult
    {
        public List<RectInt> buildingInteriors = new List<RectInt>();
    }

    /// <summary>
    /// Static utility that reads a TownDefinition and generates the full town:
    /// walls, doors, floors, NPCs, decorations.
    /// Uses delegates to call back into TestMapBuilder methods so no visibility changes are needed.
    /// </summary>
    public static class TownBuilder
    {
        /// <summary>Delegate matching TestMapBuilder.BuildRectRoom signature.</summary>
        public delegate void BuildRoomDelegate(int left, int right, int bottom, int top, string doorSide, int doorPos);

        /// <summary>Delegate matching TestMapBuilder.CreateNPC signature.</summary>
        public delegate void CreateNpcDelegate(int tileIndex, int x, int y, NpcAI.AIBehavior behavior,
            string merchantId, FactionDefinition faction, string factionRankId, SpeciesDefinition species);

        /// <summary>Delegate matching TestMapBuilder.PlaceTile signature.</summary>
        public delegate void PlaceTileDelegate(int tileIndex, int gridX, int gridY, Transform parent, int sortOrder);

        // Placed building footprints (including walls) for collision checks during layout
        private static readonly List<RectInt> _placedFootprints = new List<RectInt>();

        /// <summary>
        /// Build a town from the given definition.
        /// </summary>
        public static TownBuildResult Build(
            TownDefinition def,
            BuildRoomDelegate buildRoom,
            CreateNpcDelegate createNpc,
            PlaceTileDelegate placeTile,
            FactionDefinition faction,
            SpeciesDefinition species,
            Transform entityLayer)
        {
            _placedFootprints.Clear();
            var result = new TownBuildResult();

            int minX = def.boundsMinX + def.edgePadding;
            int maxX = def.boundsMaxX - def.edgePadding;
            int minY = def.boundsMinY + def.edgePadding;
            int maxY = def.boundsMaxY - def.edgePadding;

            // ---- Phase 1: Layout buildings ----
            var placements = new List<BuildingPlacement>();

            // Place Town Center first (top-center of bounds, door facing south)
            int townCenterIdx = -1;
            for (int i = 0; i < def.buildings.Count; i++)
            {
                if (def.buildings[i].type == BuildingType.TownCenter)
                {
                    townCenterIdx = i;
                    break;
                }
            }

            if (townCenterIdx >= 0)
            {
                var tcSlot = def.buildings[townCenterIdx];
                int w = tcSlot.minWidth; // Use min size for reliable layout
                int h = tcSlot.minHeight;
                int cx = (minX + maxX) / 2 - w / 2;
                int cy = maxY - h;
                var placement = new BuildingPlacement(tcSlot, cx, cy, w, h, "bottom");
                placements.Add(placement);
                _placedFootprints.Add(placement.Footprint(def.buildingGap));
            }

            // Pack remaining buildings in rows below the Town Center
            int tcBottom = townCenterIdx >= 0 ? placements[0].y : maxY;
            int cursorX = minX;
            int cursorY = tcBottom - def.buildingGap - 1; // start one row below TC
            int rowMaxHeight = 0;

            int retryCount = 0;
            const int maxRetries = 50; // Safety limit per building

            for (int i = 0; i < def.buildings.Count; i++)
            {
                if (i == townCenterIdx) continue;

                var slot = def.buildings[i];
                int w = slot.minWidth; // Use min size for reliable layout
                int h = slot.minHeight;

                // If building doesn't fit in current row, start a new row
                if (cursorX + w > maxX)
                {
                    cursorX = minX;
                    cursorY -= rowMaxHeight + def.buildingGap + 1;
                    rowMaxHeight = 0;
                }

                int bottomY = cursorY - h + 1;

                // If we've run out of vertical space, skip
                if (bottomY < minY)
                {
                    Debug.LogWarning($"[TownBuilder] Ran out of space for building '{slot.label}'");
                    retryCount = 0;
                    continue;
                }

                // Choose door side: prefer side facing open space
                string doorSide = PickDoorSide(cursorX, bottomY, w, h, minX, maxX, minY, maxY);

                var placement = new BuildingPlacement(slot, cursorX, bottomY, w, h, doorSide);

                // Collision check against all placed buildings
                bool overlaps = false;
                for (int p = 0; p < _placedFootprints.Count; p++)
                {
                    if (Overlaps(_placedFootprints[p], placement.Footprint(def.buildingGap)))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                {
                    retryCount++;
                    if (retryCount > maxRetries)
                    {
                        Debug.LogWarning($"[TownBuilder] Exceeded retry limit for building '{slot.label}', skipping");
                        retryCount = 0;
                        continue;
                    }
                    // Slide right past the collision and retry this building
                    cursorX += def.buildingGap + 1;
                    i--; // retry this building at new position
                    continue;
                }

                retryCount = 0;

                placements.Add(placement);
                _placedFootprints.Add(placement.Footprint(def.buildingGap));

                if (h > rowMaxHeight) rowMaxHeight = h;
                cursorX += w + def.buildingGap + 1;
            }

            Debug.Log($"[TownBuilder] Placed {placements.Count}/{def.buildings.Count} buildings in {def.displayName}");

            // ---- Phase 2: Build rooms and spawn building NPCs ----
            for (int p = 0; p < placements.Count; p++)
            {
                var pl = placements[p];
                int left = pl.x;
                int bottom = pl.y;
                int right = pl.x + pl.width;
                int top = pl.y + pl.height;

                // Determine door position (center of the door side)
                int doorPos;
                if (pl.doorSide == "bottom" || pl.doorSide == "top")
                    doorPos = left + pl.width / 2;
                else
                    doorPos = bottom + pl.height / 2;

                buildRoom(left, right, bottom, top, pl.doorSide, doorPos);

                // Register interior (inside walls) for floor detection
                var interior = new RectInt(left + 1, bottom + 1, pl.width - 2, pl.height - 2);
                result.buildingInteriors.Add(interior);

                // Spawn building NPCs at random interior tiles
                if (pl.slot.npcSlots != null)
                {
                    for (int n = 0; n < pl.slot.npcSlots.Length; n++)
                    {
                        var npcSlot = pl.slot.npcSlots[n];
                        int spawnX = UnityEngine.Random.Range(left + 1, right);
                        int spawnY = UnityEngine.Random.Range(bottom + 1, top);

                        // Resolve faction override
                        FactionDefinition npcFaction = faction;
                        if (!string.IsNullOrEmpty(npcSlot.factionOverrideId))
                        {
                            var overrideFaction = Resources.Load<FactionDefinition>($"Factions/{StripPrefix(npcSlot.factionOverrideId)}");
                            if (overrideFaction != null) npcFaction = overrideFaction;
                        }

                        int spriteIndex;
                        NpcAI.AIBehavior behavior;
                        string rank;
                        ResolveRoleDefaults(npcSlot, out spriteIndex, out behavior, out rank);

                        createNpc(spriteIndex, spawnX, spawnY, behavior,
                            npcSlot.merchantId, npcFaction, rank, species);
                    }
                }

                // Place interior decorations
                if (pl.slot.interiorDecorations != null)
                {
                    for (int d = 0; d < pl.slot.interiorDecorations.Length; d++)
                    {
                        int decoX = UnityEngine.Random.Range(left + 1, right);
                        int decoY = UnityEngine.Random.Range(bottom + 1, top);
                        placeTile(pl.slot.interiorDecorations[d], decoX, decoY, entityLayer, 8);
                    }
                }
            }

            // ---- Phase 3: Spawn roaming NPCs ----
            if (def.roamingNpcs != null)
            {
                for (int r = 0; r < def.roamingNpcs.Count; r++)
                {
                    var npcSlot = def.roamingNpcs[r];
                    Vector2Int pos = FindOutdoorTile(def, result.buildingInteriors);

                    FactionDefinition npcFaction = faction;
                    if (!string.IsNullOrEmpty(npcSlot.factionOverrideId))
                    {
                        var overrideFaction = Resources.Load<FactionDefinition>($"Factions/{StripPrefix(npcSlot.factionOverrideId)}");
                        if (overrideFaction != null) npcFaction = overrideFaction;
                    }

                    int spriteIndex;
                    NpcAI.AIBehavior behavior;
                    string rank;
                    ResolveRoleDefaults(npcSlot, out spriteIndex, out behavior, out rank);

                    createNpc(spriteIndex, pos.x, pos.y, behavior,
                        npcSlot.merchantId, npcFaction, rank, species);
                }
            }

            // ---- Phase 4: Outdoor decorations ----
            if (def.outdoorDecorationTiles != null && def.outdoorDecorationTiles.Length > 0)
            {
                for (int d = 0; d < def.outdoorDecorationCount; d++)
                {
                    Vector2Int pos = FindOutdoorTile(def, result.buildingInteriors);
                    int tileIdx = def.outdoorDecorationTiles[UnityEngine.Random.Range(0, def.outdoorDecorationTiles.Length)];
                    placeTile(tileIdx, pos.x, pos.y, entityLayer, 5);
                }
            }

            _placedFootprints.Clear();
            return result;
        }

        // ---- Helpers ----

        private static void ResolveRoleDefaults(NpcSlot slot, out int spriteIndex, out NpcAI.AIBehavior behavior, out string rank)
        {
            // Use rank override if provided
            rank = !string.IsNullOrEmpty(slot.rankOverride) ? slot.rankOverride : null;

            switch (slot.role)
            {
                case TownRole.Elder:
                    spriteIndex = TestMapBuilder.Tiles.Wizard;
                    behavior = NpcAI.AIBehavior.Stationary;
                    if (rank == null) rank = "high";
                    break;
                case TownRole.Guard:
                    spriteIndex = TestMapBuilder.Tiles.NPC2;
                    behavior = NpcAI.AIBehavior.Wander;
                    if (rank == null) rank = "mid";
                    break;
                case TownRole.Herald:
                    spriteIndex = TestMapBuilder.Tiles.NPC2;
                    behavior = NpcAI.AIBehavior.Wander;
                    if (rank == null) rank = "low";
                    break;
                case TownRole.Shopkeeper:
                    spriteIndex = TestMapBuilder.Tiles.NPC1;
                    behavior = NpcAI.AIBehavior.Stationary;
                    if (rank == null) rank = "low";
                    break;
                case TownRole.Craftsman:
                    spriteIndex = TestMapBuilder.Tiles.NPC2;
                    behavior = NpcAI.AIBehavior.Stationary;
                    if (rank == null) rank = "mid";
                    break;
                default: // Resident
                    spriteIndex = TestMapBuilder.Tiles.NPC1;
                    behavior = NpcAI.AIBehavior.Stationary;
                    if (rank == null) rank = "low";
                    break;
            }
        }

        private static string PickDoorSide(int x, int y, int w, int h, int minX, int maxX, int minY, int maxY)
        {
            // Prefer south-facing, then whichever side has the most space
            int spaceBelow = y - minY;
            int spaceAbove = maxY - (y + h);
            int spaceLeft = x - minX;
            int spaceRight = maxX - (x + w);

            // Default: bottom (south-facing)
            string best = "bottom";
            int bestSpace = spaceBelow;

            if (spaceAbove > bestSpace) { best = "top"; bestSpace = spaceAbove; }
            if (spaceLeft > bestSpace) { best = "left"; bestSpace = spaceLeft; }
            if (spaceRight > bestSpace) { best = "right"; }

            return best;
        }

        private static Vector2Int FindOutdoorTile(TownDefinition def, List<RectInt> interiors)
        {
            // Try up to 50 times to find a tile outside all building interiors
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = UnityEngine.Random.Range(def.boundsMinX + 1, def.boundsMaxX);
                int y = UnityEngine.Random.Range(def.boundsMinY + 1, def.boundsMaxY);

                bool insideBuilding = false;
                for (int i = 0; i < interiors.Count; i++)
                {
                    // Check against expanded rect (includes walls)
                    var inner = interiors[i];
                    if (x >= inner.xMin - 1 && x <= inner.xMax + 1 &&
                        y >= inner.yMin - 1 && y <= inner.yMax + 1)
                    {
                        insideBuilding = true;
                        break;
                    }
                }

                if (!insideBuilding)
                    return new Vector2Int(x, y);
            }

            // Fallback: just pick somewhere in bounds
            return new Vector2Int(
                (def.boundsMinX + def.boundsMaxX) / 2,
                (def.boundsMinY + def.boundsMaxY) / 2);
        }

        private static bool Overlaps(RectInt a, RectInt b)
        {
            return a.xMin < b.xMax && a.xMax > b.xMin &&
                   a.yMin < b.yMax && a.yMax > b.yMin;
        }

        /// <summary>
        /// Convert faction ID (e.g. "faction_inkbound_scribes") to Resources path name
        /// (e.g. "InkboundScribes") by stripping "faction_" prefix and PascalCasing.
        /// </summary>
        private static string StripPrefix(string factionId)
        {
            if (string.IsNullOrEmpty(factionId)) return factionId;

            string raw = factionId;
            if (raw.StartsWith("faction_"))
                raw = raw.Substring(8); // strip "faction_"

            // PascalCase: split on '_', capitalize each part
            var parts = raw.Split('_');
            var sb = new System.Text.StringBuilder(raw.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                sb.Append(char.ToUpper(parts[i][0]));
                if (parts[i].Length > 1)
                    sb.Append(parts[i].Substring(1));
            }
            return sb.ToString();
        }

        // Internal placement record
        private struct BuildingPlacement
        {
            public BuildingSlot slot;
            public int x, y, width, height;
            public string doorSide;

            public BuildingPlacement(BuildingSlot slot, int x, int y, int w, int h, string doorSide)
            {
                this.slot = slot;
                this.x = x;
                this.y = y;
                this.width = w;
                this.height = h;
                this.doorSide = doorSide;
            }

            /// <summary>Footprint including walls and gap margin for collision checks.</summary>
            public RectInt Footprint(int gap)
            {
                return new RectInt(x - gap, y - gap, width + gap * 2, height + gap * 2);
            }
        }
    }
}
