using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Type of projectile to spawn for the spell
    /// </summary>
    public enum ProjectileType
    {
        Fireball,
        InkStream
    }
    
    /// <summary>
    /// ScriptableObject defining spell properties.
    /// Create via Assets > Create > InkSim > Spell Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpell", menuName = "InkSim/Spell Data")]
    public class SpellData : ScriptableObject
    {
        [Header("Identity")]
        public string spellName = "Unnamed Spell";
        public string description = "";
        public KeyCode hotkey = KeyCode.Alpha1;
        
        [Header("Combat")]
        public int damage = 5;
        public int manaCost = 0; // For future mana system
        public float cooldown = 1f;
        
        [Header("Targeting")]
        public int range = 8; // In tiles
        public bool requiresLineOfSight = false;
        public bool canTargetSelf = false;
        public bool canTargetEmpty = true; // Can target empty tiles
        
        [Header("Projectile")]
        public ProjectileType projectileType = ProjectileType.Fireball;
        public float projectileSpeed = 12f; // World units per second
        public float impactRadius = 0f; // 0 = single target, >0 = AoE
        
        [Header("Stream Properties (for Ink Stream type)")]
        public float puddleChance = 0.4f;
        public float puddleLifetime = 4f;
        public int puddleDamagePerTick = 1; // 0 = single target, >0 = AoE
        
        [Header("Visuals")]
        public Color primaryColor = new Color(1f, 0.5f, 0f); // Orange
        public Color secondaryColor = Color.yellow;
        public float projectileSize = 0.3f;
        
        /// <summary>
        /// Check if target is within range of caster
        /// </summary>
        public bool IsInRange(int casterX, int casterY, int targetX, int targetY)
        {
            int dx = Mathf.Abs(targetX - casterX);
            int dy = Mathf.Abs(targetY - casterY);
            // Chebyshev distance (allows diagonal)
            return Mathf.Max(dx, dy) <= range;
        }
    }
}
