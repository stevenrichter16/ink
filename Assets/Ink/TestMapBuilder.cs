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
        public int mapHeight = 210;

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
        private List<RectInt> _buildingInteriors = new List<RectInt>();

        private void Start()
        {
            // Force correct map size (overrides any serialized Inspector values)
            mapWidth = 120;
            mapHeight = 210;

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

            // Build walls (empty — TownBuilder generates buildings)
            BuildWalls();

            // Place entities (TownBuilder creates town buildings & NPCs here)
            PlaceEntities();

            // Place floor tiles AFTER PlaceEntities so _buildingInteriors is populated
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int floorTile = GetFloorTile(x, y);
                    PlaceTile(floorTile, x, y, _floorLayer, 0);
                }
            }
        }

        private int GetFloorTile(int x, int y)
        {
            // Building interiors use stone floor
            if (IsInsideBuilding(x, y))
                return Tiles.FloorStone;

            // Everything else is dark dirt/grass
            return Tiles.FloorDirt;
        }

        // Dynamic building interior detection — populated by TownBuilder
        private bool IsInsideBuilding(int x, int y)
        {
            for (int i = 0; i < _buildingInteriors.Count; i++)
            {
                var r = _buildingInteriors[i];
                if (x >= r.xMin && x < r.xMin + r.width &&
                    y >= r.yMin && y < r.yMin + r.height)
                    return true;
            }
            return false;
        }

        private void BuildWalls()
        {
            // Town buildings are now generated by TownBuilder in PlaceEntities().
            // This method is kept for any future non-town wall placement.
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
            Debug.Log($"[TestMapBuilder] PlaceEntities() - Inkbound town + empty wilderness");

            // === PLAYER (Market Row territory) ===
            _player = CreatePlayer(Tiles.HeroKnight, 8, 165);

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
            CreateDummy(Tiles.Barrel, 12, 165);

            // Load factions
            var inkboundFaction = Resources.Load<FactionDefinition>("Factions/Inkbound");
            // Load all factions (needed for territory overlay colors and simulation systems)
            var inkguardFaction = Resources.Load<FactionDefinition>("Factions/Inkguard");
            var skeletonFaction = Resources.Load<FactionDefinition>("Factions/Skeleton");
            var goblinFaction = Resources.Load<FactionDefinition>("Factions/Goblin");
            var snakeFaction = Resources.Load<FactionDefinition>("Factions/Snake");
            var demonFaction = Resources.Load<FactionDefinition>("Factions/Demon");
            // Pre-load remaining factions for simulation systems (no direct references needed)
            Resources.Load<FactionDefinition>("Factions/InkboundScribes");
            Resources.Load<FactionDefinition>("Factions/Ghost");
            Resources.Load<FactionDefinition>("Factions/Slime");

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
            // INKBOUND TOWN (Market Row, x:3-30, y:132-198)
            // Built via data-driven TownBuilder
            // ================================================================
            var townDef = InkboundTownFactory.Create();
            var townResult = TownBuilder.Build(
                townDef,
                BuildRectRoom,
                CreateNPC,
                PlaceTile,
                inkboundFaction,
                humanSpecies,
                _entityLayer);

            // Register building interiors for floor-tile detection
            _buildingInteriors.AddRange(townResult.buildingInteriors);

            // Register town building walls with GridWorld
            foreach (var pos in _wallPositions)
            {
                _gridWorld.SetWalkable(pos.x, pos.y, false);
            }

            // Market Row items (near town)
            CreateItemPickup("potion", Tiles.Potion, 10, 138, 1);
            CreateItemPickup("coin", Tiles.Coin, 19, 168, 5);
            CreateItemPickup("leather_armor", Tiles.Armor, 15, 156, 1);

            // ================================================================
            // WILDERNESS — Trees in all territories (no NPCs, no enemies)
            // ================================================================

            // Territory 2 area trees (Temple Ward)
            PlaceTile(Tiles.TreeLarge, 46, 192, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 68, 186, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 45, 144, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 69, 138, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 50, 192, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 66, 180, _entityLayer, 5);

            // Territory 3 area trees (Iron Keep)
            PlaceTile(Tiles.TreeLarge, 85, 192, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 110, 186, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 86, 138, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 108, 138, _entityLayer, 5);

            // Territory 4 area trees (Outer Slums)
            PlaceTile(Tiles.TreeLarge, 4, 66, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 8, 72, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 26, 60, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 3, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 28, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 24, 66, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 7, 54, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 20, 18, _entityLayer, 5);

            // Territory 5 area trees (The Wilds)
            PlaceTile(Tiles.TreeLarge, 46, 66, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 50, 72, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 54, 66, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 58, 72, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 62, 60, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 66, 66, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 48, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 52, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 60, 12, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 68, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 45, 36, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 67, 42, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 49, 48, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 63, 30, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 55, 60, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 46, 24, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 64, 72, _entityLayer, 5);

            // Territory 6 area trees/props (Boneyard)
            PlaceTile(Tiles.Cross, 86, 18, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 90, 12, _entityLayer, 5);
            PlaceTile(Tiles.CrossLarge, 94, 18, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 88, 24, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 100, 12, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 104, 18, _entityLayer, 5);
            PlaceTile(Tiles.CrossLarge, 108, 12, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 110, 24, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 86, 60, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 102, 66, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 85, 72, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 110, 66, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 112, 12, _entityLayer, 5);

            // ================================================================
            // WILDERNESS CORRIDORS — Forest between territories
            // ================================================================

            // Horizontal corridor: Market Row ↔ Temple Ward (x:31-43, y:150-180)
            PlaceTile(Tiles.TreeLarge, 33, 174, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 36, 162, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 40, 180, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 34, 144, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 38, 156, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 42, 168, _entityLayer, 5);

            // Horizontal corridor: Temple Ward ↔ Iron Keep (x:71-83, y:150-180)
            PlaceTile(Tiles.TreeLarge, 73, 168, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 76, 156, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 80, 174, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 74, 144, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 78, 162, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 82, 150, _entityLayer, 5);

            // Vertical corridor: Market Row ↔ Outer Slums (x:10-20, y:76-131)
            PlaceTile(Tiles.TreeLarge, 6, 108, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 10, 90, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 14, 114, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 8, 126, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 18, 102, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 22, 84, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 4, 96, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 26, 120, _entityLayer, 5);

            // Vertical corridor: Temple Ward ↔ Wilds (x:50-60, y:76-131)
            PlaceTile(Tiles.TreeLarge, 48, 108, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 54, 90, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 60, 114, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 52, 126, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 58, 102, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 64, 84, _entityLayer, 5);

            // Vertical corridor: Iron Keep ↔ Boneyard (x:92-102, y:76-131)
            PlaceTile(Tiles.TreeLarge, 88, 108, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 94, 90, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 100, 114, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 90, 126, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 96, 102, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 104, 84, _entityLayer, 5);

            // Horizontal corridor: Outer Slums ↔ Wilds (x:31-43, y:30-54)
            PlaceTile(Tiles.TreeLarge, 33, 48, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 36, 36, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 40, 54, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 34, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 38, 30, _entityLayer, 5);

            // Horizontal corridor: Wilds ↔ Boneyard (x:71-83, y:30-54)
            PlaceTile(Tiles.TreeLarge, 73, 42, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 76, 30, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 80, 54, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 74, 18, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 78, 36, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 82, 24, _entityLayer, 5);

            // Center wilderness (x:31-83, y:78-129) — dense forest
            PlaceTile(Tiles.TreeLarge, 35, 96, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 40, 108, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 46, 90, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 50, 120, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 56, 96, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 62, 114, _entityLayer, 5);
            PlaceTile(Tiles.TreeMedium, 68, 102, _entityLayer, 5);
            PlaceTile(Tiles.TreeSmall, 72, 90, _entityLayer, 5);
            PlaceTile(Tiles.TreePine, 76, 120, _entityLayer, 5);
            PlaceTile(Tiles.TreeLarge, 80, 108, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 44, 102, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 58, 114, _entityLayer, 5);
            PlaceTile(Tiles.Mushroom, 70, 96, _entityLayer, 5);

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
            RegisterPalimpsestLayer(14, 156, new List<string> { "DEFLATE:0.2" }, radius: 6, turns: -1);
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
