using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Handles spell input, validation, and casting.
    /// Listens for hotkeys (1, 2, 3...) and casts spells at cursor target.
    /// </summary>
    public class SpellSystem : MonoBehaviour
    {
        [Header("References")]
        public PlayerController player;
        public TileCursor tileCursor;
        public GridWorld gridWorld;
        
        [Header("Spells")]
        public List<SpellData> equippedSpells = new List<SpellData>();
        
        [Header("State")]
        public float[] cooldownTimers;
        public int selectedSpellIndex = 0;
        
        [Header("Debug")]
        public bool showRangeIndicator = true;
        
        private static SpellSystem _instance;
        public static SpellSystem Instance => _instance;
        
        void Awake()
        {
            _instance = this;
        }
        
        void Start()
        {
            // Auto-find references if not set
            if (player == null) player = FindObjectOfType<PlayerController>();
            if (tileCursor == null) tileCursor = FindObjectOfType<TileCursor>();
            if (gridWorld == null) gridWorld = GridWorld.Instance;
            
            // Initialize cooldown timers
            cooldownTimers = new float[equippedSpells.Count];
            
            // Load default spells if none equipped
            if (equippedSpells.Count == 0)
            {
                var fireball = Resources.Load<SpellData>("Spells/Fireball");
                if (fireball != null)
                    equippedSpells.Add(fireball);
                    
                var inkStream = Resources.Load<SpellData>("Spells/InkStream");
                if (inkStream != null)
                    equippedSpells.Add(inkStream);
                    
                cooldownTimers = new float[equippedSpells.Count];
            }
        }
        
        void Update()
        {
            // Update cooldowns
            for (int i = 0; i < cooldownTimers.Length; i++)
            {
                if (cooldownTimers[i] > 0)
                {
                    cooldownTimers[i] -= Time.deltaTime;
                }
            }
            
            // Check for spell hotkeys
            HandleSpellInput();
        }
        
private void HandleSpellInput()
        {
            // Don't cast if no cursor or player
            if (tileCursor == null || player == null) return;
            if (TileInfoPanel.IsOpen) return;
            
            var kb = Keyboard.current;
            if (kb == null) return;
            
            // Check number keys 1-9
            Key[] numberKeys = {
                Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
                Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
            };
            
            for (int i = 0; i < Mathf.Min(equippedSpells.Count, 9); i++)
            {
                if (kb[numberKeys[i]].wasPressedThisFrame)
                {
                    TryCastSpell(i, tileCursor.gridX, tileCursor.gridY);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Attempt to cast a spell at target tile
        /// </summary>
        public bool TryCastSpell(int spellIndex, int targetX, int targetY)
        {
            // Validate spell index
            if (spellIndex < 0 || spellIndex >= equippedSpells.Count)
            {
                Debug.LogWarning($"[SpellSystem] Invalid spell index: {spellIndex}");
                return false;
            }
            
            SpellData spell = equippedSpells[spellIndex];
            
            // Check cooldown
            if (cooldownTimers[spellIndex] > 0)
            {
                Debug.Log($"[SpellSystem] {spell.spellName} on cooldown: {cooldownTimers[spellIndex]:F1}s remaining");
                return false;
            }
            
            // Check range
            if (!spell.IsInRange(player.gridX, player.gridY, targetX, targetY))
            {
                Debug.Log($"[SpellSystem] Target out of range for {spell.spellName}");
                return false;
            }
            
            // Check self-targeting
            if (!spell.canTargetSelf && targetX == player.gridX && targetY == player.gridY)
            {
                Debug.Log($"[SpellSystem] Cannot target self with {spell.spellName}");
                return false;
            }
            
            // Check if targeting empty tile
            var occupant = gridWorld?.GetEntityAt(targetX, targetY);
            if (!spell.canTargetEmpty && occupant == null)
            {
                Debug.Log($"[SpellSystem] {spell.spellName} requires a target");
                return false;
            }
            
            // All checks passed - cast the spell!
            CastSpell(spell, spellIndex, targetX, targetY);
            return true;
        }
        
private void CastSpell(SpellData spell, int spellIndex, int targetX, int targetY)
        {
            Debug.Log($"[SpellSystem] Casting {spell.spellName} at ({targetX}, {targetY})");
            
            // Calculate tile size
            float tileSize = gridWorld?.tileSize ?? 0.5f;
            
            // Check line of sight and adjust target if blocked
            int finalTargetX = targetX;
            int finalTargetY = targetY;
            
            // Always check for projectile collision with walls
            var adjustedTarget = SpellUtils.GetAdjustedTarget(
                player.gridX, player.gridY,
                targetX, targetY,
                gridWorld
            );
            
            if (adjustedTarget.x != targetX || adjustedTarget.y != targetY)
            {
                Debug.Log($"[SpellSystem] Spell blocked! Adjusted target from ({targetX},{targetY}) to ({adjustedTarget.x},{adjustedTarget.y})");
                finalTargetX = adjustedTarget.x;
                finalTargetY = adjustedTarget.y;
            }
            
            // Check if completely blocked (target is player position)
            if (finalTargetX == player.gridX && finalTargetY == player.gridY)
            {
                Debug.Log($"[SpellSystem] Spell completely blocked - no valid path!");
                // Still consume cooldown but don't spawn projectile
                cooldownTimers[spellIndex] = spell.cooldown * 0.5f; // Half cooldown for failed cast
                return;
            }
            
            // Start cooldown
            cooldownTimers[spellIndex] = spell.cooldown;
            
            // Calculate world positions
            Vector3 startPos = new Vector3(player.gridX * tileSize, player.gridY * tileSize, 0);
            Vector3 targetPos = new Vector3(finalTargetX * tileSize, finalTargetY * tileSize, 0);
            
            // Spawn projectile based on spell type
            SpawnProjectile(spell, startPos, targetPos, finalTargetX, finalTargetY);
            
            // Consume player turn
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.PlayerActed();
            }
        }
        
private void SpawnProjectile(SpellData spell, Vector3 start, Vector3 target, int gridX, int gridY)
        {
            switch (spell.projectileType)
            {
                case ProjectileType.Fireball:
                    Fireball.Create(spell, start, target, gridX, gridY, player);
                    break;
                    
                case ProjectileType.InkStream:
                    var stream = InkStream.Create(spell, start, target, gridX, gridY, player);
                    // Configure stream-specific properties
                    stream.puddleChance = spell.puddleChance;
                    break;
                    
                default:
                    Debug.LogWarning($"[SpellSystem] Unknown projectile type: {spell.projectileType}");
                    Fireball.Create(spell, start, target, gridX, gridY, player);
                    break;
            }
        }
        
        /// <summary>
        /// Check if a spell is ready to cast
        /// </summary>
        public bool IsSpellReady(int spellIndex)
        {
            if (spellIndex < 0 || spellIndex >= cooldownTimers.Length) return false;
            return cooldownTimers[spellIndex] <= 0;
        }
        
        /// <summary>
        /// Get remaining cooldown for a spell
        /// </summary>
        public float GetCooldown(int spellIndex)
        {
            if (spellIndex < 0 || spellIndex >= cooldownTimers.Length) return 0;
            return Mathf.Max(0, cooldownTimers[spellIndex]);
        }
        
        /// <summary>
        /// Add a spell to equipped list
        /// </summary>
        public void EquipSpell(SpellData spell)
        {
            if (!equippedSpells.Contains(spell))
            {
                equippedSpells.Add(spell);
                // Resize cooldown array
                var newCooldowns = new float[equippedSpells.Count];
                cooldownTimers.CopyTo(newCooldowns, 0);
                cooldownTimers = newCooldowns;
            }
        }
    }
}
