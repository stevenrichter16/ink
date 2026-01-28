using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Defines level progression stats and XP requirements.
    /// Assign to Levelable components on players/enemies.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelProfile", menuName = "InkSim/Level Profile")]
    public class LevelProfile : ScriptableObject
    {
        [Header("Base Stats (Level 1)")]
        public int baseHp = 100;
        public int baseAtk = 10;
        public int baseDef = 5;
        
        [Header("Growth Per Level")]
        public int hpPerLevel = 20;
        public int atkPerLevel = 2;
        public int defPerLevel = 1;
        
        [Header("XP Curve (Linear)")]
        [Tooltip("XP needed = baseXpToLevel + (xpPerLevel * level)")]
        public int baseXpToLevel = 50;
        public int xpPerLevel = 50;
        
        /// <summary>
        /// Calculate max HP at a given level.
        /// </summary>
        public int GetMaxHp(int level)
        {
            return baseHp + (hpPerLevel * (level - 1));
        }
        
        /// <summary>
        /// Calculate attack at a given level.
        /// </summary>
        public int GetAtk(int level)
        {
            return baseAtk + (atkPerLevel * (level - 1));
        }
        
        /// <summary>
        /// Calculate defense at a given level.
        /// </summary>
        public int GetDef(int level)
        {
            return baseDef + (defPerLevel * (level - 1));
        }
        
        /// <summary>
        /// Calculate XP required to reach next level.
        /// Linear: 100, 150, 200, 250... (with defaults)
        /// </summary>
        public int GetXpToNextLevel(int currentLevel)
        {
            return baseXpToLevel + (xpPerLevel * currentLevel);
        }
    }
}
