using System;
using System.IO;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static utility for saving/loading game state to disk.
    /// Uses JSON format in Application.persistentDataPath.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILENAME = "save_slot1.json";
        
        public static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
        
        /// <summary>
        /// Check if a save file exists.
        /// </summary>
        public static bool SaveExists()
        {
            return File.Exists(SavePath);
        }
        
        /// <summary>
        /// Save game state to disk.
        /// </summary>
        public static bool Save(GameState state)
        {
            if (state == null)
            {
                Debug.LogError("[SaveSystem] Cannot save null state");
                return false;
            }
            
            try
            {
                // Update timestamps
                state.timestamp = DateTime.UtcNow.ToString("o");
                state.displayTimestamp = DateTime.Now.ToString("MMM dd, HH:mm:ss");
                
                // Serialize with pretty print for human-readability
                string json = JsonUtility.ToJson(state, prettyPrint: true);
                
                // Write to file
                File.WriteAllText(SavePath, json);
                
                Debug.Log($"[SaveSystem] Saved to: {SavePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load game state from disk.
        /// </summary>
        public static bool TryLoad(out GameState state)
        {
            state = null;
            
            if (!SaveExists())
            {
                Debug.LogWarning("[SaveSystem] No save file found");
                return false;
            }
            
            try
            {
                string json = File.ReadAllText(SavePath);
                state = JsonUtility.FromJson<GameState>(json);
                
                if (state == null)
                {
                    Debug.LogError("[SaveSystem] Failed to parse save file");
                    return false;
                }
                
                // Version check
                if (!state.IsCompatible())
                {
                    Debug.LogError($"[SaveSystem] Incompatible save version: {state.version} (current: {GameState.VERSION})");
                    state = null;
                    return false;
                }
                
                Debug.Log($"[SaveSystem] Loaded from: {SavePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get display timestamp without fully loading state.
        /// </summary>
        public static string GetSaveTimestamp()
        {
            if (!SaveExists()) return "";
            
            try
            {
                string json = File.ReadAllText(SavePath);
                var state = JsonUtility.FromJson<GameState>(json);
                return state?.displayTimestamp ?? "";
            }
            catch
            {
                return "Error reading save";
            }
        }
        
        /// <summary>
        /// Delete save file if it exists.
        /// </summary>
        public static void DeleteSave()
        {
            if (SaveExists())
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save deleted");
            }
        }
    }
}
