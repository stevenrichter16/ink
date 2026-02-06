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
                .Add("ink_vial", 0.15f, 1, 1) // slimes produce ink byproduct
            );

            // === SKELETON ===
            Register(new LootTable("skeleton", guaranteedDrops: 1, maxDrops: 3)
                .Add("coin", 0.9f, 2, 5)
                .Add("dagger", 0.15f, 1, 1)
                .Add("key", 0.05f, 1, 1)
                .Add("potion", 0.2f, 1, 1)
                .Add("bone_blade", 0.12f, 1, 1)
                .Add("bone_plate", 0.05f, 1, 1)
                .Add("skull_talisman", 0.03f, 1, 1)
            );

            // === DEMON ===
            Register(new LootTable("demon", guaranteedDrops: 2, maxDrops: 4)
                .Add("coin", 1.0f, 5, 10)
                .Add("potion", 0.5f, 1, 2)
                .Add("potion_large", 0.25f, 1, 1)
                .Add("iron_armor", 0.1f, 1, 1)
                .Add("sword", 0.1f, 1, 1)
                .Add("gem", 0.3f, 1, 2)
                .Add("hellfire_brand", 0.05f, 1, 1)
                .Add("demon_plate", 0.03f, 1, 1)
                .Add("brimstone", 0.2f, 1, 2)
                .Add("infernal_eye", 0.02f, 1, 1)
            );

            // === GHOST ===
            Register(new LootTable("ghost", guaranteedDrops: 1, maxDrops: 2)
                .Add("coin", 0.7f, 1, 3)
                .Add("soul_gem", 0.15f, 1, 1)
                .Add("spectral_blade", 0.05f, 1, 1)
                .Add("wraith_cloak", 0.03f, 1, 1)
            );

            // === SNAKE ===
            Register(new LootTable("snake", guaranteedDrops: 0, maxDrops: 2)
                .Add("coin", 0.6f, 1, 2)
                .Add("antivenom", 0.2f, 1, 1)
                .Add("venom_fang", 0.08f, 1, 1)
                .Add("snakeskin_vest", 0.03f, 1, 1)
            );

            // === GOBLIN ===
            Register(new LootTable("goblin", guaranteedDrops: 1, maxDrops: 3)
                .Add("coin", 0.85f, 2, 4)
                .Add("shiv", 0.15f, 1, 1)
                .Add("scrap_armor", 0.08f, 1, 1)
                .Add("lucky_tooth", 0.03f, 1, 1)
                .Add("potion", 0.15f, 1, 1)
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
