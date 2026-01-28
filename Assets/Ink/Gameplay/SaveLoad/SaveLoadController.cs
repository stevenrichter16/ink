using UnityEngine;
using UnityEngine.InputSystem;

namespace InkSim
{
    /// <summary>
    /// Handles Tab key for Save/Load menu toggle.
    /// Forces auto-save at game start.
    /// Triggers auto-load on player death.
    /// </summary>
    public class SaveLoadController : MonoBehaviour
    {
        [Header("References")]
        public SaveLoadMenu menu;
        
        [Header("Settings")]
        public bool autoSaveOnStart = true;
        public bool autoLoadOnDeath = true;
        
        private PlayerController _player;
        private bool _wasPlayerDead;
        
        private void Start()
        {
            // Find or create menu
            if (menu == null)
            {
                menu = FindObjectOfType<SaveLoadMenu>();
                if (menu == null)
                {
                    GameObject menuGO = new GameObject("SaveLoadMenu");
                    menu = menuGO.AddComponent<SaveLoadMenu>();
                }
            }
            
            _player = FindObjectOfType<PlayerController>();
            
            // Auto-save at game start (guarantees save exists for death reload)
            if (autoSaveOnStart)
            {
                // Delay one frame to ensure everything is initialized
                Invoke(nameof(AutoSaveOnStart), 0.1f);
            }
        }
        
        private void AutoSaveOnStart()
        {
            if (GameStateManager.QuickSave())
            {
                Debug.Log("[SaveLoadController] Auto-saved at game start");
            }
            else
            {
                Debug.LogWarning("[SaveLoadController] Auto-save failed at game start");
            }
        }
        
        private void Update()
        {
            HandleInput();
            CheckPlayerDeath();
        }
        
        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            
            // Tab toggles menu (unless inventory is open)
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                if (!InventoryUI.IsOpen)
                {
                    menu.Toggle();
                }
            }
            
            // Escape closes menu if open
            if (keyboard.escapeKey.wasPressedThisFrame && SaveLoadMenu.IsOpen)
            {
                menu.Hide();
            }
        }
        
        private void CheckPlayerDeath()
        {
            if (!autoLoadOnDeath || _player == null) return;
            
            bool isDead = _player.currentHealth <= 0;
            
            // Detect transition to dead state
            if (isDead && !_wasPlayerDead)
            {
                Debug.Log("[SaveLoadController] Player died - auto-loading");
                
                // Delay load slightly to let death effects play
                Invoke(nameof(AutoLoadOnDeath), 0.5f);
            }
            
            _wasPlayerDead = isDead;
        }
        
        private void AutoLoadOnDeath()
        {
            if (SaveSystem.SaveExists())
            {
                if (GameStateManager.QuickLoad())
                {
                    Debug.Log("[SaveLoadController] Auto-loaded after death");
                    
                    // Re-find player reference after load
                    _player = FindObjectOfType<PlayerController>();
                    _wasPlayerDead = false;
                }
                else
                {
                    Debug.LogError("[SaveLoadController] Auto-load failed after death");
                }
            }
            else
            {
                Debug.LogWarning("[SaveLoadController] No save file - cannot auto-load after death");
            }
        }
    }
}
