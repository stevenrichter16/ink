using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InkSim
{
    /// <summary>
    /// Editor utility to create default LevelProfile assets.
    /// </summary>
    public static class LevelProfileCreator
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/InkSim/Default Level Profile")]
        public static void CreateDefaultProfile()
        {
            LevelProfile profile = ScriptableObject.CreateInstance<LevelProfile>();
            
            // Balanced profile for player, enemies, and NPCs
            profile.baseHp = 20;
            profile.baseAtk = 5;
            profile.baseDef = 2;
            
            profile.hpPerLevel = 10;
            profile.atkPerLevel = 2;
            profile.defPerLevel = 1;
            
            profile.baseXpToLevel = 25;
            profile.xpPerLevel = 25;
            
            string path = "Assets/Ink/Data/Levels/DefaultLevelProfile.asset";
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;
            
            Debug.Log($"[LevelProfileCreator] Created DefaultLevelProfile at {path}");
        }
#endif
    }
}
