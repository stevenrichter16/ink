using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Factory for spawning enemies at runtime.
    /// Handles GameObject creation, component setup, and grid registration.
    /// </summary>
    public static class EnemyFactory
    {
        private const string DefaultSpeciesId = "species_default";
        private static readonly Dictionary<string, SpeciesDefinition> _defaultSpeciesByEnemyId = new Dictionary<string, SpeciesDefinition>();

        public static SpeciesDefinition GetDefaultSpeciesForEnemyId(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return null;

            if (_defaultSpeciesByEnemyId.TryGetValue(enemyId, out var cached))
                return cached;

            string resourceName = ToResourceName(enemyId);
            SpeciesDefinition species = string.IsNullOrEmpty(resourceName)
                ? null
                : Resources.Load<SpeciesDefinition>($"Species/{resourceName}");
            _defaultSpeciesByEnemyId[enemyId] = species;
            return species;
        }

        private static string ToResourceName(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;

            string[] parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Concat(parts);
        }

        private static void ApplyDefaultSpecies(GameObject go, string enemyId)
        {
            if (go == null) return;

            var speciesMember = go.GetComponent<SpeciesMember>() ?? go.AddComponent<SpeciesMember>();
            if (speciesMember.species != null && speciesMember.species.id != DefaultSpeciesId)
                return;

            var defaultSpecies = GetDefaultSpeciesForEnemyId(enemyId);
            if (defaultSpecies != null)
                speciesMember.species = defaultSpecies;
                speciesMember.EnsureDefaultFaction();
        }
        /// <summary>
        /// Spawn an enemy by type ID at the given grid position.
        /// </summary>
        /// <param name="enemyId">Enemy type ID from EnemyDatabase</param>
        /// <param name="x">Grid X position</param>
        /// <param name="y">Grid Y position</param>
        /// <param name="gridWorld">Optional GridWorld reference (will find if null)</param>
        /// <returns>The spawned EnemyAI, or null if failed</returns>
        public static EnemyAI Spawn(string enemyId, int x, int y, GridWorld gridWorld = null, int level = 1, FactionDefinition faction = null, string factionRankId = null)
        {
            var data = EnemyDatabase.Get(enemyId);
            if (data == null)
            {
                Debug.LogWarning($"[EnemyFactory] Unknown enemy type: {enemyId}");
                return null;
            }

            return SpawnFromData(data, x, y, gridWorld, level, faction, factionRankId);
        }

        /// <summary>
        /// Spawn an enemy from data template.
        /// </summary>
        public static EnemyAI SpawnFromData(EnemyData data, int x, int y, GridWorld gridWorld = null, int level = 1, FactionDefinition faction = null, string factionRankId = null)
        {
            if (data == null) return null;

            // Get GridWorld if not provided
            if (gridWorld == null)
                gridWorld = Object.FindObjectOfType<GridWorld>();

            if (gridWorld == null)
            {
                Debug.LogWarning("[EnemyFactory] No GridWorld found");
                return null;
            }

            // Check if position is valid
            if (!gridWorld.IsWalkable(x, y))
            {
                Debug.LogWarning($"[EnemyFactory] Cannot spawn at ({x}, {y}) - not walkable");
                return null;
            }

            if (gridWorld.GetEntityAt(x, y) != null)
            {
                Debug.LogWarning($"[EnemyFactory] Cannot spawn at ({x}, {y}) - occupied");
                return null;
            }

            // Get sprite
            Sprite sprite = SpriteLibrary.Instance?.GetSprite(data.tileIndex);
            if (sprite == null)
            {
                Debug.LogWarning($"[EnemyFactory] No sprite for enemy: {data.id} (tile {data.tileIndex})");
                return null;
            }

            // Create GameObject
            GameObject go = new GameObject($"Enemy_{data.id}_{x}_{y}");
            go.transform.localPosition = new Vector3(x * gridWorld.tileSize, y * gridWorld.tileSize, 0);

            // Add SpriteRenderer
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            // Add and configure EnemyAI
            EnemyAI enemy = go.AddComponent<EnemyAI>();
            
            // Add Levelable for stat scaling
            Levelable levelable = go.AddComponent<Levelable>();
            #if UNITY_EDITOR
            levelable.profile = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelProfile>("Assets/Ink/Data/Levels/DefaultLevelProfile.asset");
            #endif
            levelable.SetLevel(level > 0 ? level : data.baseLevel);
            enemy.levelable = levelable;
            
            enemy.gridX = x;
            enemy.gridY = y;
            enemy.aggroRange = data.aggroRange;
            enemy.attackRange = data.attackRange;
            enemy.lootTableId = data.lootTableId;
            enemy.enemyId = data.id;

            ApplyDefaultSpecies(go, data.id);

            // Apply faction/rank (defaults to species-based faction when none provided)
            var member = go.AddComponent<FactionMember>();
            member.faction = faction;
            if (!string.IsNullOrEmpty(factionRankId))
                member.rankId = factionRankId;
            member.applyLevelFromRank = faction != null;
            member.ApplyRank();

            // Register with world
            gridWorld.SetOccupant(x, y, enemy);
            TurnManager.Instance?.RegisterEnemy(enemy);

            Debug.Log($"[EnemyFactory] Spawned {data.displayName} at ({x}, {y})");
            return enemy;
        }

        /// <summary>
        /// Spawn a random enemy from the database.
        /// </summary>
        public static EnemyAI SpawnRandom(int x, int y, GridWorld gridWorld = null, int level = 1)
        {
            var allIds = new System.Collections.Generic.List<string>(EnemyDatabase.AllIds);
            if (allIds.Count == 0) return null;

            string randomId = allIds[Random.Range(0, allIds.Count)];
            return Spawn(randomId, x, y, gridWorld, level);
        }
    }
}
