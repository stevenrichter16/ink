using UnityEngine;
using UnityEditor;

namespace InkSim
{
    /// <summary>
    /// Editor utility to fix build settings
    /// </summary>
    public static class BuildHelper
    {
        [MenuItem("Tools/Fix Build Settings (Add TestMap)")]
        public static void FixBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Ink/TestMap.unity", true)
            };
            
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[BuildHelper] Build settings updated! TestMap is now the only scene in build.");
        }
    }
}
