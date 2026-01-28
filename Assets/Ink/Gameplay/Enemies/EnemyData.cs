namespace InkSim
{
    /// <summary>
    /// Data template for an enemy type.
    /// Used by EnemyDatabase and EnemyFactory.
    /// </summary>
    public class EnemyData
    {
        public string id;
        public string displayName;
        public int tileIndex;
        public int maxHealth;
        public int attackDamage;
        public int aggroRange;
        public int attackRange;
        public string lootTableId;
        public int xpOnKill = 10;
        public int baseLevel = 1;

        public EnemyData(string id, string displayName, int tileIndex)
        {
            this.id = id;
            this.displayName = displayName;
            this.tileIndex = tileIndex;
            this.maxHealth = 3;
            this.attackDamage = 1;
            this.aggroRange = 6;
            this.attackRange = 1;
            this.lootTableId = id; // Default loot table matches enemy id
        }
    }
}
