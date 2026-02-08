using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static factory that produces the concrete Inkbound TownDefinition.
    /// 9 buildings, 4 roaming NPCs, 16 total NPCs.
    /// </summary>
    public static class InkboundTownFactory
    {
        public static TownDefinition Create()
        {
            var def = ScriptableObject.CreateInstance<TownDefinition>();
            def.townId = "town_inkbound";
            def.displayName = "Inkbound Settlement";
            def.factionId = "faction_inkbound";
            def.boundsMinX = 3;
            def.boundsMaxX = 30;
            def.boundsMinY = 132;
            def.boundsMaxY = 198;
            def.edgePadding = 1;
            def.buildingGap = 1;
            def.interiorFloorTile = TestMapBuilder.Tiles.FloorStone;
            def.outdoorFloorTile = TestMapBuilder.Tiles.FloorDirt;

            def.buildings = new List<BuildingSlot>();

            // 1. Town Center — Elder's Hall
            def.buildings.Add(new BuildingSlot
            {
                type = BuildingType.TownCenter,
                label = "Elder's Hall",
                minWidth = 7, maxWidth = 8,
                minHeight = 5, maxHeight = 6,
                npcSlots = new[]
                {
                    new NpcSlot { role = TownRole.Elder }
                },
                interiorDecorations = new[]
                {
                    TestMapBuilder.Tiles.Candle,
                    TestMapBuilder.Tiles.Candle,
                    TestMapBuilder.Tiles.ChestBig
                }
            });

            // 2. General Store
            def.buildings.Add(new BuildingSlot
            {
                type = BuildingType.Shop,
                label = "General Store",
                minWidth = 5, maxWidth = 7,
                minHeight = 4, maxHeight = 5,
                npcSlots = new[]
                {
                    new NpcSlot { role = TownRole.Shopkeeper, merchantId = "general_store" }
                },
                interiorDecorations = new[]
                {
                    TestMapBuilder.Tiles.Barrel,
                    TestMapBuilder.Tiles.Barrel,
                    TestMapBuilder.Tiles.ChestClosed
                }
            });

            // 3. Scribe's Study
            def.buildings.Add(new BuildingSlot
            {
                type = BuildingType.Shop,
                label = "Scribe's Study",
                minWidth = 5, maxWidth = 7,
                minHeight = 4, maxHeight = 5,
                npcSlots = new[]
                {
                    new NpcSlot
                    {
                        role = TownRole.Shopkeeper,
                        merchantId = "scribe_shop",
                        factionOverrideId = "faction_inkbound_scribes"
                    }
                },
                interiorDecorations = new[]
                {
                    TestMapBuilder.Tiles.Candle,
                    TestMapBuilder.Tiles.ChestClosed
                }
            });

            // 4-8. Five Houses (1-2 residents each, 8 total)
            for (int i = 0; i < 5; i++)
            {
                int residentCount = (i < 3) ? 2 : 1; // first 3 houses have 2, last 2 have 1 = 8 total
                var npcs = new NpcSlot[residentCount];
                for (int r = 0; r < residentCount; r++)
                    npcs[r] = new NpcSlot { role = TownRole.Resident };

                def.buildings.Add(new BuildingSlot
                {
                    type = BuildingType.House,
                    label = $"House {i + 1}",
                    minWidth = 3, maxWidth = 5,
                    minHeight = 3, maxHeight = 4,
                    npcSlots = npcs,
                    interiorDecorations = new[]
                    {
                        TestMapBuilder.Tiles.Barrel
                    }
                });
            }

            // 9. Workshop
            def.buildings.Add(new BuildingSlot
            {
                type = BuildingType.Workshop,
                label = "Workshop",
                minWidth = 4, maxWidth = 6,
                minHeight = 3, maxHeight = 4,
                npcSlots = new[]
                {
                    new NpcSlot { role = TownRole.Craftsman }
                },
                interiorDecorations = new[]
                {
                    TestMapBuilder.Tiles.Barrel,
                    TestMapBuilder.Tiles.Barrel,
                    TestMapBuilder.Tiles.ChestClosed
                }
            });

            // Roaming NPCs: 3 Guards + 1 Herald
            def.roamingNpcs = new List<NpcSlot>
            {
                new NpcSlot { role = TownRole.Guard },
                new NpcSlot { role = TownRole.Guard },
                new NpcSlot { role = TownRole.Guard },
                new NpcSlot { role = TownRole.Herald }
            };

            // Outdoor decorations
            def.outdoorDecorationTiles = new[]
            {
                TestMapBuilder.Tiles.TreeSmall,
                TestMapBuilder.Tiles.TreeMedium,
                TestMapBuilder.Tiles.TreePine,
                TestMapBuilder.Tiles.Mushroom,
                TestMapBuilder.Tiles.Barrel
            };
            def.outdoorDecorationCount = 8;

            return def;
        }
    }
}
