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
            
            // Spawn performance monitor
            if (FindObjectOfType<PerfMonitor>() == null)
                new GameObject("PerfMonitor").AddComponent<PerfMonitor>();
            
            // Spawn build diagnostic for debugging black screen
            if (FindObjectOfType<BuildDiagnostic>() == null)
                new GameObject("BuildDiagnostic").AddComponent<BuildDiagnostic>();
            
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
            // First try to load as Sprite (if textures imported as Sprite type)
            Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Tiles");
            Debug.Log($"[TestMapBuilder] Resources.LoadAll<Sprite> returned: {loadedSprites?.Length ?? 0}");
            
            // If no sprites found, try loading as Texture2D and create sprites
            if (loadedSprites == null || loadedSprites.Length == 0)
            {
                Debug.Log("[TestMapBuilder] No sprites found, trying Texture2D...");
                Texture2D[] textures = Resources.LoadAll<Texture2D>("Tiles");
                Debug.Log($"[TestMapBuilder] Resources.LoadAll<Texture2D> returned: {textures?.Length ?? 0}");
                
                if (textures == null || textures.Length == 0)
                {
                    Debug.LogError("[TestMapBuilder] No textures found in Resources/Tiles! Game cannot start.");
                    return;
                }
                
                // Separate tile_ prefixed textures from extras
                List<Texture2D> tileTextures = new List<Texture2D>();
                List<Texture2D> extraTextures = new List<Texture2D>();
                
                for (int i = 0; i < textures.Length; i++)
                {
                    if (textures[i].name.StartsWith("tile_"))
                        tileTextures.Add(textures[i]);
                    else
                        extraTextures.Add(textures[i]);
                }
                
                // Sort by name
                tileTextures.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
                extraTextures.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
                
                // Create sprites from textures
                _allSprites = new Sprite[tileTextures.Count + extraTextures.Count];
                
                for (int i = 0; i < tileTextures.Count; i++)
                {
                    var tex = tileTextures[i];
                    _allSprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, pixelsPerUnit);
                }
                
                for (int i = 0; i < extraTextures.Count; i++)
                {
                    var tex = extraTextures[i];
                    _allSprites[tileTextures.Count + i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, pixelsPerUnit);
                }
                
                Debug.Log($"[TestMapBuilder] Created {_allSprites.Length} sprites from textures ({tileTextures.Count} tiles + {extraTextures.Count} extras)");
                return;
            }
            
            // Sprites loaded directly - sort them
            List<Sprite> tileSprites = new List<Sprite>();
            List<Sprite> extraSprites = new List<Sprite>();
            
            for (int i = 0; i < loadedSprites.Length; i++)
            {
                if (loadedSprites[i].name.StartsWith("tile_"))
                    tileSprites.Add(loadedSprites[i]);
                else
                    extraSprites.Add(loadedSprites[i]);
            }
            
            tileSprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            extraSprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            
            _allSprites = new Sprite[tileSprites.Count + extraSprites.Count];
            for (int i = 0; i < tileSprites.Count; i++)
                _allSprites[i] = tileSprites[i];
            for (int i = 0; i < extraSprites.Count; i++)
                _allSprites[tileSprites.Count + i] = extraSprites[i];
            
            Debug.Log($"[TestMapBuilder] Loaded {_allSprites.Length} sprites from Resources ({tileSprites.Count} tiles + {extraSprites.Count} extras)");
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

            // Territory control service (lean loop)
            var territoryGO = new GameObject("DistrictControlService");
            territoryGO.AddComponent<DistrictControlService>();

            // Quick palimpsest test surface near the player spawn
            var palSurface = new GameObject("PalimpsestTestSurface");
            var surface = palSurface.AddComponent<InscribableSurface>();
            surface.radius = 6;
            surface.priority = 1;
            surface.turns = 8;
            surface.defaultTokens = new List<string> { "TRUCE", "ALLY:PLAYER" };
            surface.registerOnStart = true;
            palSurface.transform.position = new Vector3(8, 12, 0f);

            // Territory debug panel
            new GameObject("TerritoryDebugPanel").AddComponent<TerritoryDebugPanel>();
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
        
        // Define dungeon bounds (L-shaped building)
private bool IsInsideDungeon(int x, int y)
        {
            // Main room: x=18-38, y=5-14
            bool inMainRoom = x >= 18 && x <= 38 && y >= 5 && y <= 14;
            // Upper wing: x=28-38, y=14-22
            bool inUpperWing = x >= 28 && x <= 38 && y >= 14 && y <= 22;
            
            return inMainRoom || inUpperWing;
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
            
            // === TRAINING DUMMY ===
            CreateDummy(Tiles.Barrel, 10, 10);

            // Load factions
            var inkboundFaction = Resources.Load<FactionDefinition>("Factions/Inkbound");
            var inkguardFaction = Resources.Load<FactionDefinition>("Factions/Inkguard");
            var snakeSpecies = Resources.Load<SpeciesDefinition>("Species/Snake");

            // === NPCs ===
            // Merchant in forest clearing (left area with sign)
            CreateNPC(Tiles.NPC1, 6, 18, NpcAI.AIBehavior.Stationary, "general_store", inkboundFaction, "low");
            // Companion Inkbound nearby
            CreateNPC(Tiles.NPC1, 7, 18, NpcAI.AIBehavior.Stationary, null, inkboundFaction, "low");
            // Weaponsmith inside dungeon
            CreateNPC(Tiles.NPC2, 32, 8, NpcAI.AIBehavior.Stationary, "weaponsmith", inkguardFaction, "mid");
            
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
            
            // === GRAVEYARD (bottom right) ===
            PlaceTile(Tiles.Cross, 40, 4, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 42, 3, _entityLayer, 5);
            PlaceTile(Tiles.CrossLarge, 44, 5, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 41, 6, _entityLayer, 5);
            PlaceTile(Tiles.Cross, 45, 2, _entityLayer, 5);
            PlaceTile(Tiles.Tombstone, 43, 7, _entityLayer, 5);
            
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
            
            // === ENEMIES ===
            // Forest enemies (left/upper area)
            CreateEnemy(Tiles.Ghost, 5, 20, "ghost", 3);
            CreateEnemy(Tiles.Ghost, 8, 14, "ghost", 2);
            CreateEnemy(Tiles.Slime, 10, 22, "slime", 1);
            CreateEnemy(Tiles.Slime, 3, 8, "slime", 1);
            CreateEnemy(Tiles.Goblin, 12, 18, "skeleton", 2);
            CreateEnemy(Tiles.Snake, 9, 19, "snake", 1, snakeSpecies, null, "high");
            CreateEnemy(Tiles.Snake, 11, 21, "snake", 1, snakeSpecies, null, "high");
            CreateEnemy(Tiles.Snake, 13, 17, "snake", 1, snakeSpecies, null, "high");
            
            // Enemies near dungeon
            CreateEnemy(Tiles.Ghost, 15, 14, "ghost", 3);
            CreateEnemy(Tiles.Slime, 14, 6, "slime", 1);
            
            // Inside dungeon
            CreateEnemy(Tiles.Skeleton, 22, 8, "skeleton", 2);
            CreateEnemy(Tiles.Skeleton, 28, 12, "skeleton", 2);
            CreateEnemy(Tiles.Ghost, 34, 8, "skeleton", 3);
            CreateEnemy(Tiles.Ghost, 32, 19, "ghost", 4);
            
            // Graveyard skeletons
            CreateEnemy(Tiles.Skeleton, 42, 5, "skeleton", 2);
            CreateEnemy(Tiles.Skeleton, 45, 4, "skeleton", 3);
            CreateEnemy(Tiles.Ghost, 40, 8, "skeleton", 4);
            
            // === PICKUPABLE ITEMS ===
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
            healthUI.heartSprite = _allSprites[Tiles.HeartFull];
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
            // Load level profile (works in both editor and builds)
            levelable.profile = Resources.Load<LevelProfile>("Levels/DefaultLevelProfile");
            if (levelable.profile == null)
                Debug.LogWarning($"[TestMapBuilder] Could not load DefaultLevelProfile for enemy at ({x},{y})");
            levelable.SetLevel(level);

            EnemyAI enemy = enemyGO.AddComponent<EnemyAI>();
            enemy.gridX = x;
            enemy.gridY = y;
            enemy.levelable = levelable;
            // NOTE: Don't set currentHealth here - let FactionMember.ApplyRank() handle it
            // after stats are finalizednitialize from levelable
            enemy.lootTableId = lootTableId;
            enemy.enemyId = lootTableId;

            bool explicitSpecies = species != null;
            if (!explicitSpecies)
                species = EnemyFactory.GetDefaultSpeciesForEnemyId(lootTableId);

            if (species != null)
            {
                var speciesMember = enemyGO.GetComponent<SpeciesMember>() ?? enemyGO.AddComponent<SpeciesMember>();
                speciesMember.species = species;
            }

            var factionMember = enemyGO.AddComponent<FactionMember>();
            factionMember.faction = faction;
            factionMember.rankId = factionRankId;
            factionMember.applyLevelFromRank = faction != null || explicitSpecies;
            factionMember.ApplyRank();

            _gridWorld.SetOccupant(x, y, enemy);
        }

        private void CreateNPC(int tileIndex, int x, int y, NpcAI.AIBehavior behavior, string merchantId = null, FactionDefinition faction = null, string factionRankId = "low")
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

            if (faction != null)
            {
                var member = go.AddComponent<FactionMember>();
                member.faction = faction;
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
            
            // If no camera exists, create one
            if (cam == null)
            {
                Debug.Log("[TestMapBuilder] No Camera.main found, creating new camera");
                GameObject camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                
                // Add AudioListener
                camGO.AddComponent<AudioListener>();
            }
            else
            {
                Debug.Log($"[TestMapBuilder] Using existing camera: {cam.name}");
            }

            // Configure camera for 2D
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.12f, 0.18f);
            cam.nearClipPlane = -100f;  // Important for 2D - allow negative Z
            cam.farClipPlane = 100f;
            cam.cullingMask = -1; // Render all layers
            cam.depth = 0;
            
            // For URP - try to add Universal camera data if available
            var urpCamData = cam.GetComponent("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (urpCamData == null)
            {
                // Try to add it via reflection (works in URP projects)
                var urpCamType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpCamType != null)
                {
                    cam.gameObject.AddComponent(urpCamType);
                    Debug.Log("[TestMapBuilder] Added UniversalAdditionalCameraData");
                }
            }
            
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
                CameraController camController = cam.gameObject.GetComponent<CameraController>();
                if (camController == null)
                    camController = cam.gameObject.AddComponent<CameraController>();
                    
                camController.target = _player?.transform;
                camController.gridWorld = _gridWorld;
                camController.smoothSpeed = 8f;
                camController.deadZone = new Vector2(0.5f, 0.3f);
                camController.enableLookAhead = true;
                camController.lookAheadDistance = 0.8f;
                
                // Snap to player initially
                camController.SnapToTarget();
            }
            
            Debug.Log($"[TestMapBuilder] Camera setup complete: pos={cam.transform.position}, orthoSize={cam.orthographicSize}");
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
