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
        public int mapWidth = 48;
        public int mapHeight = 28;

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
            mapWidth = 48;
            mapHeight = 28;
            
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
            // Dungeon interior uses stone floor
            if (IsInsideDungeon(x, y))
                return Tiles.FloorStone;
            
            // Everything else is dark dirt/grass
            return Tiles.FloorDirt;
        }
        
        // Define dungeon bounds (L-shaped building + Iron Keep outpost)
private bool IsInsideDungeon(int x, int y)
        {
            // Main room: x=18-38, y=5-14
            bool inMainRoom = x >= 18 && x <= 38 && y >= 5 && y <= 14;
            // Upper wing: x=28-38, y=14-22
            bool inUpperWing = x >= 28 && x <= 38 && y >= 14 && y <= 22;
            // Iron Keep outpost: x=38-46, y=10-16
            bool inOutpost = x >= 38 && x <= 46 && y >= 10 && y <= 16;

            return inMainRoom || inUpperWing || inOutpost;
        }

private void BuildWalls()
        {
            // === L-SHAPED DUNGEON COMPLEX ===
            // Based on screenshot: dungeon in center-right of map
            
            // Main room bounds (lower large room)
            int mainLeft = 18;
            int mainRight = 38;
            int mainBottom = 5;
            int mainTop = 14;
            
            // Upper wing bounds (corridor + small room)
            int upperLeft = 28;
            int upperRight = 38;
            int upperBottom = 14;
            int upperTop = 22;
            
            // === MAIN ROOM (lower section) ===
            // Bottom wall
            PlaceWall(Tiles.WallCornerBL, mainLeft, mainBottom);
            for (int x = mainLeft + 1; x < mainRight; x++)
                PlaceWall(Tiles.WallBottom, x, mainBottom);
            PlaceWall(Tiles.WallCornerBR, mainRight, mainBottom);
            
            // Left wall with door
            int doorY = 10;
            for (int y = mainBottom + 1; y < mainTop; y++)
            {
                if (y == doorY)
                    PlaceTile(Tiles.DoorWood, mainLeft, y, _wallLayer, 1); // Door is walkable
                else
                    PlaceWall(Tiles.WallLeft, mainLeft, y);
            }
            
            // Top wall of main room (partial - connects to upper wing)
            PlaceWall(Tiles.WallCornerTL, mainLeft, mainTop);
            for (int x = mainLeft + 1; x < upperLeft; x++)
                PlaceWall(Tiles.WallTop, x, mainTop);
            
            // Right wall of main room (partial - connects to upper wing)
            for (int y = mainBottom + 1; y < upperBottom; y++)
                PlaceWall(Tiles.WallRight, mainRight, y);
            
            // === UPPER WING (corridor + room) ===
            // Left wall of upper wing
            for (int y = mainTop; y < upperTop; y++)
                PlaceWall(Tiles.WallLeft, upperLeft, y);
            PlaceWall(Tiles.WallCornerTL, upperLeft, upperTop);
            
            // Top wall
            for (int x = upperLeft + 1; x < upperRight; x++)
                PlaceWall(Tiles.WallTop, x, upperTop);
            PlaceWall(Tiles.WallCornerTR, upperRight, upperTop);
            
            // Right wall
            for (int y = mainBottom + 1; y < upperTop; y++)
                PlaceWall(Tiles.WallRight, upperRight, y);
            
            // === INTERNAL WALLS ===
            // Horizontal divider in main room
            int dividerY = 10;
            for (int x = mainLeft + 6; x < mainRight - 2; x++)
            {
                if (x != mainLeft + 10) // Gap for passage
                    PlaceWall(Tiles.WallSolid, x, dividerY);
            }
            
            // Small room divider in upper wing
            int smallRoomDivider = upperTop - 4;
            for (int x = upperLeft + 1; x < upperRight - 4; x++)
                PlaceWall(Tiles.WallSolid, x, smallRoomDivider);
        }

        private void PlaceWall(int tileIndex, int x, int y)
        {
            PlaceTile(tileIndex, x, y, _wallLayer, 1);
            _wallPositions.Add(new Vector2Int(x, y));
        }

private void PlaceEntities()
        {
            Debug.Log($"[TestMapBuilder] PlaceEntities() - Creating entities based on reference screenshot");

            // === PLAYER (left side of map) ===
            _player = CreatePlayer(Tiles.HeroKnight, 8, 12);

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

            // World simulation layer (faction AI, dynamic spawning, NPC goals, etc.)
            var simGO = new GameObject("WorldSimulationService");
            simGO.transform.SetParent(transform, false);
            simGO.AddComponent<WorldSimulationService>();

            // === TRAINING DUMMY ===
            CreateDummy(Tiles.Barrel, 10, 10);

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

            // Load species
            var humanSpecies = Resources.Load<SpeciesDefinition>("Species/Human");

            // === NPCs (Original) ===
            // General Store — Market Row, Inkbound faction
            CreateNPC(Tiles.NPC1, 6, 18, NpcAI.AIBehavior.Stationary, "general_store", inkboundFaction, "low", humanSpecies);
            // Weaponsmith inside dungeon — Inkguard faction
            CreateNPC(Tiles.NPC2, 32, 8, NpcAI.AIBehavior.Stationary, "weaponsmith", inkguardFaction, "mid", humanSpecies);

            // === NPCs (New) ===
            // Inkbound Scribe — Market Row
            CreateNPC(Tiles.NPC1, 10, 20, NpcAI.AIBehavior.Stationary, "scribe_shop", inkboundScribesFaction ?? inkboundFaction, "mid", humanSpecies);
            // Goblin Fence — Outer Slums
            CreateNPC(Tiles.Goblin, 8, 6, NpcAI.AIBehavior.Stationary, "goblin_fence", goblinFaction, "high", null);
            // Skeleton Armory — Iron Keep
            CreateNPC(Tiles.Skeleton, 40, 12, NpcAI.AIBehavior.Stationary, "bone_armory", skeletonFaction, "high", null);
            // Demon Broker — Boneyard
            CreateNPC(Tiles.Demon, 43, 4, NpcAI.AIBehavior.Stationary, "demon_broker", demonFaction, "mid", null);
            // Snake Herbalist — Wilds
            CreateNPC(Tiles.Snake, 20, 24, NpcAI.AIBehavior.Stationary, "snake_herbalist", snakeFaction, "mid", null);
            // Inkguard Patrol Captain — Quest Giver, Temple Ward
            CreateNPC(Tiles.Wizard, 26, 14, NpcAI.AIBehavior.Stationary, null, inkguardFaction, "high", humanSpecies);

            // === FOREST - DENSE TREE PLACEMENT ===
            // Upper left forest
            PlaceTile(Tiles.TreeLarge, 2, 25, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 5, 24, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 8, 26, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 3, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 10, 25, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 14, 24, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 6, 23, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 12, 26, _entityLayer, 5);

            // Left side forest (dense)
            PlaceTile(Tiles.TreeLarge, 1, 20, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 4, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 2, 15, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 6, 16, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 3, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 1, 9, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 5, 8, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 2, 6, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 4, 14, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 7, 11, _entityLayer, 5);

            // Lower left forest
            PlaceTile(Tiles.TreeLarge, 1, 4, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 4, 2, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 7, 3, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 10, 1, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 12, 3, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 15, 2, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 6, 1, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 9, 4, _entityLayer, 5);

            // Trees around dungeon entrance
            PlaceTile(Tiles.TreeLarge, 14, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 16, 8, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 12, 6, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 15, 10, _entityLayer, 5);

            // Right side forest (sparse - near graveyard)
            PlaceTile(Tiles.TreeLarge, 42, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 44, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 40, 24, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 46, 20, _entityLayer, 5);

            // Bottom center trees
            PlaceTile(Tiles.TreeLarge, 20, 2, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 25, 1, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 30, 3, _entityLayer, 5);

            // Wilds area trees (new — give the Wilds visual identity)
            PlaceTile(Tiles.TreeLarge, 16, 22, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 18, 26, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 22, 25, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 24, 21, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 27, 23, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 15, 20, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 17, 24, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 26, 22, _entityLayer, 5);

            // === GRAVEYARD (bottom right) ===
            PlaceTile(Tiles.Cross, 40, 4, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 42, 3, _entityLayer, 5);
            PlaceTile(Tiles.CrossLarge, 44, 5, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 41, 6, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 45, 2, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 43, 7, _entityLayer, 5);
            // Extended Boneyard graveyard props
            PlaceTile(Tiles.Cross, 37, 1, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 38, 5, _entityLayer, 5);
            PlaceTile(Tiles.CrossLarge, 46, 3, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 39, 7, _entityLayer, 5);

            // === DUNGEON INTERIOR DECORATIONS ===
            // Main room furniture
            PlaceTile(Tiles.Barrel, 20, 7, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 21, 7, _entityLayer, 8);
            PlaceTile(Tiles.ChestClosed, 35, 7, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 25, 12, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 33, 12, _entityLayer, 8);
            // Upper wing
            PlaceTile(Tiles.ChestBig, 36, 20, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 30, 16, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 34, 16, _entityLayer, 8);

            // === IRON KEEP OUTPOST (east side, x=38-46, y=10-16) ===
            // Bottom wall
            PlaceWall(Tiles.WallCornerBL, 38, 10);
            for (int x = 39; x < 46; x++)
                PlaceWall(Tiles.WallBottom, x, 10);
            PlaceWall(Tiles.WallCornerBR, 46, 10);
            // Top wall
            PlaceWall(Tiles.WallCornerTL, 38, 16);
            for (int x = 39; x < 46; x++)
                PlaceWall(Tiles.WallTop, x, 16);
            PlaceWall(Tiles.WallCornerTR, 46, 16);
            // Left wall with door
            for (int y = 11; y < 16; y++)
            {
                if (y == 13)
                    PlaceTile(Tiles.DoorWood, 38, y, _wallLayer, 1); // Door on west side
                else
                    PlaceWall(Tiles.WallLeft, 38, y);
            }
            // Right wall
            for (int y = 11; y < 16; y++)
                PlaceWall(Tiles.WallRight, 46, y);
            // Interior decorations
            PlaceTile(Tiles.Barrel, 40, 11, _entityLayer, 8);
            PlaceTile(Tiles.Barrel, 41, 11, _entityLayer, 8);
            PlaceTile(Tiles.Candle, 42, 15, _entityLayer, 8);
            PlaceTile(Tiles.ChestClosed, 45, 14, _entityLayer, 8);

            // === ENEMIES (Original — with bug fixes) ===
            // Forest enemies (left/upper area)
            CreateEnemy(Tiles.Ghost, 5, 20, "ghost", 3);
            CreateEnemy(Tiles.Ghost, 8, 14, "ghost", 2);
            CreateEnemy(Tiles.Slime, 10, 22, "slime", 1);
            CreateEnemy(Tiles.Slime, 3, 8, "slime", 1);
            CreateEnemy(Tiles.Goblin, 12, 18, "goblin", 2);  // BUG FIX: was "skeleton"
            CreateEnemy(Tiles.Snake, 9, 19, "snake", 1);
            CreateEnemy(Tiles.Snake, 11, 21, "snake", 1);
            CreateEnemy(Tiles.Snake, 13, 17, "snake", 1);

            // Enemies near dungeon
            CreateEnemy(Tiles.Ghost, 15, 14, "ghost", 3);
            CreateEnemy(Tiles.Slime, 14, 6, "slime", 1);

            // Inside dungeon
            CreateEnemy(Tiles.Skeleton, 22, 8, "skeleton", 2);
            CreateEnemy(Tiles.Skeleton, 28, 12, "skeleton", 2);
            CreateEnemy(Tiles.Ghost, 34, 8, "ghost", 3);    // BUG FIX: was "skeleton"
            CreateEnemy(Tiles.Ghost, 32, 19, "ghost", 4);

            // Graveyard skeletons
            CreateEnemy(Tiles.Skeleton, 42, 5, "skeleton", 2);
            CreateEnemy(Tiles.Skeleton, 45, 4, "skeleton", 3);
            CreateEnemy(Tiles.Ghost, 40, 8, "ghost", 4);     // BUG FIX: was "skeleton"

            // === ENEMIES (New) ===
            // Wilds (upper forest, y=19-27)
            CreateEnemy(Tiles.Goblin, 16, 24, "goblin", 2);
            CreateEnemy(Tiles.Goblin, 22, 22, "goblin", 2);
            CreateEnemy(Tiles.Snake, 19, 26, "snake", 2);
            CreateEnemy(Tiles.Snake, 25, 20, "snake", 1);

            // Iron Keep (east side, x=33-47, y=9-22)
            CreateEnemy(Tiles.Skeleton, 36, 12, "skeleton", 3);
            CreateEnemy(Tiles.Skeleton, 40, 16, "skeleton", 3);
            CreateEnemy(Tiles.Ghost, 38, 20, "ghost", 4);
            CreateEnemy(Tiles.Goblin, 42, 14, "goblin", 3);
            CreateEnemy(Tiles.Demon, 44, 18, "demon", 5);

            // Boneyard (bottom-right, x=36-47, y=0-8)
            CreateEnemy(Tiles.Demon, 38, 3, "demon", 4);
            CreateEnemy(Tiles.Skeleton, 44, 2, "skeleton", 3);
            CreateEnemy(Tiles.Ghost, 46, 6, "ghost", 5);
            CreateEnemy(Tiles.Goblin, 40, 1, "goblin", 2);

            // Dungeon Interior — new additions
            CreateEnemy(Tiles.Goblin, 25, 11, "goblin", 2);
            CreateEnemy(Tiles.Demon, 35, 18, "demon", 6);    // Boss-tier demon in upper wing

            // Outer Slums — new additions
            CreateEnemy(Tiles.Goblin, 6, 5, "goblin", 1);
            CreateEnemy(Tiles.Snake, 8, 2, "snake", 1);
            CreateEnemy(Tiles.Slime, 11, 10, "slime", 2);

            // === PICKUPABLE ITEMS (Original) ===
            // Forest items
            CreateItemPickup("potion", Tiles.Potion, 7, 16, 1);
            CreateItemPickup("coin", Tiles.Coin, 4, 10, 5);
            CreateItemPickup("sword", Tiles.Sword, 11, 4, 1);

            // Dungeon items
            CreateItemPickup("potion", Tiles.Potion, 23, 7, 1);
            CreateItemPickup("coin", Tiles.Coin, 30, 8, 10);
            CreateItemPickup("key", Tiles.Key, 35, 12, 1);
            CreateItemPickup("gem", Tiles.Gem, 36, 19, 1);
            CreateItemPickup("leather_armor", Tiles.Armor, 20, 12, 1);
            CreateItemPickup("iron_armor", Tiles.Armor, 32, 16, 1);

            // Graveyard loot
            CreateItemPickup("potion", Tiles.Potion, 44, 6, 2);
            CreateItemPickup("coin", Tiles.Coin, 41, 3, 15);

            // === PICKUPABLE ITEMS (New — missing item types) ===
            CreateItemPickup("dagger", Tiles.Sword, 17, 22, 1);         // Wilds
            CreateItemPickup("shield", Tiles.Armor, 37, 15, 1);         // Iron Keep
            CreateItemPickup("ring", Tiles.Gem, 29, 20, 1);             // Upper wing area
            CreateItemPickup("potion_large", Tiles.Potion, 41, 7, 1);   // Boneyard
            CreateItemPickup("ink", Tiles.Candle, 24, 16, 3);           // Temple Ward
            CreateItemPickup("steel_armor", Tiles.Armor, 45, 16, 1);    // Iron Keep outpost
            CreateItemPickup("gem", Tiles.Gem, 39, 2, 2);               // Boneyard
            CreateItemPickup("coin", Tiles.Coin, 15, 23, 8);            // Wilds
            CreateItemPickup("potion", Tiles.Potion, 34, 14, 2);        // Iron Keep approach

            // === PALIMPSEST INSCRIPTIONS ===
            BootstrapInscriptions();

            // === TRADE RELATIONS ===
            BootstrapTradeRelations();

            // === DEMAND EVENTS ===
            BootstrapDemandEvents();
        }

        private void BootstrapInscriptions()
        {
            // Register palimpsest layers directly (using grid coordinates for correct distance checks)
            // Market Row — CHEAP zone (prices lower near general store)
            RegisterPalimpsestLayer(6, 18, new List<string> { "DEFLATE:0.2" }, radius: 4, turns: -1);
            // Iron Keep — TAX_HEAVY zone (military taxation)
            RegisterPalimpsestLayer(40, 14, new List<string> { "TAX:+0.15" }, radius: 5, turns: -1);
            // Boneyard — SCARCITY + EXPENSIVE zone (harsh economy)
            RegisterPalimpsestLayer(43, 4, new List<string> { "INFLATE:0.3", "SCARCITY:gem" }, radius: 4, turns: -1);
            // Wilds — FREE_TRADE zone (no taxes, no restrictions)
            RegisterPalimpsestLayer(20, 24, new List<string> { "FREE_TRADE" }, radius: 5, turns: -1);
            // Temple Ward — TRUCE zone (no combat near the temple)
            RegisterPalimpsestLayer(26, 14, new List<string> { "TRUCE", "TAX_BREAK:0.05" }, radius: 4, turns: -1);
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
            panel.cursor = FindObjectOfType<TileCursor>();
        }

private void CreateSpellSystem()
        {
            GameObject go = new GameObject("SpellSystem");
            SpellSystem spellSystem = go.AddComponent<SpellSystem>();
            spellSystem.player = _player;
            spellSystem.tileCursor = FindObjectOfType<TileCursor>();
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
