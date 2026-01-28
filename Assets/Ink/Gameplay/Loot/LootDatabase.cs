using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Static registry of all loot tables.
    /// Call Initialize() at game start.
    /// </summary>
    public static class LootDatabase
    {
        private static Dictionary<string, LootTable> _tables;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the database with all loot tables.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _tables = new Dictionary<string, LootTable>();

            // === SLIME ===
            Register(new LootTable("slime", guaranteedDrops: 0, maxDrops: 2)
                .Add("coin", 0.8f, 1, 3)
                .Add("potion", 0.1f, 1, 1)
            );

            // === SKELETON ===
            Register(new LootTable("skeleton", guaranteedDrops: 1, maxDrops: 3)
                .Add("coin", 0.9f, 2, 5)
                .Add("dagger", 0.15f, 1, 1)
                .Add("key", 0.05f, 1, 1)
                .Add("potion", 0.2f, 1, 1)
            );

            // === DEMON ===
            Register(new LootTable("demon", guaranteedDrops: 2, maxDrops: 4)
                .Add("coin", 1.0f, 5, 10)
                .Add("potion", 0.5f, 1, 2)
                .Add("potion_large", 0.25f, 1, 1)
                .Add("iron_armor", 0.1f, 1, 1)
                .Add("sword", 0.1f, 1, 1)
                .Add("gem", 0.3f, 1, 2)
            );

            // === GENERIC (fallback) ===
            Register(new LootTable("generic", guaranteedDrops: 0, maxDrops: 1)
                .Add("coin", 0.5f, 1, 2)
            );

            _initialized = true;
        }

        private static void Register(LootTable table)
        {
            _tables[table.id] = table;
        }

        /// <summary>
        /// Get a loot table by ID.
        /// </summary>
        public static LootTable Get(string id)
        {
            if (!_initialized) Initialize();
            
            if (string.IsNullOrEmpty(id))
                return _tables.TryGetValue("generic", out var generic) ? generic : null;
                
            return _tables.TryGetValue(id, out var table) ? table : null;
        }

        /// <summary>
        /// Check if a loot table exists.
        /// </summary>
        public static bool Exists(string id)
        {
            if (!_initialized) Initialize();
            return _tables.ContainsKey(id);
        }
    }
}
