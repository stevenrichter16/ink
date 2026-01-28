using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Text;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Full-screen merchant UI with Buy/Sell tabs.
    /// Tab to switch modes, arrows to navigate, Enter to confirm.
    /// </summary>
    public class MerchantUI : MonoBehaviour
    {
        public static MerchantUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }
        
        [Header("Colors")]
        public Color backgroundColor = new Color(0.02f, 0.05f, 0.08f, 0.98f);
        public Color borderColor = new Color(0.0f, 0.6f, 0.6f, 1f);
        public Color headerColor = new Color(0.0f, 0.8f, 0.8f, 1f);
        public Color textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        public Color selectedColor = new Color(1f, 0.9f, 0.4f, 1f);
        public Color affordColor = new Color(0.4f, 0.9f, 0.4f, 1f);
        public Color cantAffordColor = new Color(0.9f, 0.4f, 0.4f, 1f);
        public Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        private enum Tab { Buy, Sell }
        
        private Tab _currentTab = Tab.Buy;
        private int _selectedIndex = 0;
        private Merchant _merchant;
        private PlayerController _player;
        
        private GameObject _canvas;
        private Text _mainText;
        private Font _monoFont;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            _monoFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
            if (_monoFont == null)
                _monoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        private void Update()
        {
            if (!IsOpen) return;
            
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            
            // Close
            if (kb.escapeKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            
            // Tab switching
            if (kb.tabKey.wasPressedThisFrame)
            {
                _currentTab = _currentTab == Tab.Buy ? Tab.Sell : Tab.Buy;
                _selectedIndex = 0;
            }
            
            // Navigation
            int itemCount = GetCurrentItemCount();
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
            
            // Clamp
            if (itemCount == 0)
                _selectedIndex = 0;
            else if (_selectedIndex >= itemCount)
                _selectedIndex = itemCount - 1;
            
            // Confirm transaction
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
            {
                ExecuteTransaction();
            }
            
            RefreshDisplay();
        }
        
        /// <summary>
        /// Open the merchant UI.
        /// </summary>
        public static void Open(Merchant merchant, PlayerController player)
        {
            if (Instance == null)
            {
                var go = new GameObject("MerchantUI");
                Instance = go.AddComponent<MerchantUI>();
                Instance.Start();
            }
            
            Instance._merchant = merchant;
            Instance._player = player;
            Instance._currentTab = Tab.Buy;
            Instance._selectedIndex = 0;
            
            merchant.OnShopOpened();
            
            Instance.CreateCanvas();
            Instance.RefreshDisplay();
            
            IsOpen = true;
            Debug.Log($"[MerchantUI] Opened shop: {merchant.DisplayName}");
        }
        
        public void Close()
        {
            if (_canvas != null)
                Destroy(_canvas);
            _canvas = null;
            IsOpen = false;
            Debug.Log("[MerchantUI] Closed");
        }
        
        private void CreateCanvas()
        {
            if (_canvas != null) return;
            
            _canvas = new GameObject("MerchantCanvas");
            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            _canvas.AddComponent<GraphicRaycaster>();
            
            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(_canvas.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = backgroundColor;
            
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Text
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
        }
        
        private void RefreshDisplay()
        {
            if (_mainText == null || _merchant == null || _player == null) return;
            
            StringBuilder sb = new StringBuilder();
            
            string cyan = ColorToHex(headerColor);
            string white = ColorToHex(textColor);
            string gold = ColorToHex(selectedColor);
            string green = ColorToHex(affordColor);
            string red = ColorToHex(cantAffordColor);
            string dim = ColorToHex(dimColor);
            
            int coins = _player.inventory?.CountItem("coin") ?? 0;
            
            // Header
            sb.AppendLine($"<color={cyan}>════════════════════════════════════════════════════════════</color>");
            sb.AppendLine($"<color={cyan}>                    ══ {_merchant.DisplayName.ToUpper()} ══</color>");
            sb.AppendLine($"<color={cyan}>════════════════════════════════════════════════════════════</color>");
            sb.AppendLine();
            
            // Coins
            sb.AppendLine($"<color={white}>Your Coins:</color> <color={gold}>{coins}</color>");
            sb.AppendLine();
            
            // Tabs
            string buyTab = _currentTab == Tab.Buy ? $"<color={gold}>[ BUY ]</color>" : $"<color={dim}>[ BUY ]</color>";
            string sellTab = _currentTab == Tab.Sell ? $"<color={gold}>[ SELL ]</color>" : $"<color={dim}>[ SELL ]</color>";
            sb.AppendLine($"  {buyTab}    {sellTab}    <color={dim}>(Tab to switch)</color>");
            sb.AppendLine();
            sb.AppendLine($"<color={dim}>────────────────────────────────────────────────────────────</color>");
            sb.AppendLine();
            
            if (_currentTab == Tab.Buy)
            {
                RenderBuyTab(sb, coins, cyan, white, gold, green, red, dim);
            }
            else
            {
                RenderSellTab(sb, cyan, white, gold, green, dim);
            }
            
            // Footer
            sb.AppendLine();
            sb.AppendLine($"<color={dim}>────────────────────────────────────────────────────────────</color>");
            sb.AppendLine($"<color={dim}>  ↑↓ Select  |  Enter/E Confirm  |  Tab Switch  |  Esc Close</color>");
            
            _mainText.text = sb.ToString();
        }
        
        private void RenderBuyTab(StringBuilder sb, int coins, string cyan, string white, string gold, string green, string red, string dim)
        {
            sb.AppendLine($"<color={cyan}>[ MERCHANT STOCK ]</color>");
            sb.AppendLine();
            
            var stock = _merchant.Stock;
            if (stock.Count == 0)
            {
                sb.AppendLine($"  <color={dim}>(No items for sale)</color>");
                return;
            }
            
            for (int i = 0; i < stock.Count; i++)
            {
                var entry = stock[i];
                var data = ItemDatabase.Get(entry.itemId);
                if (data == null) continue;
                
                bool isSelected = (i == _selectedIndex);
                int price = _merchant.GetBuyPrice(entry.itemId);
                bool canAfford = coins >= price;
                
                string prefix = isSelected ? $"<color={gold}>▶ </color>" : "  ";
                string itemColor = isSelected ? gold : white;
                string priceColor = canAfford ? green : red;
                
                string itemName = data.name.Length > 20 ? data.name.Substring(0, 17) + "..." : data.name.PadRight(20);
                
                sb.AppendLine($"{prefix}<color={itemColor}>{itemName}</color> <color={dim}>x{entry.quantity}</color>  <color={priceColor}>{price} coins</color>");
            }
        }
        
        private void RenderSellTab(StringBuilder sb, string cyan, string white, string gold, string green, string dim)
        {
            sb.AppendLine($"<color={cyan}>[ YOUR ITEMS ]</color>");
            sb.AppendLine();
            
            var sellableItems = GetSellableItems();
            if (sellableItems.Count == 0)
            {
                sb.AppendLine($"  <color={dim}>(No items to sell)</color>");
                return;
            }
            
            for (int i = 0; i < sellableItems.Count; i++)
            {
                var item = sellableItems[i];
                bool isSelected = (i == _selectedIndex);
                int price = _merchant.GetSellPrice(item.Id);
                
                string prefix = isSelected ? $"<color={gold}>▶ </color>" : "  ";
                string itemColor = isSelected ? gold : white;
                
                string itemName = item.Name.Length > 20 ? item.Name.Substring(0, 17) + "..." : item.Name.PadRight(20);
                string qtyStr = item.quantity > 1 ? $" x{item.quantity}" : "";
                
                sb.AppendLine($"{prefix}<color={itemColor}>{itemName}</color><color={dim}>{qtyStr}</color>  <color={green}>+{price} coins</color>");
            }
        }
        
        private int GetCurrentItemCount()
        {
            if (_currentTab == Tab.Buy)
                return _merchant?.Stock.Count ?? 0;
            else
                return GetSellableItems().Count;
        }
        
        private List<ItemInstance> GetSellableItems()
        {
            var result = new List<ItemInstance>();
            if (_player?.inventory == null) return result;
            
            foreach (var item in _player.inventory.items)
            {
                if (MerchantService.CanSell(item.Id))
                    result.Add(item);
            }
            return result;
        }
        
        private void ExecuteTransaction()
        {
            if (_currentTab == Tab.Buy)
            {
                var stock = _merchant.Stock;
                if (_selectedIndex >= 0 && _selectedIndex < stock.Count)
                {
                    var entry = stock[_selectedIndex];
                    if (MerchantService.TryBuy(_merchant, _player, entry.itemId, 1))
                    {
                        // Success - item bought
                        // Adjust selection if item depleted
                        if (_selectedIndex >= stock.Count)
                            _selectedIndex = Mathf.Max(0, stock.Count - 1);
                    }
                }
            }
            else
            {
                var sellable = GetSellableItems();
                if (_selectedIndex >= 0 && _selectedIndex < sellable.Count)
                {
                    var item = sellable[_selectedIndex];
                    if (MerchantService.TrySell(_player, _merchant, item.Id, 1))
                    {
                        // Success - item sold
                        // Adjust selection
                        var newSellable = GetSellableItems();
                        if (_selectedIndex >= newSellable.Count)
                            _selectedIndex = Mathf.Max(0, newSellable.Count - 1);
                    }
                }
            }
        }
        
        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
        
        private void OnDestroy()
        {
            Close();
            if (Instance == this) Instance = null;
        }
    }
}
