namespace InkSim
{
    /// <summary>
    /// Pure-logic helper for enemy ranged attack decisions.
    /// Separates the "should I use ranged?" decision from Unity dependencies.
    /// </summary>
    public static class EnemyRangedHelper
    {
        /// <summary>
        /// Returns true if the enemy should cast a ranged spell instead of meleeing/chasing.
        /// Prefers melee when adjacent — ranged is for targets beyond melee range but within spell range.
        /// </summary>
        public static bool ShouldUseRanged(bool hasSpell, int distToTarget, int attackRange, int spellRange)
        {
            if (!hasSpell) return false;
            if (distToTarget <= attackRange) return false; // melee is better when adjacent
            if (distToTarget > spellRange) return false;   // out of spell range
            return true;
        }
    }
}
