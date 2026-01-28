using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Static registry of all enemy definitions.
    /// Follows same pattern as ItemDatabase.
    /// </summary>
    public static class EnemyDatabase
    {
        private static Dictionary<string, EnemyData> _enemies;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the database with all enemy types.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _enemies = new Dictionary<string, EnemyData>();

            // === BASIC ENEMIES ===
            Register(new EnemyData("slime", "Slime", 24)
            {
                maxHealth = 3,
                attackDamage = 2,
                aggroRange = 4,
                attackRange = 1,
                xpOnKill = 10
            });

            Register(new EnemyData("snake", "Snake", 20)
            {
                maxHealth = 2,
                attackDamage = 2,
                aggroRange = 5,
                attackRange = 1,
                xpOnKill = 8
            });

            // === UNDEAD ===
            Register(new EnemyData("skeleton", "Skeleton", 27)
            {
                maxHealth = 4,
                attackDamage = 3,
                aggroRange = 6,
                attackRange = 1,
                xpOnKill = 15
            });

            Register(new EnemyData("ghost", "Ghost", 25)
            {
                maxHealth = 3,
                attackDamage = 2,
                aggroRange = 8,
                attackRange = 1,
                xpOnKill = 12
            });

            // === MONSTERS ===
            Register(new EnemyData("goblin", "Goblin", 26)
            {
                maxHealth = 4,
                attackDamage = 2,
                aggroRange = 6,
                attackRange = 1,
                xpOnKill = 12
            });

            Register(new EnemyData("demon", "Demon", 28)
            {
                maxHealth = 8,
                attackDamage = 5,
                aggroRange = 7,
                attackRange = 1,
                xpOnKill = 30
            });

            _initialized = true;
        }

        private static void Register(EnemyData enemy)
        {
            _enemies[enemy.id] = enemy;
        }

        /// <summary>
        /// Get enemy data by ID.
        /// </summary>
        public static EnemyData Get(string id)
        {
            if (!_initialized) Initialize();
            return _enemies.TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>
        /// Check if an enemy type exists.
        /// </summary>
        public static bool Exists(string id)
        {
            if (!_initialized) Initialize();
            return _enemies.ContainsKey(id);
        }

        /// <summary>
        /// Get all enemy IDs.
        /// </summary>
        public static IEnumerable<string> AllIds
        {
            get
            {
                if (!_initialized) Initialize();
                return _enemies.Keys;
            }
        }

        /// <summary>
        /// Get all enemy data.
        /// </summary>
        public static IEnumerable<EnemyData> All
        {
            get
            {
                if (!_initialized) Initialize();
                return _enemies.Values;
            }
        }
    }
}
