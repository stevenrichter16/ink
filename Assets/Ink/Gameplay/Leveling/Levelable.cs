using System;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Manages level, XP, and derived stats for any entity.
    /// Attach to Player or Enemies. Subscribe to events for UI updates.
    /// </summary>
    public class Levelable : MonoBehaviour
    {
        [Header("Profile")]
        public LevelProfile profile;
        
        [Header("Current State")]
        [SerializeField] private int _level = 1;
        [SerializeField] private int _xp = 0;
        
        // Computed stats (updated on level change)
        public int MaxHp { get; private set; }
        public int Atk { get; private set; }
        public int Def { get; private set; }
        public int Spd { get; private set; }
        public int XpToNextLevel { get; private set; }
        
        // Public accessors
        public int Level => _level;
        public int Xp => _xp;
        public float XpProgress => XpToNextLevel > 0 ? (float)_xp / XpToNextLevel : 0f;
        
        // Events
        public event Action<int, int> OnXpChanged;      // (currentXp, xpToNext)
        public event Action<int> OnLevelUp;             // (newLevel)
        public event Action OnStatsChanged;             // Fired after any stat recalc
        
        private void Start()
        {
            RecomputeStats();
        }
        
        /// <summary>
        /// Recalculate all stats from profile and current level.
        /// </summary>
        public void RecomputeStats()
        {
            if (profile == null)
            {
                Debug.LogWarning($"[Levelable] {gameObject.name} has no LevelProfile assigned!");
                // Fallback with linear scaling
            MaxHp = 100 + 20 * (_level - 1);
            Atk = 10 + 2 * (_level - 1);
            Def = 5 + 1 * (_level - 1);
            Spd = 5 + 1 * (_level - 1);
            XpToNextLevel = 50 + 50 * _level;
                
                OnStatsChanged?.Invoke();
                return;
            }
            
            MaxHp = profile.GetMaxHp(_level);
            Atk = profile.GetAtk(_level);
            Def = profile.GetDef(_level);
            Spd = profile.GetSpd(_level);
            XpToNextLevel = profile.GetXpToNextLevel(_level);
            
            OnStatsChanged?.Invoke();
        }
        
        /// <summary>
        /// Add XP and level up if threshold reached.
        /// </summary>
        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            
            _xp += amount;
            Debug.Log($"[Levelable] {gameObject.name} gained {amount} XP ({_xp}/{XpToNextLevel})");
            
            // Check for level up (can level multiple times)
            while (_xp >= XpToNextLevel)
            {
                _xp -= XpToNextLevel;
                LevelUp();
            }
            
            OnXpChanged?.Invoke(_xp, XpToNextLevel);
        }
        
        /// <summary>
        /// Increment level and recalculate stats.
        /// </summary>
        private void LevelUp()
        {
            _level++;
            RecomputeStats();
            
            Debug.Log($"[Levelable] {gameObject.name} leveled up to {_level}! HP:{MaxHp} ATK:{Atk} DEF:{Def}");
            
            OnLevelUp?.Invoke(_level);
        }
        
        /// <summary>
        /// Set level directly (for loading or enemy scaling).
        /// </summary>
        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            _xp = 0;
            RecomputeStats();
        }
        
        /// <summary>
        /// Set XP directly (for loading saves).
        /// </summary>
        public void SetXp(int xp)
        {
            _xp = Mathf.Max(0, xp);
            OnXpChanged?.Invoke(_xp, XpToNextLevel);
        }
        
        /// <summary>
        /// Set both level and XP (for loading saves).
        /// </summary>
        public void SetLevelAndXp(int level, int xp)
        {
            _level = Mathf.Max(1, level);
            _xp = Mathf.Max(0, xp);
            RecomputeStats();
            OnXpChanged?.Invoke(_xp, XpToNextLevel);
        }
    }
}
