using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Builds a test map using Kenney Micro Roguelike tiles.
    /// Integrates with gameplay systems (GridWorld, TurnManager, entities).
    /// </summary>
    public class TestMapBuilder : MonoBehaviour
    {
        [Header("Tile Settings")]
        public string tileFolderPath = "Assets/Ink/Tiles/Colored";
        public float tileSize = 0.5f;
        public int pixelsPerUnit = 16;

        [Header("Map Size")]
        public int mapWidth = 120;
        public int mapHeight = 70;

        [Header("Debug")]
        public bool showTileCatalog = false;

        // Kenney Micro Roguelike tile indices
        public static class Tiles
        {
            // Floors
            public const int FloorGray1 = 48;
            public const int FloorGray2 = 49;
            public const int FloorStone = 49;
            public const int FloorDirt = 68;
            public const int FloorDirtDark = 69;

            // Walls - corners
            public const int WallCornerTL = 0;
            public const int WallTop = 1;
            public const int WallCornerTR = 2;
            public const int WallLeft = 16;
            public const int WallRight = 18;
            public const int WallCornerBL = 144;
            public const int WallBottom = 145;
            public const int WallCornerBR = 146;
            public const int WallSolid = 17;

            // Doors
            public const int DoorWood = 19;

            // Trees
            public const int TreeSmall = 80;
            public const int TreeMedium = 81;
            public const int TreeLarge = 82;
            public const int TreePine = 83;
            public const int Mushroom = 84;

            // Characters
            public const int HeroKnight = 6;
            public const int Wizard = 15;

            // Enemies
            public const int Snake = 20;
            public const int Ghost = 25;
            public const int Slime = 24;
            public const int Goblin = 26;
            public const int Skeleton = 27;
            public const int Demon = 28;

            // NPCs
            public const int NPC1 = 44;
            public const int NPC2 = 45;

            // Items
            public const int Sword = 70;
            public const int Armor = 42;

            public const int Coin = 128;
            public const int Key = 87;
            public const int Gem = 129;
            public const int Potion = 141;
            public const int ChestClosed = 36;
            public const int ChestBig = 89;
            public const int Barrel = 38;
            public const int Ladder = 88;
            public const int Candle = 140;

            // Buildings
            public const int Castle = 118;

            // Graveyard
            public const int Cross = 103;
            public const int Tombstone = 104;
            public const int CrossLarge = 119;

            // UI - Hearts
            public const int HeartFull = 100;
            public const int HeartHalf = 99;
            public const int HeartEmpty = 98;

        }

        private Sprite[] _allSprites;
        private Transform _floorLayer;
        private Transform _wallLayer;
        private Transform _entityLayer;
        private GridWorld _gridWorld;
        private List<Vector2Int> _wallPositions = new List<Vector2Int>();
        private PlayerController _player;

        private void Start()
        {
            // Force correct map size (overrides any serialized Inspector values)
            mapWidth = 120;
            mapHeight = 70;

            Debug.Log($"[TestMapBuilder] START - mapWidth={mapWidth}, mapHeight={mapHeight}");

            LoadAllTiles();

            if (showTileCatalog)
            {
                ShowTileCatalog();
            }
            else
            {
                SetupGameplaySystems();
                BuildMap();
            }

            SetupCamera();
        }

        private void LoadAllTiles()
        {
            string fullPath = Path.Combine(Application.dataPath, tileFolderPath.Replace("Assets/", ""));
            string[] allFiles = Directory.GetFiles(fullPath, "*.png");
            List<string> tileFiles = new List<string>();
            List<string> extraFiles = new List<string>();

            for (int i = 0; i < allFiles.Length; i++)
            {
                string fileName = Path.GetFileName(allFiles[i]);
                if (fileName.StartsWith("tile_"))
                    tileFiles.Add(allFiles[i]);
                else
                    extraFiles.Add(allFiles[i]);
            }

            tileFiles.Sort();
            extraFiles.Sort();

            string[] files = new string[tileFiles.Count + extraFiles.Count];
            tileFiles.CopyTo(files, 0);
            extraFiles.CopyTo(files, tileFiles.Count);

            _allSprites = new Sprite[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                byte[] data = File.ReadAllBytes(files[i]);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);
                tex.filterMode = FilterMode.Point;
                _allSprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, pixelsPerUnit);
            }

            Debug.Log($"[TestMapBuilder] Loaded {_allSprites.Length} tiles");
        }

        private void SetupGameplaySystems()
        {
            // Create GridWorld
            GameObject gridWorldGO = new GameObject("GridWorld");
            _gridWorld = gridWorldGO.AddComponent<GridWorld>();
            _gridWorld.width = mapWidth;
            _gridWorld.height = mapHeight;
            _gridWorld.tileSize = tileSize;
            _gridWorld.Initialize();

            // Create TurnManager
            GameObject turnManagerGO = new GameObject("TurnManager");
            turnManagerGO.AddComponent<TurnManager>();

            // Create GameManager (for restart functionality)
            GameObject gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
        }

        private void BuildMap()
        {
            Debug.Log($"[TestMapBuilder] BuildMap() - Creating {mapWidth}x{mapHeight} map");

            // Create layer parents
            _floorLayer = new GameObject("FloorLayer").transform;
            _floorLayer.SetParent(transform);

            _wallLayer = new GameObject("WallLayer").transform;
            _wallLayer.SetParent(transform);

            _entityLayer = new GameObject("EntityLayer").transform;
            _entityLayer.SetParent(transform);

            // Place floor everywhere first
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int floorTile = GetFloorTile(x, y);
                    PlaceTile(floorTile, x, y, _floorLayer, 0);
                }
            }

            // Build walls (tracks positions for collision)
            BuildWalls();

            // Register walls with GridWorld
            foreach (var pos in _wallPositions)
            {
                _gridWorld.SetWalkable(pos.x, pos.y, false);
            }

            // Place entities with gameplay components
            PlaceEntities();
        }

        private int GetFloorTile(int x, int y)
        {
            // Building interiors use stone floor
            if (IsInsideBuilding(x, y))
                return Tiles.FloorStone;

            // Everything else is dark dirt/grass
            return Tiles.FloorDirt;
        }

        // Define building interiors across all territories
        private bool IsInsideBuilding(int x, int y)
        {
            // Market Row Shop: x=10-20, y=50-58
            if (x >= 10 && x <= 20 && y >= 50 && y <= 58) return true;
            // Temple Ward Temple: x=50-62, y=50-58
            if (x >= 50 && x <= 62 && y >= 50 && y <= 58) return true;
            // Iron Keep Fortress: x=90-104, y=50-58
            if (x >= 90 && x <= 104 && y >= 50 && y <= 58) return true;
            // Outer Slums Shack: x=10-20, y=8-16
            if (x >= 10 && x <= 20 && y >= 8 && y <= 16) return true;
            // Boneyard Crypt: x=92-102, y=8-16
            if (x >= 92 && x <= 102 && y >= 8 && y <= 16) return true;

            return false;
        }

        private void BuildWalls()
        {
            // === MARKET ROW SHOP (NW territory, x=10-20, y=50-58) ===
            BuildRectRoom(10, 20, 50, 58, "left", 54);

            // === TEMPLE WARD TEMPLE (N-Center territory, x=50-62, y=50-58) ===
            BuildRectRoom(50, 62, 50, 58, "bottom", 56);
            // Internal divider
            for (int x = 51; x < 57; x++)
                PlaceWall(Tiles.WallSolid, x, 54);

            // === IRON KEEP FORTRESS (NE territory, x=90-104, y=50-58) ===
            BuildRectRoom(90, 104, 50, 58, "left", 54);
            // Internal divider with gap
            for (int x = 94; x < 101; x++)
            {
                if (x != 97) // Gap for passage
                    PlaceWall(Tiles.WallSolid, x, 54);
            }

            // === OUTER SLUMS SHACK (SW territory, x=10-20, y=8-16) ===
            BuildRectRoom(10, 20, 8, 16, "top", 15);

            // === BONEYARD CRYPT (SE territory, x=92-102, y=8-16) ===
            BuildRectRoom(92, 102, 8, 16, "left", 12);
        }

        /// <summary>
        /// Builds a rectangular walled room with a single door.
        /// </summary>
        private void BuildRectRoom(int left, int right, int bottom, int top, string doorSide, int doorPos)
        {
            // Bottom wall
            PlaceWall(Tiles.WallCornerBL, left, bottom);
            for (int x = left + 1; x < right; x++)
            {
                if (doorSide == "bottom" && x == doorPos)
                    PlaceTile(Tiles.DoorWood, x, bottom, _wallLayer, 1);
                else
                    PlaceWall(Tiles.WallBottom, x, bottom);
            }
            PlaceWall(Tiles.WallCornerBR, right, bottom);

            // Top wall
            PlaceWall(Tiles.WallCornerTL, left, top);
            for (int x = left + 1; x < right; x++)
            {
                if (doorSide == "top" && x == doorPos)
                    PlaceTile(Tiles.DoorWood, x, top, _wallLayer, 1);
                else
                    PlaceWall(Tiles.WallTop, x, top);
            }
            PlaceWall(Tiles.WallCornerTR, right, top);

            // Left wall
            for (int y = bottom + 1; y < top; y++)
            {
                if (doorSide == "left" && y == doorPos)
                    PlaceTile(Tiles.DoorWood, left, y, _wallLayer, 1);
                else
                    PlaceWall(Tiles.WallLeft, left, y);
            }

            // Right wall
            for (int y = bottom + 1; y < top; y++)
            {
                if (doorSide == "right" && y == doorPos)
                    PlaceTile(Tiles.DoorWood, right, y, _wallLayer, 1);
                else
                    PlaceWall(Tiles.WallRight, right, y);
            }
        }

        private void PlaceWall(int tileIndex, int x, int y)
        {
            PlaceTile(tileIndex, x, y, _wallLayer, 1);
            _wallPositions.Add(new Vector2Int(x, y));
        }

        private void PlaceEntities()
        {
            Debug.Log($"[TestMapBuilder] PlaceEntities() - Creating entities across 6 isolated territories");

            // === PLAYER (Market Row territory) ===
            _player = CreatePlayer(Tiles.HeroKnight, 8, 55);

            // === UI SYSTEMS ===
            CreateHealthUI(_player);
            CreateXPUI(_player);
            CreateInventoryUI(_player);
            CreateSpriteLibrary();
            CreateEquipmentUI(_player);
            CreateTileCursor();
            CreateTileInfoPanel();
            CreateSpellSystem();
            // Territory / economy systems & debug
            var dcsGO = new GameObject("DistrictControlService");
            dcsGO.transform.SetParent(transform, false);
            dcsGO.AddComponent<DistrictControlService>();

            var territoryDebugGO = new GameObject("TerritoryDebugPanel");
            territoryDebugGO.transform.SetParent(transform, false);
            territoryDebugGO.AddComponent<TerritoryDebugPanel>();

            var territoryOverlayGO = new GameObject("TerritoryOverlay");
            territoryOverlayGO.transform.SetParent(transform, false);
            var territoryOverlay = territoryOverlayGO.AddComponent<TerritoryOverlay>();
            territoryOverlay.Initialize(tileSize);

            // World simulation layer (faction AI, dynamic spawning, NPC goals, etc.)
            var simGO = new GameObject("WorldSimulationService");
            simGO.transform.SetParent(transform, false);
            simGO.AddComponent<WorldSimulationService>();
            simGO.AddComponent<SimulationEventLog>();

            // NPC conversation system (speech bubbles above entity heads)
            simGO.AddComponent<ConversationManager>();
            simGO.AddComponent<SpeechBubblePool>();
            simGO.AddComponent<ConversationLogPanel>();

            // === TRAINING DUMMY (Market Row) ===
            CreateDummy(Tiles.Barrel, 12, 55);

            // Load factions
            var inkboundFaction = Resources.Load<FactionDefinition>("Factions/Inkbound");
            var inkboundScribesFaction = Resources.Load<FactionDefinition>("Factions/InkboundScribes");
            var inkguardFaction = Resources.Load<FactionDefinition>("Factions/Inkguard");
            var ghostFaction = Resources.Load<FactionDefinition>("Factions/Ghost");
            var goblinFaction = Resources.Load<FactionDefinition>("Factions/Goblin");
            var skeletonFaction = Resources.Load<FactionDefinition>("Factions/Skeleton");
            var demonFaction = Resources.Load<FactionDefinition>("Factions/Demon");
            var snakeFaction = Resources.Load<FactionDefinition>("Factions/Snake");
            var slimeFaction = Resources.Load<FactionDefinition>("Factions/Slime");

            // Assign territory overlay colors
            if (inkboundFaction != null)  inkboundFaction.color  = new Color(0.2f, 0.4f, 0.9f, 0.25f);
            if (inkguardFaction != null)  inkguardFaction.color  = new Color(0.9f, 0.8f, 0.2f, 0.25f);
            if (skeletonFaction != null)  skeletonFaction.color  = new Color(0.7f, 0.7f, 0.7f, 0.25f);
            if (goblinFaction != null)    goblinFaction.color    = new Color(0.2f, 0.8f, 0.3f, 0.25f);
            if (snakeFaction != null)     snakeFaction.color     = new Color(0.6f, 0.2f, 0.8f, 0.25f);
            if (demonFaction != null)     demonFaction.color     = new Color(0.9f, 0.2f, 0.2f, 0.25f);

            // District HUD (upper-left: shows current district name + controlling faction)
            var districtHudGO = new GameObject("DistrictHUD");
            districtHudGO.transform.SetParent(transform, false);
            districtHudGO.AddComponent<DistrictHUD>();

            // Faction Legend (bottom-left: shows faction color key, synced with overlay toggle)
            var legendGO = new GameObject("FactionLegendPanel");
            legendGO.transform.SetParent(transform, false);
            legendGO.AddComponent<FactionLegendPanel>().Initialize(territoryOverlay);

            // Load species
            var humanSpecies = Resources.Load<SpeciesDefinition>("Species/Human");

            // ================================================================
            // TERRITORY 1: MARKET ROW (NW, x:3-30, y:44-66) — Inkbound/Scribes
            // ================================================================

            // Merchants
            CreateNPC(Tiles.NPC1, 14, 52, NpcAI.AIBehavior.Stationary, "general_store", inkboundFaction, "low", humanSpecies);
            CreateNPC(Tiles.NPC1, 18, 54, NpcAI.AIBehavior.Stationary, "scribe_shop", inkboundScribesFaction ?? inkboundFaction, "mid", humanSpecies);

            // Inkbound sentinels — patrol Market Row
            CreateNPC(Tiles.NPC1, 8, 60, NpcAI.AIBehavior.Wander, null, inkboundFaction, "mid", humanSpecies);
            CreateNPC(Tiles.NPC1, 16, 48, NpcAI.AIBehavior.Wander, null, inkboundFaction, "low", humanSpecies);

            // Market Row trees & vegetation
            PlaceTile(Tiles.TreeLarge, 4, 64, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 7, 62, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 3, 58, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 24, 60, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 26, 65, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 28, 48, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 5, 56, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 22, 46, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 4, 48, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 27, 56, _entityLayer, 5);

            // Market Row building interior decorations
            PlaceTile(Tiles.Barrel, 12, 51, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 13, 51, _entityLayer, 8);
            PlaceTile(Tiles.ChestClosed, 19, 56, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 15, 57, _entityLayer, 8);

            // Market Row enemies
            CreateEnemy(Tiles.Ghost, 6, 62, "ghost", 2);
            CreateEnemy(Tiles.Ghost, 25, 60, "ghost", 3);
            CreateEnemy(Tiles.Slime, 22, 48, "slime", 1);
            CreateEnemy(Tiles.Slime, 5, 50, "slime", 1);

            // Market Row items
            CreateItemPickup("potion", Tiles.Potion, 10, 46, 1);
            CreateItemPickup("coin", Tiles.Coin, 19, 56, 5);
            CreateItemPickup("leather_armor", Tiles.Armor, 15, 52, 1);

            // ================================================================
            // TERRITORY 2: TEMPLE WARD (N-Center, x:44-70, y:44-66) — Inkguard
            // ================================================================

            // Merchants & quest givers
            CreateNPC(Tiles.NPC2, 54, 52, NpcAI.AIBehavior.Stationary, "weaponsmith", inkguardFaction, "mid", humanSpecies);
            CreateNPC(Tiles.Wizard, 58, 56, NpcAI.AIBehavior.Stationary, null, inkguardFaction, "high", humanSpecies);

            // Inkguard soldiers — patrol Temple Ward
            CreateNPC(Tiles.NPC2, 48, 58, NpcAI.AIBehavior.Wander, null, inkguardFaction, "mid", humanSpecies);
            CreateNPC(Tiles.NPC2, 64, 48, NpcAI.AIBehavior.Wander, null, inkguardFaction, "mid", humanSpecies);
            CreateNPC(Tiles.NPC1, 56, 62, NpcAI.AIBehavior.Wander, null, inkguardFaction, "low", humanSpecies);

            // Temple Ward trees
            PlaceTile(Tiles.TreeLarge, 46, 64, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 68, 62, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 45, 48, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 69, 46, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 50, 64, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 66, 60, _entityLayer, 5);

            // Temple Ward building interior
            PlaceTile(Tiles.Candle, 52, 56, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 60, 56, _entityLayer, 8);
            PlaceTile(Tiles.ChestBig, 61, 52, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 51, 52, _entityLayer, 8);

            // Temple Ward enemies
            CreateEnemy(Tiles.Skeleton, 50, 48, "skeleton", 2);
            CreateEnemy(Tiles.Goblin, 62, 60, "goblin", 2);
            CreateEnemy(Tiles.Ghost, 66, 64, "ghost", 3);
            CreateEnemy(Tiles.Goblin, 68, 52, "goblin", 3);

            // Temple Ward items
            CreateItemPickup("ink", Tiles.Candle, 55, 56, 3);
            CreateItemPickup("key", Tiles.Key, 60, 53, 1);
            CreateItemPickup("iron_armor", Tiles.Armor, 52, 52, 1);
            CreateItemPickup("ring", Tiles.Gem, 65, 54, 1);

            // ================================================================
            // TERRITORY 3: IRON KEEP (NE, x:84-112, y:44-66) — Skeleton/Ghost
            // ================================================================

            // Merchants
            CreateNPC(Tiles.Skeleton, 96, 52, NpcAI.AIBehavior.Stationary, "bone_armory", skeletonFaction, "high", null);

            // Skeleton sentries & Ghost guards — patrol Iron Keep
            CreateNPC(Tiles.Skeleton, 88, 58, NpcAI.AIBehavior.Wander, null, skeletonFaction, "mid", null);
            CreateNPC(Tiles.Skeleton, 102, 48, NpcAI.AIBehavior.Wander, null, skeletonFaction, "low", null);
            CreateNPC(Tiles.Ghost, 86, 62, NpcAI.AIBehavior.Wander, null, ghostFaction, "low", null);

            // Iron Keep trees (sparse — military zone)
            PlaceTile(Tiles.TreeLarge, 85, 64, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 110, 62, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 86, 46, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 108, 46, _entityLayer, 5);

            // Iron Keep building interior
            PlaceTile(Tiles.Barrel, 92, 51, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 93, 51, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 98, 57, _entityLayer, 8);
            PlaceTile(Tiles.ChestClosed, 103, 56, _entityLayer, 8);
            PlaceTile(Tiles.ChestBig, 101, 52, _entityLayer, 8);

            // Iron Keep enemies
            CreateEnemy(Tiles.Skeleton, 94, 56, "skeleton", 3);
            CreateEnemy(Tiles.Skeleton, 106, 50, "skeleton", 3);
            CreateEnemy(Tiles.Ghost, 100, 60, "ghost", 4);
            CreateEnemy(Tiles.Ghost, 86, 48, "ghost", 4);
            CreateEnemy(Tiles.Skeleton, 110, 58, "skeleton", 4);

            // Iron Keep items
            CreateItemPickup("shield", Tiles.Armor, 98, 53, 1);
            CreateItemPickup("steel_armor", Tiles.Armor, 103, 52, 1);
            CreateItemPickup("potion", Tiles.Potion, 92, 56, 2);

            // ================================================================
            // TERRITORY 4: OUTER SLUMS (SW, x:3-30, y:3-25) — Goblin
            // ================================================================

            // Merchants
            CreateNPC(Tiles.Goblin, 14, 12, NpcAI.AIBehavior.Stationary, "goblin_fence", goblinFaction, "high", null);

            // Goblin scouts — patrol Outer Slums
            CreateNPC(Tiles.Goblin, 6, 8, NpcAI.AIBehavior.Wander, null, goblinFaction, "mid", null);
            CreateNPC(Tiles.Goblin, 22, 18, NpcAI.AIBehavior.Wander, null, goblinFaction, "low", null);

            // Outer Slums trees & debris
            PlaceTile(Tiles.TreeLarge, 4, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 8, 24, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 26, 20, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 3, 6, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 28, 4, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 24, 22, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 7, 18, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 20, 6, _entityLayer, 5);

            // Outer Slums building interior
            PlaceTile(Tiles.Barrel, 12, 10, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 13, 10, _entityLayer, 8);
            PlaceTile(Tiles.ChestClosed, 18, 14, _entityLayer, 8);

            // Outer Slums enemies
            CreateEnemy(Tiles.Goblin, 8, 4, "goblin", 1);
            CreateEnemy(Tiles.Snake, 22, 10, "snake", 1);
            CreateEnemy(Tiles.Slime, 12, 20, "slime", 2);
            CreateEnemy(Tiles.Goblin, 26, 14, "goblin", 2);
            CreateEnemy(Tiles.Goblin, 5, 16, "goblin", 1);

            // Outer Slums items
            CreateItemPickup("sword", Tiles.Sword, 16, 10, 1);
            CreateItemPickup("coin", Tiles.Coin, 8, 16, 5);
            CreateItemPickup("potion", Tiles.Potion, 24, 6, 1);

            // ================================================================
            // TERRITORY 5: THE WILDS (S-Center, x:44-70, y:3-25) — Snake/Slime
            // ================================================================

            // Merchants
            CreateNPC(Tiles.Snake, 56, 14, NpcAI.AIBehavior.Stationary, "snake_herbalist", snakeFaction, "mid", null);

            // Snake rangers — patrol The Wilds
            CreateNPC(Tiles.Snake, 50, 8, NpcAI.AIBehavior.Wander, null, snakeFaction, "mid", null);
            CreateNPC(Tiles.Snake, 64, 18, NpcAI.AIBehavior.Wander, null, snakeFaction, "low", null);

            // Wilds dense forest (no building — wilderness territory)
            PlaceTile(Tiles.TreeLarge, 46, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 50, 24, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 54, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 58, 24, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 62, 20, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 66, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 48, 4, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 52, 6, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 60, 4, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 68, 6, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 45, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 67, 14, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 49, 16, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 63, 10, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 55, 20, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 46, 8, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 64, 24, _entityLayer, 5);

            // Wilds enemies
            CreateEnemy(Tiles.Snake, 48, 12, "snake", 1);
            CreateEnemy(Tiles.Snake, 60, 18, "snake", 2);
            CreateEnemy(Tiles.Slime, 54, 6, "slime", 1);
            CreateEnemy(Tiles.Slime, 66, 10, "slime", 1);
            CreateEnemy(Tiles.Goblin, 68, 16, "goblin", 2);
            CreateEnemy(Tiles.Snake, 46, 20, "snake", 2);

            // Wilds items
            CreateItemPickup("dagger", Tiles.Sword, 54, 10, 1);
            CreateItemPickup("potion", Tiles.Potion, 62, 16, 1);
            CreateItemPickup("coin", Tiles.Coin, 50, 22, 8);

            // ================================================================
            // TERRITORY 6: THE BONEYARD (SE, x:84-112, y:3-25) — Demon
            // ================================================================

            // Merchants
            CreateNPC(Tiles.Demon, 96, 12, NpcAI.AIBehavior.Stationary, "demon_broker", demonFaction, "mid", null);

            // Demon wardens — patrol Boneyard
            CreateNPC(Tiles.Demon, 88, 18, NpcAI.AIBehavior.Wander, null, demonFaction, "mid", null);
            CreateNPC(Tiles.Demon, 106, 8, NpcAI.AIBehavior.Wander, null, demonFaction, "mid", null);

            // Boneyard graveyard props
            PlaceTile(Tiles.Cross, 86, 6, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 90, 4, _entityLayer, 5);
            PlaceTile(Tiles.CrossLarge, 94, 6, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 88, 8, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 100, 4, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 104, 6, _entityLayer, 5);
            PlaceTile(Tiles.CrossLarge, 108, 4, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 110, 8, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 86, 20, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 102, 22, _entityLayer, 5);

            // Boneyard crypt interior
            PlaceTile(Tiles.Candle, 94, 14, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 100, 14, _entityLayer, 8);
            PlaceTile(Tiles.ChestBig, 98, 10, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 94, 10, _entityLayer, 8);

            // Sparse trees around Boneyard edges
            PlaceTile(Tiles.TreeLarge, 85, 24, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 110, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 112, 4, _entityLayer, 5);

            // Boneyard enemies
            CreateEnemy(Tiles.Demon, 88, 6, "demon", 4);
            CreateEnemy(Tiles.Skeleton, 100, 20, "skeleton", 3);
            CreateEnemy(Tiles.Ghost, 108, 14, "ghost", 5);
            CreateEnemy(Tiles.Goblin, 92, 22, "goblin", 2);
            CreateEnemy(Tiles.Demon, 106, 4, "demon", 5);

            // Boneyard items
            CreateItemPickup("gem", Tiles.Gem, 94, 9, 2);
            CreateItemPickup("potion_large", Tiles.Potion, 100, 15, 1);
            CreateItemPickup("coin", Tiles.Coin, 98, 20, 15);
            CreateItemPickup("gem", Tiles.Gem, 108, 10, 1);

            // ================================================================
            // WILDERNESS CORRIDORS — Forest between territories
            // ================================================================

            // Horizontal corridor: Market Row ↔ Temple Ward (x:31-43, y:50-60)
            PlaceTile(Tiles.TreeLarge, 33, 58, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 36, 54, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 40, 60, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 34, 48, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 38, 52, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 42, 56, _entityLayer, 5);

            // Horizontal corridor: Temple Ward ↔ Iron Keep (x:71-83, y:50-60)
            PlaceTile(Tiles.TreeLarge, 73, 56, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 76, 52, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 80, 58, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 74, 48, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 78, 54, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 82, 50, _entityLayer, 5);

            // Vertical corridor: Market Row ↔ Outer Slums (x:10-20, y:26-43)
            PlaceTile(Tiles.TreeLarge, 6, 36, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 10, 30, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 14, 38, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 8, 42, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 18, 34, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 22, 28, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 4, 32, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 26, 40, _entityLayer, 5);

            // Vertical corridor: Temple Ward ↔ Wilds (x:50-60, y:26-43)
            PlaceTile(Tiles.TreeLarge, 48, 36, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 54, 30, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 60, 38, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 52, 42, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 58, 34, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 64, 28, _entityLayer, 5);

            // Vertical corridor: Iron Keep ↔ Boneyard (x:92-102, y:26-43)
            PlaceTile(Tiles.TreeLarge, 88, 36, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 94, 30, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 100, 38, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 90, 42, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 96, 34, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 104, 28, _entityLayer, 5);

            // Horizontal corridor: Outer Slums ↔ Wilds (x:31-43, y:10-18)
            PlaceTile(Tiles.TreeLarge, 33, 16, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 36, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 40, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 34, 6, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 38, 10, _entityLayer, 5);

            // Horizontal corridor: Wilds ↔ Boneyard (x:71-83, y:10-18)
            PlaceTile(Tiles.TreeLarge, 73, 14, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 76, 10, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 80, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 74, 6, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 78, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 82, 8, _entityLayer, 5);

            // Center wilderness (x:31-83, y:26-43) — dense forest
            PlaceTile(Tiles.TreeLarge, 35, 32, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 40, 36, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 46, 30, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 50, 40, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 56, 32, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 62, 38, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 68, 34, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 72, 30, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 76, 40, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 80, 36, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 44, 34, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 58, 38, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 70, 32, _entityLayer, 5);

            // ================================================================
            // CROSS-FACTION BORDER PATROLS (in wilderness between territories)
            // ================================================================

            // Market Row / Outer Slums border — Inkbound meets Goblin (tense)
            CreateNPC(Tiles.NPC1, 15, 38, NpcAI.AIBehavior.Wander, null, inkboundFaction, "low", humanSpecies);
            CreateNPC(Tiles.Goblin, 15, 32, NpcAI.AIBehavior.Wander, null, goblinFaction, "low", null);

            // Temple Ward / Wilds border — Inkguard meets Snake (tense)
            CreateNPC(Tiles.NPC2, 57, 38, NpcAI.AIBehavior.Wander, null, inkguardFaction, "low", humanSpecies);
            CreateNPC(Tiles.Snake, 57, 32, NpcAI.AIBehavior.Wander, null, snakeFaction, "low", null);

            // Iron Keep / Boneyard border — Skeleton meets Demon (allied)
            CreateNPC(Tiles.Skeleton, 98, 38, NpcAI.AIBehavior.Wander, null, skeletonFaction, "low", null);
            CreateNPC(Tiles.Demon, 98, 32, NpcAI.AIBehavior.Wander, null, demonFaction, "low", null);

            // Market Row / Temple Ward border — Ghost meets Goblin (tense)
            CreateNPC(Tiles.Ghost, 35, 56, NpcAI.AIBehavior.Wander, null, ghostFaction, "low", null);
            CreateNPC(Tiles.Goblin, 39, 56, NpcAI.AIBehavior.Wander, null, goblinFaction, "low", null);

            // === PALIMPSEST INSCRIPTIONS ===
            BootstrapInscriptions();

            // === TRADE RELATIONS ===
            BootstrapTradeRelations();

            // === INTER-FACTION REPUTATION ===
            BootstrapInterFactionRep();

            // === DEMAND EVENTS ===
            BootstrapDemandEvents();
        }

        private void BootstrapInscriptions()
        {
            // Register palimpsest layers directly (using grid coordinates for correct distance checks)
            // Market Row — CHEAP zone (prices lower near general store)
            RegisterPalimpsestLayer(14, 52, new List<string> { "DEFLATE:0.2" }, radius: 6, turns: -1);
            // Iron Keep — TAX_HEAVY zone (military taxation)
            RegisterPalimpsestLayer(96, 54, new List<string> { "TAX:+0.15" }, radius: 7, turns: -1);
            // Boneyard — SCARCITY + EXPENSIVE zone (harsh economy)
            RegisterPalimpsestLayer(96, 12, new List<string> { "INFLATE:0.3", "SCARCITY:gem" }, radius: 6, turns: -1);
            // Wilds — FREE_TRADE zone (no taxes, no restrictions)
            RegisterPalimpsestLayer(56, 14, new List<string> { "FREE_TRADE" }, radius: 7, turns: -1);
            // Temple Ward — TRUCE zone (no combat near the temple)
            RegisterPalimpsestLayer(56, 54, new List<string> { "TRUCE", "TAX_BREAK:0.05" }, radius: 6, turns: -1);
            // Outer Slums — BLACK_MARKET zone (stolen goods flow freely)
            RegisterPalimpsestLayer(14, 12, new List<string> { "DEFLATE:0.1", "FREE_TRADE" }, radius: 5, turns: -1);
        }

        private void RegisterPalimpsestLayer(int gridX, int gridY, List<string> tokens, int radius = 5, int turns = -1, int priority = 0)
        {
            var layer = new PalimpsestLayer
            {
                center = new Vector2Int(gridX, gridY),
                radius = radius,
                priority = priority,
                turnsRemaining = turns,
                tokens = new List<string>(tokens)
            };
            OverlayResolver.RegisterLayer(layer);
        }

        private void BootstrapTradeRelations()
        {
            // Inkguard vs Skeleton — Mortal enemies, no trade
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = "faction_inkguard",
                targetFactionId = "faction_skeleton",
                status = TradeStatus.Embargo,
                tariffRate = 0f,
                bannedItems = new List<string>(),
                exclusiveItems = new List<string>()
            });

            // Inkbound vs Ghost — Restricted trade, ghosts ban potions
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = "faction_inkbound",
                targetFactionId = "faction_ghost",
                status = TradeStatus.Restricted,
                tariffRate = 0.25f,
                bannedItems = new List<string> { "potion" },
                exclusiveItems = new List<string>()
            });

            // Goblin + Demon — Criminal alliance, cheap trade
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = "faction_goblin",
                targetFactionId = "faction_demon",
                status = TradeStatus.Alliance,
                tariffRate = 0.05f,
                bannedItems = new List<string>(),
                exclusiveItems = new List<string>()
            });

            // Inkguard + Inkbound — Human faction alliance
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = "faction_inkguard",
                targetFactionId = "faction_inkbound",
                status = TradeStatus.Alliance,
                tariffRate = 0.02f,
                bannedItems = new List<string>(),
                exclusiveItems = new List<string>()
            });

            // Skeleton + Ghost — Undead alliance
            TradeRelationRegistry.SetRelation(new FactionTradeRelation
            {
                sourceFactionId = "faction_skeleton",
                targetFactionId = "faction_ghost",
                status = TradeStatus.Alliance,
                tariffRate = 0f,
                bannedItems = new List<string>(),
                exclusiveItems = new List<string>()
            });
        }

        private void BootstrapInterFactionRep()
        {
            // Allies: start with positive inter-rep so friendly cross-faction conversations fire
            ReputationSystem.SetInterRep("faction_inkguard", "faction_inkbound", 40);
            ReputationSystem.SetInterRep("faction_inkbound", "faction_inkguard", 40);
            ReputationSystem.SetInterRep("faction_goblin", "faction_demon", 35);
            ReputationSystem.SetInterRep("faction_demon", "faction_goblin", 35);
            ReputationSystem.SetInterRep("faction_skeleton", "faction_ghost", 30);
            ReputationSystem.SetInterRep("faction_ghost", "faction_skeleton", 30);

            // Enemies: start with negative inter-rep so hostile cross-faction conversations fire
            ReputationSystem.SetInterRep("faction_inkguard", "faction_skeleton", -50);
            ReputationSystem.SetInterRep("faction_skeleton", "faction_inkguard", -50);
            ReputationSystem.SetInterRep("faction_inkguard", "faction_goblin", -30);
            ReputationSystem.SetInterRep("faction_goblin", "faction_inkguard", -30);
            ReputationSystem.SetInterRep("faction_inkbound", "faction_skeleton", -35);
            ReputationSystem.SetInterRep("faction_skeleton", "faction_inkbound", -35);

            // Neutral/wary: small negative or near-zero for factions that don't like each other but aren't at war
            ReputationSystem.SetInterRep("faction_inkbound", "faction_goblin", -15);
            ReputationSystem.SetInterRep("faction_goblin", "faction_inkbound", -15);
            ReputationSystem.SetInterRep("faction_inkguard", "faction_demon", -40);
            ReputationSystem.SetInterRep("faction_demon", "faction_inkguard", -40);
            ReputationSystem.SetInterRep("faction_inkbound", "faction_demon", -20);
            ReputationSystem.SetInterRep("faction_demon", "faction_inkbound", -20);

            // Ghost vs human factions — uneasy, the Inkbound restrict their trade
            ReputationSystem.SetInterRep("faction_ghost", "faction_inkguard", -24);
            ReputationSystem.SetInterRep("faction_inkguard", "faction_ghost", -24);
            ReputationSystem.SetInterRep("faction_ghost", "faction_inkbound", -15);
            ReputationSystem.SetInterRep("faction_inkbound", "faction_ghost", -15);
            ReputationSystem.SetInterRep("faction_ghost", "faction_goblin", -10);
            ReputationSystem.SetInterRep("faction_goblin", "faction_ghost", -10);
            ReputationSystem.SetInterRep("faction_ghost", "faction_demon", 10);
            ReputationSystem.SetInterRep("faction_demon", "faction_ghost", 10);

            // Snake (faction_snake) — hostile to most, wary of goblins
            ReputationSystem.SetInterRep("faction_snake", "faction_inkguard", -45);
            ReputationSystem.SetInterRep("faction_inkguard", "faction_snake", -45);
            ReputationSystem.SetInterRep("faction_snake", "faction_inkbound", -35);
            ReputationSystem.SetInterRep("faction_inkbound", "faction_snake", -35);
            ReputationSystem.SetInterRep("faction_snake", "faction_goblin", -10);
            ReputationSystem.SetInterRep("faction_goblin", "faction_snake", -10);
            ReputationSystem.SetInterRep("faction_snake", "faction_skeleton", -20);
            ReputationSystem.SetInterRep("faction_skeleton", "faction_snake", -20);
            ReputationSystem.SetInterRep("faction_snake", "faction_demon", -15);
            ReputationSystem.SetInterRep("faction_demon", "faction_snake", -15);

            Debug.Log("[TestMapBuilder] Seeded inter-faction reputation for 6 factions.");
        }

        private void BootstrapDemandEvents()
        {
            // Iron Keep garrison needs supplies
            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = "startup_potion_demand",
                itemId = "potion",
                demandMultiplier = 1.5f,
                durationDays = 10,
                districtId = "district_ironkeep",
                description = "Garrison needs supplies"
            });

            // Necromantic rituals in the Boneyard drive gem demand
            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = "startup_gem_demand",
                itemId = "gem",
                demandMultiplier = 2.0f,
                durationDays = 8,
                districtId = "district_boneyard",
                description = "Necromantic rituals"
            });

            // Scribes need ink in the Temple Ward
            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = "startup_ink_demand",
                itemId = "ink",
                demandMultiplier = 1.8f,
                durationDays = 12,
                districtId = "district_temple",
                description = "Scribes need ink"
            });
        }

private PlayerController CreatePlayer(int tileIndex, int x, int y)
        {
            GameObject go = new GameObject("Player");
            go.transform.SetParent(_entityLayer);
            go.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _allSprites[tileIndex];
            sr.sortingOrder = 10;

            PlayerController player = go.AddComponent<PlayerController>();
            player.gridX = x;
            player.gridY = y;

            // Add leveling system
            Levelable levelable = go.AddComponent<Levelable>();
            player.levelable = levelable;

            // Load and assign player-specific level profile
            var playerProfile = Resources.Load<LevelProfile>("Levels/PlayerHighHp");
            if (playerProfile != null)
            {
                levelable.profile = playerProfile;
                levelable.RecomputeStats();
            }

            return player;
        }

private void CreateHealthUI(PlayerController player)
        {
            GameObject go = new GameObject("HealthUI");
            HealthUI healthUI = go.AddComponent<HealthUI>();
            healthUI.player = player;
            healthUI.heartFull = _allSprites[Tiles.HeartFull];
            healthUI.heartEmpty = _allSprites[Tiles.HeartEmpty];
        }

private void CreateXPUI(PlayerController player)
        {
            GameObject go = new GameObject("XPUI");
            XPUI xpUI = go.AddComponent<XPUI>();
            xpUI.target = player.levelable;
        }


private void CreateInventoryUI(PlayerController player)
        {
            GameObject go = new GameObject("InventoryUI");
            InventoryUI inv = go.AddComponent<InventoryUI>();
            inv.player = player;
        }

private void CreateSpriteLibrary()
        {
            GameObject go = new GameObject("SpriteLibrary");
            SpriteLibrary lib = go.AddComponent<SpriteLibrary>();
            lib.SetSprites(_allSprites);
        }

        private void CreateEquipmentUI(PlayerController player)
        {
            GameObject go = new GameObject("EquipmentUI");
            EquipmentUI ui = go.AddComponent<EquipmentUI>();
            ui.player = player;
        }

private void CreateTileCursor()
        {
            GameObject go = new GameObject("TileCursor");
            TileCursor cursor = go.AddComponent<TileCursor>();
            cursor.gridWorld = _gridWorld;
        }

private void CreateTileInfoPanel()
        {
            GameObject go = new GameObject("TileInfoPanel");
            TileInfoPanel panel = go.AddComponent<TileInfoPanel>();
            panel.gridWorld = _gridWorld;
            panel.cursor = UnityEngine.Object.FindFirstObjectByType<TileCursor>();
        }

private void CreateSpellSystem()
        {
            GameObject go = new GameObject("SpellSystem");
            SpellSystem spellSystem = go.AddComponent<SpellSystem>();
            spellSystem.player = _player;
            spellSystem.tileCursor = UnityEngine.Object.FindFirstObjectByType<TileCursor>();
            spellSystem.gridWorld = _gridWorld;
        }







private void CreateEnemy(int tileIndex, int x, int y, string lootTableId, int level = 1, SpeciesDefinition species = null, FactionDefinition faction = null, string factionRankId = "low")
        {
            Debug.Log($"[TestMapBuilder] CreateEnemy at ({x}, {y}) - {lootTableId} level {level}");

            GameObject enemyGO = new GameObject($"Enemy_{x}_{y}");
            enemyGO.transform.SetParent(_entityLayer);
            enemyGO.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);

            SpriteRenderer sr = enemyGO.AddComponent<SpriteRenderer>();
            sr.sprite = _allSprites[tileIndex];
            sr.sortingOrder = 10;

            // Add Levelable first
            Levelable levelable = enemyGO.AddComponent<Levelable>();
            #if UNITY_EDITOR
            levelable.profile = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelProfile>("Assets/Ink/Data/Levels/DefaultLevelProfile.asset");
            #endif
            levelable.SetLevel(level);

            EnemyAI enemy = enemyGO.AddComponent<EnemyAI>();
            enemy.gridX = x;
            enemy.gridY = y;
            enemy.levelable = levelable;
            enemy.currentHealth = enemy.maxHealth; // Initialize from levelable
            enemy.lootTableId = lootTableId;
            enemy.enemyId = lootTableId;
            enemy.leashRange = 15;

            if (species == null)
            {
                species = EnemyFactory.GetDefaultSpeciesForEnemyId(lootTableId);
            }

            if (species != null)
            {
                var speciesMember = enemyGO.GetComponent<SpeciesMember>() ?? enemyGO.AddComponent<SpeciesMember>();
                speciesMember.species = species;
                speciesMember.EnsureDefaultFaction();
}


            if (faction != null || species != null)
            {
                var member = enemyGO.AddComponent<FactionMember>();
                member.faction = faction;
                member.rankId = factionRankId;
                member.ApplyRank();
            }

            _gridWorld.SetOccupant(x, y, enemy);
        }

        private void CreateNPC(int tileIndex, int x, int y, NpcAI.AIBehavior behavior, string merchantId = null, FactionDefinition faction = null, string factionRankId = "low", SpeciesDefinition species = null)
        {
            GameObject go = new GameObject($"NPC_{x}_{y}");
            go.transform.SetParent(_entityLayer);
            go.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _allSprites[tileIndex];
            sr.sortingOrder = 10;

            NpcAI npc = go.AddComponent<NpcAI>();
            npc.gridX = x;
            npc.gridY = y;
            npc.behavior = behavior;

            // Add merchant component if specified
            if (!string.IsNullOrEmpty(merchantId))
            {
                Merchant merchant = go.AddComponent<Merchant>();
                merchant.profileId = merchantId;
            }

            // Dialogue + faction
            DialogueRunner runner = go.AddComponent<DialogueRunner>();
            runner.questIdForStateSwitch = "quest_slime_hunt";

            // Species & default faction handling
            SpeciesMember speciesMember = null;
            if (species != null)
            {
                speciesMember = go.GetComponent<SpeciesMember>() ?? go.AddComponent<SpeciesMember>();
                speciesMember.species = species;
                speciesMember.EnsureDefaultFaction();
            }

            if (faction != null || speciesMember != null)
            {
                var member = go.AddComponent<FactionMember>();
                member.faction = faction ?? species?.defaultFaction;
                member.rankId = factionRankId;
                member.ApplyRank();

                // Assign home district from spawn position (null for wilderness border patrols)
                var dcs = DistrictControlService.Instance;
                if (dcs != null)
                {
                    var districtState = dcs.GetStateByPosition(x, y);
                    if (districtState != null)
                        member.homeDistrictId = districtState.Id;
                }
            }
            else
            {
                runner.defaultSequence = Resources.Load<DialogueSequence>("Dialogues/Dialogue_Merchant_Offer");
                runner.onQuestCompleteSequence = Resources.Load<DialogueSequence>("Dialogues/Dialogue_Merchant_TurnIn");
            }
        }

private void CreateDummy(int tileIndex, int x, int y)
        {
            GameObject go = new GameObject($"Dummy_{x}_{y}");
            go.transform.SetParent(_entityLayer);
            go.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _allSprites[tileIndex];
            sr.sortingOrder = 10;

            AttackDummy dummy = go.AddComponent<AttackDummy>();
            dummy.gridX = x;
            dummy.gridY = y;
        }


private void CreateItemPickup(string itemId, int tileIndex, int x, int y, int quantity = 1)
        {
            Debug.Log($"[TestMapBuilder] CreateItemPickup at ({x}, {y}) - {itemId} x{quantity}");

            GameObject go = new GameObject($"Pickup_{itemId}_{x}_{y}");
            go.transform.SetParent(_entityLayer);
            go.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _allSprites[tileIndex];
            sr.sortingOrder = 8;

            ItemPickup pickup = go.AddComponent<ItemPickup>();
            pickup.itemId = itemId;
            pickup.quantity = quantity;
            pickup.gridX = x;
            pickup.gridY = y;
        }


        private void PlaceTile(int tileIndex, int gridX, int gridY, Transform parent, int sortOrder)
        {
            if (tileIndex < 0 || tileIndex >= _allSprites.Length)
            {
                Debug.LogWarning($"Tile index {tileIndex} out of range");
                return;
            }

            GameObject go = new GameObject($"tile_{tileIndex}_{gridX}_{gridY}");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(gridX * tileSize, gridY * tileSize, 0);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _allSprites[tileIndex];
            sr.sortingOrder = sortOrder;
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.12f, 0.18f);

            if (showTileCatalog)
            {
                cam.transform.position = new Vector3(mapWidth * tileSize * 0.5f - tileSize * 0.5f, -mapHeight * tileSize * 0.5f + tileSize * 0.5f, -10);
                cam.orthographicSize = mapHeight * tileSize * 0.55f;
            }
            else
            {
                // Keep viewport size same as original 14-tile height map
                cam.orthographicSize = 14 * tileSize * 0.6f;

                // Add camera controller for smooth follow
                CameraController camController = cam.gameObject.AddComponent<CameraController>();
                camController.target = _player?.transform;
                camController.gridWorld = _gridWorld;
                camController.smoothSpeed = 8f;
                camController.deadZone = new Vector2(0.5f, 0.3f);
                camController.enableLookAhead = true;
                camController.lookAheadDistance = 0.8f;

                // Snap to player initially
                camController.SnapToTarget();
            }
        }

        private void ShowTileCatalog()
        {
            int cols = 16;

            for (int i = 0; i < _allSprites.Length; i++)
            {
                int x = i % cols;
                int y = i / cols;

                GameObject go = new GameObject($"tile_{i:D4}");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(x * tileSize, -y * tileSize, 0);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _allSprites[i];
                sr.sortingOrder = 0;
            }

            mapWidth = cols;
            mapHeight = (_allSprites.Length + cols - 1) / cols;
        }
    }
}
