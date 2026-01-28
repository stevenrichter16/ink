using UnityEngine;
using UnityEditor;

namespace InkSim
{
    /// <summary>
    /// Editor utility to fix texture import settings for tiles.
    /// CRITICAL: Enables Read/Write which is required for Sprite.Create() in builds.
    /// </summary>
    public static class TileImportFixer
    {
        [MenuItem("Tools/Fix Tile Import Settings")]
        public static void FixTileImports()
        {
            string folderPath = "Assets/Ink/Resources/Tiles";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            
            int fixedCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                
                if (importer != null)
                {
                    bool needsReimport = false;
                    
                    // Set to Sprite type
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        needsReimport = true;
                    }
                    
                    // CRITICAL: Enable Read/Write for Sprite.Create() to work in builds
                    if (!importer.isReadable)
                    {
                        importer.isReadable = true;
                        needsReimport = true;
                    }
                    
                    if (importer.spritePixelsPerUnit != 16)
                    {
                        importer.spritePixelsPerUnit = 16;
                        needsReimport = true;
                    }
                    
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        needsReimport = true;
                    }
                    
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        needsReimport = true;
                    }
                    
                    if (needsReimport)
                    {
                        importer.SaveAndReimport();
                        fixedCount++;
                    }
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"[TileImportFixer] Fixed {fixedCount} texture import settings in {folderPath} (including Read/Write enable)");
        }
    }
}
