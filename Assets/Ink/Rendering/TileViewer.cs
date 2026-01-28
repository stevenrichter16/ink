using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Editor utility to display tiles in a grid for visual inspection.
    /// Attach to a GameObject in a scene, hit Play, and screenshot.
    /// </summary>
    public class TileViewer : MonoBehaviour
    {
        public string tileFolderPath = "Assets/Ink/Tiles/Colored";
        public int tilesPerRow = 16;
        public float tileSize = 0.5f;
        public int startIndex = 0;
        public int maxTiles = 160;

        private void Start()
        {
            LoadAndDisplayTiles();
        }

        private void LoadAndDisplayTiles()
        {
            // Find all PNG files in the folder
            string fullPath = Path.Combine(Application.dataPath, tileFolderPath.Replace("Assets/", ""));
            if (!Directory.Exists(fullPath))
            {
                Debug.LogError($"Folder not found: {fullPath}");
                return;
            }

            string[] allFiles = Directory.GetFiles(fullPath, "*.png");
            List<string> tileFiles = new List<string>();
            List<string> extraFiles = new List<string>();

            for (int i = 0; i < allFiles.Length; i++)
            {
                string fileName = Path.GetFileName(allFiles[i]);
                if (fileName.StartsWith("tile_"))
                    tileFiles.Add(allFiles[i]);
                else
                    extraFiles.Add(allFiles[i]);
            }

            tileFiles.Sort();
            extraFiles.Sort();

            string[] files = new string[tileFiles.Count + extraFiles.Count];
            tileFiles.CopyTo(files, 0);
            extraFiles.CopyTo(files, tileFiles.Count);

            int count = Mathf.Min(files.Length - startIndex, maxTiles);
            Debug.Log($"[TileViewer] Loading {count} tiles from {tileFolderPath}");

            for (int i = 0; i < count; i++)
            {
                int fileIdx = startIndex + i;
                if (fileIdx >= files.Length) break;

                string file = files[fileIdx];
                byte[] data = File.ReadAllBytes(file);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);
                tex.filterMode = FilterMode.Point;

                // Create sprite
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, tex.width);

                // Create GameObject
                int col = i % tilesPerRow;
                int row = i / tilesPerRow;
                
                GameObject go = new GameObject($"tile_{fileIdx:D4}");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(col * tileSize, -row * tileSize, 0);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.transform.localScale = Vector3.one * tileSize / (tex.width / 16f); // Normalize to tileSize

                // Add index label
                GameObject label = new GameObject("label");
                label.transform.SetParent(go.transform);
                label.transform.localPosition = new Vector3(0, -tileSize * 0.4f, 0);
                TextMesh tm = label.AddComponent<TextMesh>();
                tm.text = fileIdx.ToString();
                tm.fontSize = 24;
                tm.characterSize = 0.05f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;
            }

            // Center camera
            Camera.main.orthographic = true;
            Camera.main.transform.position = new Vector3(tilesPerRow * tileSize * 0.5f - tileSize * 0.5f, -count / tilesPerRow * tileSize * 0.5f, -10);
            Camera.main.orthographicSize = Mathf.Max(count / tilesPerRow * tileSize * 0.6f, 3f);
            Camera.main.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        }
    }
}
