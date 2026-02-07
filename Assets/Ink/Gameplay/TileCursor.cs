using UnityEngine;
using UnityEngine.InputSystem;

namespace InkSim
{
    /// <summary>
    /// Cursor that follows mouse and snaps to grid tiles.
    /// Shows white outline around hovered tile.
    /// </summary>
    public class TileCursor : MonoBehaviour
    {
        [Header("References")]
        public GridWorld gridWorld;
        
        [Header("Settings")]
        public float lineWidth = 0.02f;
        public Color normalColor = Color.white;
        public Color enemyColor = new Color(1f, 0.3f, 0.3f, 1f);
        public Color blockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Color itemColor = new Color(1f, 0.9f, 0.3f, 1f);

        [Header("State")]
        public int gridX;
        public int gridY;
        public bool isValid;

        private Camera _camera;
        private LineRenderer _line;
        private float _tileSize;
        private int _mapWidth;
        private int _mapHeight;

        private void Start()
        {
            _camera = Camera.main;
            
            if (gridWorld == null)
                gridWorld = UnityEngine.Object.FindFirstObjectByType<GridWorld>();

            if (gridWorld != null)
            {
                _tileSize = gridWorld.tileSize;
                _mapWidth = gridWorld.width;
                _mapHeight = gridWorld.height;
            }
            else
            {
                _tileSize = 0.5f;
                _mapWidth = 20;
                _mapHeight = 12;
            }

            CreateOutline();
        }

        private void CreateOutline()
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 4;
            _line.loop = true;
            _line.startWidth = lineWidth;
            _line.endWidth = lineWidth;
            _line.useWorldSpace = true;
            _line.sortingOrder = 100;

            // Create simple unlit material
            Material mat = new Material(Shader.Find("Sprites/Default"));
            _line.material = mat;
            _line.startColor = normalColor;
            _line.endColor = normalColor;
        }

        private void Update()
        {
            // Hide when inventory open
            if (InventoryUI.IsOpen)
            {
                _line.enabled = false;
                return;
            }
            _line.enabled = true;

            // Get mouse position
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 mouseWorld = _camera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0));

            // Snap to grid
            gridX = Mathf.RoundToInt(mouseWorld.x / _tileSize);
            gridY = Mathf.RoundToInt(mouseWorld.y / _tileSize);

            // Clamp to map bounds
            gridX = Mathf.Clamp(gridX, 0, _mapWidth - 1);
            gridY = Mathf.Clamp(gridY, 0, _mapHeight - 1);

            // Update outline position
            UpdateOutlinePosition();

            // Update color based on what's under cursor
            UpdateColor();
        }

        private void UpdateOutlinePosition()
        {
            float x = gridX * _tileSize;
            float y = gridY * _tileSize;
            float half = _tileSize * 0.5f;

            // Square corners (slightly inset for cleaner look)
            float inset = lineWidth * 0.5f;
            _line.SetPosition(0, new Vector3(x - half + inset, y - half + inset, 0));
            _line.SetPosition(1, new Vector3(x + half - inset, y - half + inset, 0));
            _line.SetPosition(2, new Vector3(x + half - inset, y + half - inset, 0));
            _line.SetPosition(3, new Vector3(x - half + inset, y + half - inset, 0));
        }

        private void UpdateColor()
        {
            Color color = normalColor;
            isValid = true;

            if (gridWorld != null)
            {
                // Check for entity
                GridEntity entity = gridWorld.GetEntityAt(gridX, gridY);
                
                if (entity != null)
                {
                    if (entity.entityType == GridEntity.EntityType.Enemy)
                        color = enemyColor;
                    else if (entity.entityType == GridEntity.EntityType.Player)
                        color = normalColor;
                }
                else if (!gridWorld.IsWalkable(gridX, gridY))
                {
                    color = blockedColor;
                    isValid = false;
                }
                
                // Check for item pickup
                ItemPickup pickup = FindPickupAt(gridX, gridY);
                if (pickup != null)
                    color = itemColor;
            }

            SetColor(color);
        }

private ItemPickup FindPickupAt(int x, int y)
        {
            // Use static registry if available (much faster than FindObjectsOfType)
            var pickups = ItemPickup.ActivePickups;
            if (pickups != null)
            {
                for (int i = 0; i < pickups.Count; i++)
                {
                    var pickup = pickups[i];
                    if (pickup != null && pickup.gridX == x && pickup.gridY == y)
                        return pickup;
                }
                return null;
            }
            
            // Fallback to slow method (shouldn't happen if ItemPickup has registry)
            foreach (var pickup in UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
            {
                if (pickup.gridX == x && pickup.gridY == y)
                    return pickup;
            }
            return null;
        }

        private void SetColor(Color color)
        {
            _line.startColor = color;
            _line.endColor = color;
        }

        /// <summary>
        /// Get the entity under the cursor.
        /// </summary>
        public GridEntity GetEntityUnderCursor()
        {
            return gridWorld?.GetEntityAt(gridX, gridY);
        }

        /// <summary>
        /// Get the item pickup under the cursor.
        /// </summary>
        public ItemPickup GetPickupUnderCursor()
        {
            return FindPickupAt(gridX, gridY);
        }
    }
}
