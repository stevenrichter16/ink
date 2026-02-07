using UnityEngine;
using UnityEngine.Rendering;

namespace InkSim
{
    /// <summary>
    /// Diagnostic script to debug black screen issues.
    /// Add to scene or will be auto-spawned by TestMapBuilder.
    /// </summary>
    public class BuildDiagnostic : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("========== BUILD DIAGNOSTIC START ==========");
            
            // Check render pipeline
            Debug.Log($"[Diag] Render Pipeline: {GraphicsSettings.currentRenderPipeline?.name ?? "BUILT-IN (null)"}");
            Debug.Log($"[Diag] Color Space: {QualitySettings.activeColorSpace}");
            
            // Check camera
            Camera cam = Camera.main;
            Debug.Log($"[Diag] Camera.main: {(cam != null ? cam.name : "NULL!")}");
            
            if (cam != null)
            {
                Debug.Log($"[Diag] Camera position: {cam.transform.position}");
                Debug.Log($"[Diag] Camera ortho: {cam.orthographic}, size: {cam.orthographicSize}");
                Debug.Log($"[Diag] Camera clear: {cam.clearFlags}, bg: {cam.backgroundColor}");
                Debug.Log($"[Diag] Camera culling mask: {cam.cullingMask}");
                Debug.Log($"[Diag] Camera depth: {cam.depth}");
                
                // Check for URP camera data
                var urpData = cam.GetComponent("UniversalAdditionalCameraData");
                Debug.Log($"[Diag] URP Camera Data: {(urpData != null ? "Present" : "MISSING")}");
            }
            
            // Check sprites loaded
            Sprite[] sprites = Resources.LoadAll<Sprite>("Tiles");
            Debug.Log($"[Diag] Sprites in Resources/Tiles: {sprites?.Length ?? 0}");
            
            if (sprites != null && sprites.Length > 0)
            {
                Debug.Log($"[Diag] First sprite: {sprites[0].name}, texture: {sprites[0].texture?.name ?? "NULL"}");
            }
            
            // Check scene objects
            var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            Debug.Log($"[Diag] SpriteRenderers in scene: {renderers.Length}");
            
            if (renderers.Length > 0)
            {
                int withSprite = 0;
                int visible = 0;
                foreach (var r in renderers)
                {
                    if (r.sprite != null) withSprite++;
                    if (r.enabled && r.gameObject.activeInHierarchy) visible++;
                }
                Debug.Log($"[Diag] SpriteRenderers with sprite: {withSprite}");
                Debug.Log($"[Diag] SpriteRenderers visible: {visible}");
                
                // Sample first few
                for (int i = 0; i < Mathf.Min(3, renderers.Length); i++)
                {
                    var r = renderers[i];
                    Debug.Log($"[Diag] Renderer[{i}]: {r.name}, pos: {r.transform.position}, sprite: {r.sprite?.name ?? "NULL"}, sortOrder: {r.sortingOrder}");
                }
            }
            
            // Check GridWorld
            var gridWorld = GridWorld.Instance;
            Debug.Log($"[Diag] GridWorld: {(gridWorld != null ? $"{gridWorld.width}x{gridWorld.height}" : "NULL")}");
            
            // Check player
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            Debug.Log($"[Diag] Player: {(player != null ? $"at ({player.gridX}, {player.gridY})" : "NULL")}");
            
            Debug.Log("========== BUILD DIAGNOSTIC END ==========");
        }
    }
}
