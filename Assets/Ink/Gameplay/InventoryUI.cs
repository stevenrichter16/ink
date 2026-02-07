using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Text;

namespace InkSim
{
    /// <summary>
    /// Full-screen inventory UI with item selection and equip/use controls.
    /// Press I to toggle, arrows to navigate, E to equip, U to use.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        public PlayerController player;

        [Header("Colors")]
        public Color backgroundColor = new Color(0.02f, 0.05f, 0.08f, 0.98f);
        public Color borderColor = new Color(0.0f, 0.6f, 0.6f, 1f);
        public Color headerColor = new Color(0.0f, 0.8f, 0.8f, 1f);
        public Color textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        public Color selectedColor = new Color(1f, 0.9f, 0.4f, 1f);
        public Color equippedColor = new Color(0.4f, 0.9f, 0.4f, 1f);
        public Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        private bool _isOpen = false;
        public static bool IsOpen { get; private set; } = false;

        private GameObject _canvas;
        private Text _mainText;
        private Font _monoFont;
        private int _selectedIndex = 0;

        private void Start()
        {
            if (player == null)
                player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();

            _monoFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
            if (_monoFont == null)
                _monoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.iKey.wasPressedThisFrame)
            {
                _isOpen = !_isOpen;
                IsOpen = _isOpen;
                if (_isOpen) Open();
                else Close();
            }

            if (_isOpen)
            {
                HandleInput();
                RefreshDisplay();
            }
        }

        private void HandleInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || player == null) return;

            int itemCount = player.inventory?.items.Count ?? 0;

            // Navigation
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            {
                _selectedIndex--;
                if (_selectedIndex < 0) _selectedIndex = Mathf.Max(0, itemCount - 1);
            }
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            {
                _selectedIndex++;
                if (_selectedIndex >= itemCount) _selectedIndex = 0;
            }

            // Clamp selection
            if (itemCount == 0)
                _selectedIndex = 0;
            else if (_selectedIndex >= itemCount)
                _selectedIndex = itemCount - 1;

            // Actions
            if (itemCount > 0 && _selectedIndex < itemCount)
            {
                ItemInstance selected = player.inventory.items[_selectedIndex];

                // E = Equip
                if (kb.eKey.wasPressedThisFrame && selected.data.IsEquippable)
                {
                    player.equipment.Equip(selected, player.inventory);
                }

                // U = Use
                if (kb.uKey.wasPressedThisFrame && selected.data.IsUsable)
                {
                    player.UseItem(selected);
                }

                // D = Drop (spawn pickup at player position)
                if (kb.dKey.wasPressedThisFrame)
                {
                    // Remove from inventory
                    player.inventory.RemoveItem(selected.data.id, 1);
                    
                    // Spawn pickup at player's feet
                    var gridWorld = UnityEngine.Object.FindFirstObjectByType<GridWorld>();
                    float tileSize = gridWorld != null ? gridWorld.tileSize : 0.5f;
                    ItemPickup.CreateFromLoot(selected.data.id, player.gridX, player.gridY, 1, tileSize);
                    
                    Debug.Log($"[InventoryUI] Dropped {selected.data.id} at ({player.gridX}, {player.gridY})");
                }
            }

            // Unequip shortcuts
            if (kb.digit1Key.wasPressedThisFrame)
                player.equipment.Unequip(ItemType.Weapon, player.inventory);
            if (kb.digit2Key.wasPressedThisFrame)
                player.equipment.Unequip(ItemType.Armor, player.inventory);
            if (kb.digit3Key.wasPressedThisFrame)
                player.equipment.Unequip(ItemType.Accessory, player.inventory);
        }

        private void Open()
        {
            _canvas = new GameObject("InventoryCanvas");
            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _canvas.AddComponent<GraphicRaycaster>();

            // Full screen background
            GameObject bg = CreateImage("Background", _canvas.transform, backgroundColor);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Main text display
            GameObject textGO = new GameObject("MainText");
            textGO.transform.SetParent(bg.transform, false);

            _mainText = textGO.AddComponent<Text>();
            _mainText.font = _monoFont;
            _mainText.fontSize = 22;
            _mainText.color = textColor;
            _mainText.alignment = TextAnchor.UpperLeft;
            _mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _mainText.verticalOverflow = VerticalWrapMode.Overflow;
            _mainText.supportRichText = true;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(60, 60);
            textRect.offsetMax = new Vector2(-60, -60);

            RefreshDisplay();
        }

        private void Close()
        {
            if (_canvas != null)
                Destroy(_canvas);
            _canvas = null;
        }

        private void RefreshDisplay()
        {
            if (_mainText == null || player == null) return;

            StringBuilder sb = new StringBuilder();

            string cyan = ColorToHex(headerColor);
            string white = ColorToHex(textColor);
            string gold = ColorToHex(selectedColor);
            string green = ColorToHex(equippedColor);
            string dim = ColorToHex(dimColor);
            string border = ColorToHex(borderColor);

            // Title
            sb.AppendLine($"<color={cyan}>════════════════════════════════════════════════════════════</color>");
            sb.AppendLine($"<color={cyan}>                      ══ INVENTORY ══</color>");
            sb.AppendLine($"<color={cyan}>════════════════════════════════════════════════════════════</color>");
            sb.AppendLine();

            // Stats
            int baseAtk = player.levelable != null ? player.levelable.Atk : player.baseAttack;
            int baseDef = player.levelable != null ? player.levelable.Def : player.baseDefense;
            int baseHp = player.levelable != null ? player.levelable.MaxHp : player.baseMaxHealth;
            int level = player.levelable?.Level ?? 1;
            
            sb.AppendLine($"<color={cyan}>[ STATS ]</color>  <color={dim}>Level {level}</color>");
            sb.AppendLine($"  <color={white}>Health:</color>  <color={gold}>{player.currentHealth}</color> / <color={gold}>{player.MaxHealth}</color> <color={dim}>(base {baseHp} + equip {player.equipment?.TotalHealthBonus ?? 0})</color>");
            sb.AppendLine($"  <color={white}>Attack:</color>  <color={gold}>{player.AttackDamage}</color> <color={dim}>(base {baseAtk} + equip {player.equipment?.TotalAttackBonus ?? 0})</color>");
            sb.AppendLine($"  <color={white}>Defense:</color> <color={gold}>{player.Defense}</color> <color={dim}>(base {baseDef} + equip {player.equipment?.TotalDefenseBonus ?? 0})</color>");
            sb.AppendLine();

            // Equipment
            sb.AppendLine($"<color={cyan}>[ EQUIPMENT ]</color>  <color={dim}>(press 1/2/3 to unequip)</color>");
            sb.AppendLine($"  <color={white}>1. Weapon:</color>    {FormatEquipSlot(player.equipment?.weapon)}");
            sb.AppendLine($"  <color={white}>2. Armor:</color>     {FormatEquipSlot(player.equipment?.armor)}");
            sb.AppendLine($"  <color={white}>3. Accessory:</color> {FormatEquipSlot(player.equipment?.accessory)}");
            sb.AppendLine();

            // Inventory items
            sb.AppendLine($"<color={cyan}>[ ITEMS ]</color>  <color={dim}>(↑↓ select, E=equip, U=use, D=drop)</color>");

            if (player.inventory == null || player.inventory.items.Count == 0)
            {
                sb.AppendLine($"  <color={dim}>(empty)</color>");
            }
            else
            {
                for (int i = 0; i < player.inventory.items.Count; i++)
                {
                    ItemInstance item = player.inventory.items[i];
                    bool isSelected = (i == _selectedIndex);

                    string prefix = isSelected ? $"<color={gold}>▶ </color>" : "  ";
                    string itemColor = isSelected ? gold : white;
                    string qtyStr = item.data.stackable && item.quantity > 1 ? $" x{item.quantity}" : "";
                    string typeHint = GetTypeHint(item.data);

                    sb.AppendLine($"{prefix}<color={itemColor}>{item.Name}{qtyStr}</color>  <color={dim}>{typeHint}</color>");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"<color={dim}>────────────────────────────────────────────────────────────</color>");
            sb.AppendLine($"<color={dim}>                     Press [I] to close</color>");

            _mainText.text = sb.ToString();
        }

        private string FormatEquipSlot(ItemInstance item)
        {
            if (item == null)
                return $"<color={ColorToHex(dimColor)}>(empty)</color>";

            string green = ColorToHex(equippedColor);
            string bonus = "";

            if (item.data.attackBonus > 0) bonus += $" +{item.data.attackBonus} ATK";
            if (item.data.defenseBonus > 0) bonus += $" +{item.data.defenseBonus} DEF";
            if (item.data.healthBonus > 0) bonus += $" +{item.data.healthBonus} HP";

            return $"<color={green}>{item.Name}</color><color={ColorToHex(dimColor)}>{bonus}</color>";
        }

        private string GetTypeHint(ItemData data)
        {
            return data.type switch
            {
                ItemType.Weapon => "[weapon]",
                ItemType.Armor => "[armor]",
                ItemType.Accessory => "[accessory]",
                ItemType.Consumable => "[use]",
                ItemType.Currency => "",
                ItemType.KeyItem => "[key]",
                _ => ""
            };
        }

        private GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        private void OnDestroy()
        {
            Close();
        }
    }
}
