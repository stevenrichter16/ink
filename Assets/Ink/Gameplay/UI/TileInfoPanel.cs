using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace InkSim
{
    /// <summary>
    /// Popup panel showing info about clicked tile with action buttons.
    /// Left click to open, closes when cursor leaves both tile and panel.
    /// Uses object pooling for buttons to avoid allocation overhead.
    /// </summary>
    public class TileInfoPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        [Header("References")]
        public TileCursor cursor;
        public GridWorld gridWorld;

        [Header("Style")]
        public Color backgroundColor = new Color(0.02f, 0.05f, 0.08f, 0.95f);
        public Color borderColor = new Color(0.0f, 0.6f, 0.6f, 1f);
        public Color headerColor = new Color(0.0f, 0.8f, 0.8f, 1f);
        public Color textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        public Color statColor = new Color(1f, 0.9f, 0.4f, 1f);
        public Color buttonColor = new Color(0.1f, 0.3f, 0.3f, 1f);
        public Color buttonHoverColor = new Color(0.15f, 0.4f, 0.4f, 1f);

        [Header("Layout")]
        public Vector2 panelSize = new Vector2(400, 400);
        public Vector2 cursorOffset = new Vector2(15, -15);
        public float padding = 16f;
        public float buttonHeight = 32f;

        private bool _isOpen;
        private int _targetX, _targetY;
        private Vector2 _fixedPosition;
        private GameObject _canvas;
        private RectTransform _panelRect;
        private Text _contentText;
        private Transform _actionsContainer;
        private Font _monoFont;
        private List<TileAction> _actions = new List<TileAction>();
        private List<TileAction> _visibleActions = new List<TileAction>();
        private const int MaxHotkeys = 10; // 1-9 plus 0
        private List<ITileActionProvider> _providers = new List<ITileActionProvider>();

        // Object pooling for buttons and headers
        private List<PooledButton> _buttonPool = new List<PooledButton>();
        private List<PooledHeader> _headerPool = new List<PooledHeader>();
        private const int InitialButtonPoolSize = 20;
        private const int InitialHeaderPoolSize = 6;
        private Dictionary<ActionCategory, bool> _categoryExpanded = new Dictionary<ActionCategory, bool>();

        // Cached references for pooled objects
        private class PooledButton
        {
            public GameObject gameObject;
            public Button button;
            public Text text;
            public TileAction currentAction;
        }

        private class PooledHeader
        {
            public GameObject gameObject;
            public Text text;
            public Button button;
            public ActionCategory category;
        }

        public enum ActionCategory
        {
            Combat,
            SpawnEnemy,
            SpawnItem,
            Movement,
            Debug
        }

        public class TileAction
        {
            public string label;
            public ActionCategory category;
            public int priority;
            public Action<int, int> execute;
            public Func<int, int, bool> isAvailable;

            public TileAction(string label, ActionCategory category, Action<int, int> execute, Func<int, int, bool> isAvailable = null, int priority = 0)
            {
                this.label = label;
                this.category = category;
                this.priority = priority;
                this.execute = execute;
                this.isAvailable = isAvailable ?? ((x, y) => true);
            }
        }

        private void Start()
        {
            if (cursor == null)
                cursor = FindObjectOfType<TileCursor>();
            if (gridWorld == null)
                gridWorld = FindObjectOfType<GridWorld>();

            _monoFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
            if (_monoFont == null)
                _monoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_monoFont == null)
                _monoFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _providers.Add(new CombatActionProvider());
            _providers.Add(new DialogueActionProvider());
            _providers.Add(new MerchantActionProvider());
            _providers.Add(new SpawnActionProvider());
            _providers.Add(new MovementActionProvider());
            _providers.Add(new FactionChangeActionProvider());
            _providers.Add(new DebugActionProvider());

            // Default all categories to collapsed except Combat
            foreach (ActionCategory cat in Enum.GetValues(typeof(ActionCategory)))
                _categoryExpanded[cat] = (cat == ActionCategory.Combat);

            CollectActions();
        }

        private void CollectActions()
        {
            _actions.Clear();

            foreach (var provider in _providers)
            {
                _actions.AddRange(provider.GetActions(gridWorld));
            }

            _actions.Sort((a, b) => {
                int catCompare = a.category.CompareTo(b.category);
                return catCompare != 0 ? catCompare : a.priority.CompareTo(b.priority);
            });

            Debug.Log($"[TileInfoPanel] Collected {_actions.Count} actions from {_providers.Count} providers");
        }

        private void Update()
        {
            if (InventoryUI.IsOpen)
            {
                if (_isOpen) Hide();
                return;
            }

            Mouse mouse = Mouse.current;
            Keyboard kb = Keyboard.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                // If we clicked on UI (e.g., a TileInfo button), let the UI handle it
                if (IsPointerOverUI())
                    return;

                if (_isOpen && cursor.gridX == _targetX && cursor.gridY == _targetY)
                {
                    Hide();
                    return;
                }
                
                Show(cursor.gridX, cursor.gridY);
                return;
            }

            if (kb != null && kb.escapeKey.wasPressedThisFrame && _isOpen)
            {
                Hide();
                return;
            }

            if (_isOpen)
            {
                bool cursorOnTile = (cursor.gridX == _targetX && cursor.gridY == _targetY);
                bool cursorOnPanel = IsCursorOverPanel();

                if (!cursorOnTile && !cursorOnPanel)
                {
                    Hide();
                }

                HandleHotkeyInput();
            }
        }

        private bool IsCursorOverPanel()
        {
            if (_panelRect == null) return false;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 localPoint;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _panelRect, mousePos, null, out localPoint);

            return _panelRect.rect.Contains(localPoint);
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void Show(int x, int y)
        {
            if (_monoFont == null)
            {
                _monoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_monoFont == null)
                    _monoFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            _targetX = x;
            _targetY = y;

            EnsurePanel();

            _contentText.text = GetTileInfo(x, y);
            RefreshActions();

            Canvas.ForceUpdateCanvases();

            _fixedPosition = CalculatePanelPosition();

            // Clamp to screen so the panel never bleeds off-screen
            float margin = 10f;
            float w = _panelRect.rect.width;
            float h = _panelRect.rect.height;
            _fixedPosition.x = Mathf.Clamp(_fixedPosition.x, margin, Screen.width - w - margin);
            // pivot is (0,1): y is top edge; keep top below top margin and bottom above bottom margin
            _fixedPosition.y = Mathf.Clamp(_fixedPosition.y, h + margin, Screen.height - margin);

            _panelRect.position = _fixedPosition;

            _canvas.SetActive(true);
            _isOpen = true;
            IsOpen = true;
        }

        private void Hide()
        {
            if (_canvas != null)
                _canvas.SetActive(false);
            _isOpen = false;
            IsOpen = false;
        }

        private Vector2 CalculatePanelPosition()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 offset = cursorOffset;

            if (mousePos.x + offset.x + panelSize.x > Screen.width)
                offset.x = -panelSize.x - 15;
            if (mousePos.y + offset.y < panelSize.y)
                offset.y = panelSize.y + 15;

            return mousePos + offset;
        }

        private void CreatePanel()
        {
            _canvas = new GameObject("TileInfoCanvas");
            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            CanvasScaler scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _canvas.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_canvas.transform, false);

            Image bgImage = panel.AddComponent<Image>();
            bgImage.color = backgroundColor;

            _panelRect = panel.GetComponent<RectTransform>();
            _panelRect.sizeDelta = new Vector2(panelSize.x, 0);
            _panelRect.pivot = new Vector2(0, 1);

            ContentSizeFitter panelFitter = panel.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2, -2);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject textGO = new GameObject("Content");
            textGO.transform.SetParent(panel.transform, false);

            _contentText = textGO.AddComponent<Text>();
            _contentText.font = _monoFont;
            _contentText.fontSize = 22;
            _contentText.color = textColor;
            _contentText.alignment = TextAnchor.UpperLeft;
            _contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _contentText.verticalOverflow = VerticalWrapMode.Overflow;
            _contentText.supportRichText = true;

            textGO.AddComponent<LayoutElement>();
            ContentSizeFitter textFitter = textGO.AddComponent<ContentSizeFitter>();
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(panel.transform, false);
            Image sepImage = separator.AddComponent<Image>();
            sepImage.color = borderColor;
            LayoutElement sepLayout = separator.AddComponent<LayoutElement>();
            sepLayout.minHeight = 2;
            sepLayout.preferredHeight = 2;
            sepLayout.flexibleHeight = 0;

            GameObject actionsLabel = new GameObject("ActionsLabel");
            actionsLabel.transform.SetParent(panel.transform, false);
            Text labelText = actionsLabel.AddComponent<Text>();
            labelText.font = _monoFont;
            labelText.fontSize = 18;
            labelText.color = headerColor;
            labelText.text = "Actions:";
            LayoutElement labelLayout = actionsLabel.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 24;

            GameObject actionsGO = new GameObject("Actions");
            actionsGO.transform.SetParent(panel.transform, false);

            VerticalLayoutGroup actionsLayout = actionsGO.AddComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 4;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = false;

            LayoutElement actionsLayoutElem = actionsGO.AddComponent<LayoutElement>();
            actionsLayoutElem.flexibleHeight = 0;

            ContentSizeFitter actionsFitter = actionsGO.AddComponent<ContentSizeFitter>();
            actionsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _actionsContainer = actionsGO.transform;
        }

        private void EnsurePanel()
        {
            if (_canvas == null)
            {
                CreatePanel();
                InitializeButtonPool();
                InitializeHeaderPool();
            }
            else if (_actionsContainer == null)
            {
                CreatePanel();
            }
        }

        #region Object Pooling

        private void InitializeButtonPool()
        {
            for (int i = 0; i < InitialButtonPoolSize; i++)
            {
                var pooled = CreatePooledButton();
                pooled.gameObject.SetActive(false);
                _buttonPool.Add(pooled);
            }
        }

        private void InitializeHeaderPool()
        {
            for (int i = 0; i < InitialHeaderPoolSize; i++)
            {
                var pooled = CreatePooledHeader();
                pooled.gameObject.SetActive(false);
                _headerPool.Add(pooled);
            }
        }

        private bool IsCategoryExpanded(ActionCategory category)
        {
            if (_categoryExpanded.TryGetValue(category, out var expanded))
                return expanded;
            return false;
        }

        private void ToggleCategory(ActionCategory category)
        {
            bool current = IsCategoryExpanded(category);
            _categoryExpanded[category] = !current;
        }

        private PooledButton CreatePooledButton()
        {
            GameObject btnGO = new GameObject("PooledButton");
            btnGO.transform.SetParent(_actionsContainer, false);

            Image btnBg = btnGO.AddComponent<Image>();
            btnBg.color = buttonColor;
            btnBg.raycastTarget = true;

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonHoverColor;
            btn.colors = colors;
            btn.targetGraphic = btnBg;

            LayoutElement layoutElem = btnGO.AddComponent<LayoutElement>();
            layoutElem.minHeight = buttonHeight;
            layoutElem.preferredHeight = buttonHeight;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);

            Text btnText = textGO.AddComponent<Text>();
            btnText.font = _monoFont;
            btnText.fontSize = 18;
            btnText.color = statColor;
            btnText.alignment = TextAnchor.MiddleLeft;
            btnText.raycastTarget = false;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            return new PooledButton
            {
                gameObject = btnGO,
                button = btn,
                text = btnText,
                currentAction = null
            };
        }

        private PooledHeader CreatePooledHeader()
        {
            GameObject headerGO = new GameObject("PooledHeader");
            headerGO.transform.SetParent(_actionsContainer, false);

            var img = headerGO.AddComponent<Image>();
            img.color = new Color(0f, 0.2f, 0.2f, 0.6f);
            var btn = headerGO.AddComponent<Button>();

            // Child text (Graphic cannot share with Image on same GO)
            var textGO = new GameObject("Label");
            textGO.transform.SetParent(headerGO.transform, false);
            Text headerLabel = textGO.AddComponent<Text>();
            headerLabel.font = _monoFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerLabel.fontSize = 16;
            headerLabel.color = headerColor;
            headerLabel.alignment = TextAnchor.MiddleLeft;

            RectTransform rt = headerGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 0);
            textRT.offsetMax = new Vector2(-10, 0);

            LayoutElement layout = headerGO.AddComponent<LayoutElement>();
            layout.minHeight = 28;
            layout.preferredHeight = 28;

            return new PooledHeader
            {
                gameObject = headerGO,
                text = headerLabel,
                button = btn,
                category = ActionCategory.Combat
            };
        }

        private PooledButton GetOrCreateButton(int index)
        {
            while (index >= _buttonPool.Count)
            {
                var pooled = CreatePooledButton();
                pooled.gameObject.SetActive(false);
                _buttonPool.Add(pooled);
            }
            return _buttonPool[index];
        }

        private PooledHeader GetOrCreateHeader(int index)
        {
            while (index >= _headerPool.Count)
            {
                var pooled = CreatePooledHeader();
                pooled.gameObject.SetActive(false);
                _headerPool.Add(pooled);
            }
            return _headerPool[index];
        }

        private void ConfigureButton(PooledButton pooled, TileAction action, int hotkey)
        {
            pooled.currentAction = action;

            string hotkeyPrefix = "";
            if (hotkey >= 0)
            {
                string hk = hotkey == 0 ? "0" : hotkey.ToString();
                hotkeyPrefix = $"[{hk}] ";
            }
            pooled.text.text = $"{hotkeyPrefix}\u25b6 {action.label}";
            
            // Clear old listeners and add new one
            pooled.button.onClick.RemoveAllListeners();
            
            // Capture values for closure
            int x = _targetX;
            int y = _targetY;
            var actionRef = action;
            pooled.button.onClick.AddListener(() => {
                ExecuteAction(actionRef, x, y);
            });

            pooled.gameObject.SetActive(true);
            pooled.gameObject.transform.SetAsLastSibling();
        }

        private void ConfigureHeader(PooledHeader pooled, ActionCategory category)
        {
            pooled.category = category;
            bool expanded = IsCategoryExpanded(category);
            string label = category switch
            {
                ActionCategory.Combat => "⚔ Combat",
                ActionCategory.SpawnEnemy => "👾 Spawn Enemy",
                ActionCategory.SpawnItem => "📦 Spawn Item",
                ActionCategory.Movement => "🚶 Movement",
                ActionCategory.Debug => "🔧 Debug",
                _ => category.ToString()
            };
            pooled.text.text = $"{(expanded ? "[-] " : "[+] ")}{label}";

            pooled.button.onClick.RemoveAllListeners();
            pooled.button.onClick.AddListener(() =>
            {
                ToggleCategory(category);
                RefreshActions();
            });

            pooled.gameObject.SetActive(true);
            pooled.gameObject.transform.SetAsLastSibling();
        }

        #endregion

        private void RefreshActions()
        {
            EnsurePanel();
            if (_actionsContainer == null) return;

            // Deactivate all pooled objects (no Destroy!)
            for (int i = 0; i < _buttonPool.Count; i++)
                _buttonPool[i].gameObject.SetActive(false);
            for (int i = 0; i < _headerPool.Count; i++)
                _headerPool[i].gameObject.SetActive(false);

            _visibleActions.Clear();
            ActionCategory? lastCategory = null;
            int buttonIndex = 0;
            int headerIndex = 0;
            int hotkeyIndex = 0;

            foreach (var action in _actions)
            {
                bool available = action.isAvailable(_targetX, _targetY);
                if (!available) continue;

                // Add category header if category changed
                if (action.category != lastCategory)
                {
                    var header = GetOrCreateHeader(headerIndex++);
                    ConfigureHeader(header, action.category);
                    lastCategory = action.category;
                }

                // Skip actions for collapsed categories
                if (!IsCategoryExpanded(action.category))
                    continue;

                // Configure button from pool
                _visibleActions.Add(action);
                int hotkey;
                if (hotkeyIndex < MaxHotkeys)
                {
                    // Map the 10th action to key 0
                    hotkey = hotkeyIndex == 9 ? 0 : hotkeyIndex + 1;
                }
                else
                {
                    hotkey = -1;
                }
                var btn = GetOrCreateButton(buttonIndex++);
                ConfigureButton(btn, action, hotkey);
                hotkeyIndex++;
            }
        }

        private void ExecuteAction(TileAction action, int x, int y)
        {
            action.execute(x, y);
            Hide();
        }

        private void HandleHotkeyInput()
        {
            if (!_isOpen || _visibleActions.Count == 0) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            Key[] hotkeyKeys = {
                Key.Digit1, Key.Digit2, Key.Digit3,
                Key.Digit4, Key.Digit5, Key.Digit6,
                Key.Digit7, Key.Digit8, Key.Digit9,
                Key.Digit0
            };

            for (int i = 0; i < hotkeyKeys.Length && i < _visibleActions.Count; i++)
            {
                if (keyboard[hotkeyKeys[i]].wasPressedThisFrame)
                {
                    var action = _visibleActions[i];
                    ExecuteAction(action, _targetX, _targetY);
                    return;
                }
            }

            // Dedicated 0-key lookup for Stats even if it wasn't assigned a hotkey slot
            if (keyboard[Key.Digit0].wasPressedThisFrame)
            {
                for (int i = 0; i < _visibleActions.Count; i++)
                {
                    if (string.Equals(_visibleActions[i].label, "Stats", StringComparison.OrdinalIgnoreCase))
                    {
                        ExecuteAction(_visibleActions[i], _targetX, _targetY);
                        return;
                    }
                }
            }
        }

        private string GetTileInfo(int x, int y)
        {
            StringBuilder sb = new StringBuilder();
            string cyan = ColorToHex(headerColor);
            string white = ColorToHex(textColor);
            string gold = ColorToHex(statColor);

            GridEntity entity = gridWorld?.GetEntityAt(x, y);
            ItemPickup pickup = FindPickupAt(x, y);

            bool addedContent = false;

            if (entity != null)
            {
                addedContent = true;
                if (entity is PlayerController player)
                    AppendPlayerInfo(sb, player, cyan, white, gold);
                else if (entity is EnemyAI enemy)
                {
                    AppendEnemyInfo(sb, enemy, cyan, white, gold);
                    AppendFactionInfo(sb, enemy.gameObject, cyan, white, gold);
                }
                else if (entity is NpcAI npc)
                {
                    AppendNpcInfo(sb, npc, cyan, white, gold);
                    AppendFactionInfo(sb, npc.gameObject, cyan, white, gold);
                }
                else if (entity is AttackDummy)
                {
                    sb.AppendLine($"<color={cyan}>Training Dummy</color>");
                    sb.AppendLine($"<color={white}>───────────────────</color>");
                    sb.AppendLine($"<color={white}>Invincible target</color>");
                }
                else
                {
                    sb.AppendLine($"<color={cyan}>Unknown Entity</color>");
                    sb.AppendLine($"<color={white}>Type: {entity.GetType().Name}</color>");
                    AppendFactionInfo(sb, entity.gameObject, cyan, white, gold);
                }
            }

            if (pickup != null)
            {
                if (addedContent) sb.AppendLine();
                addedContent = true;
                AppendItemInfo(sb, pickup, cyan, white, gold);
            }

            if (!addedContent)
            {
                if (gridWorld != null && !gridWorld.IsWalkable(x, y))
                {
                    sb.AppendLine($"<color={cyan}>Wall</color>");
                    sb.AppendLine($"<color={white}>───────────────────</color>");
                    sb.AppendLine($"<color={white}>Impassable</color>");
                }
                else
                {
                    sb.AppendLine($"<color={cyan}>Floor</color>");
                    sb.AppendLine($"<color={white}>───────────────────</color>");
                    sb.AppendLine($"<color={white}>Empty tile</color>");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"<color=#666666>Position: ({x}, {y})</color>");

            return sb.ToString();
        }

        private void AppendPlayerInfo(StringBuilder sb, PlayerController player, string cyan, string white, string gold)
        {
            sb.AppendLine($"<color={cyan}>Player</color>");
            sb.AppendLine($"<color={white}>───────────────────</color>");
            sb.AppendLine($"<color={white}>Health:</color>  <color={gold}>{player.currentHealth}/{player.MaxHealth}</color>");
            sb.AppendLine($"<color={white}>Attack:</color>  <color={gold}>{player.AttackDamage}</color> <color=#666666>({player.baseAttack}+{player.equipment?.TotalAttackBonus ?? 0})</color>");
            sb.AppendLine($"<color={white}>Defense:</color> <color={gold}>{player.Defense}</color> <color=#666666>({player.baseDefense}+{player.equipment?.TotalDefenseBonus ?? 0})</color>");

            if (player.equipment != null)
            {
                if (player.equipment.weapon != null)
                    sb.AppendLine($"<color={white}>Weapon:</color>  <color={gold}>{player.equipment.weapon.Name}</color>");
                if (player.equipment.armor != null)
                    sb.AppendLine($"<color={white}>Armor:</color>   <color={gold}>{player.equipment.armor.Name}</color>");
                if (player.equipment.accessory != null)
                    sb.AppendLine($"<color={white}>Ring:</color>    <color={gold}>{player.equipment.accessory.Name}</color>");
            }
        }

        private void AppendEnemyInfo(StringBuilder sb, EnemyAI enemy, string cyan, string white, string gold)
        {
            string displayName = enemy.lootTableId ?? "Enemy";
            displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
            
            int level = enemy.levelable?.Level ?? 1;

            sb.AppendLine($"<color={cyan}>{displayName}</color>  <color=#666666>Lv.{level}</color>");
            sb.AppendLine($"<color={white}>───────────────────</color>");
            sb.AppendLine($"<color={white}>Health:</color> <color={gold}>{enemy.currentHealth}/{enemy.maxHealth}</color>");
            sb.AppendLine($"<color={white}>Damage:</color> <color={gold}>{enemy.attackDamage}</color>");
            sb.AppendLine($"<color={white}>State:</color>  <color={gold}>{enemy.state}</color>");

            if (!string.IsNullOrEmpty(enemy.lootTableId))
            {
                var enemyData = EnemyDatabase.Get(enemy.enemyId ?? enemy.lootTableId);
                if (enemyData != null)
                {
                    sb.AppendLine($"<color={white}>XP:</color>     <color={gold}>{enemyData.xpOnKill}</color>");
                }
                
                var table = LootDatabase.Get(enemy.lootTableId);
                if (table != null && table.entries.Count > 0)
                {
                    sb.Append($"<color={white}>Drops:</color>  <color=#666666>");
                    for (int i = 0; i < Mathf.Min(3, table.entries.Count); i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(table.entries[i].itemId);
                    }
                    if (table.entries.Count > 3) sb.Append("...");
                    sb.AppendLine("</color>");
                }
            }
        }

        private void AppendNpcInfo(StringBuilder sb, NpcAI npc, string cyan, string white, string gold)
        {
            string displayName = "NPC";
            int level = npc.GetComponent<Levelable>()?.Level ?? 1;

            sb.AppendLine($"<color={cyan}>{displayName}</color>  <color=#666666>Lv.{level}</color>");
            sb.AppendLine($"<color={white}>───────────────────</color>");
            sb.AppendLine($"<color={white}>Health:</color> <color={gold}>{npc.currentHealth}/{npc.maxHealth}</color>");
            sb.AppendLine($"<color={white}>Damage:</color> <color={gold}>{npc.attackDamage}</color>");
        }

        private void AppendFactionInfo(StringBuilder sb, GameObject entityGO, string cyan, string white, string gold)
        {
            var factionMember = entityGO.GetComponent<FactionMember>();
            if (factionMember == null || factionMember.faction == null) return;

            var faction = factionMember.faction;
            var rank = faction.GetRank(factionMember.rankId);
            int playerRep = ReputationSystem.GetRep(faction.id);

            string standingColor;
            string standingLabel;
            if (playerRep >= 25)
            {
                standingColor = "#44FF44";
                standingLabel = "Friendly";
            }
            else if (playerRep <= -25)
            {
                standingColor = "#FF4444";
                standingLabel = "Hostile";
            }
            else
            {
                standingColor = "#AAAAAA";
                standingLabel = "Neutral";
            }

            sb.AppendLine();
            sb.AppendLine($"<color={cyan}>── Faction ──</color>");
            sb.AppendLine($"<color={white}>Faction:</color>  <color={gold}>{faction.displayName}</color>");
            
            if (rank != null)
                sb.AppendLine($"<color={white}>Rank:</color>     <color={gold}>{rank.displayName}</color>");
            
            sb.AppendLine($"<color={white}>Standing:</color> <color={standingColor}>{standingLabel}</color> <color=#666666>({playerRep:+#;-#;0})</color>");
        }

        private void AppendItemInfo(StringBuilder sb, ItemPickup pickup, string cyan, string white, string gold)
        {
            var data = ItemDatabase.Get(pickup.itemId);
            if (data == null)
            {
                sb.AppendLine($"<color={cyan}>Unknown Item</color>");
                return;
            }

            sb.AppendLine($"<color={cyan}>{data.name}</color>");
            sb.AppendLine($"<color={white}>───────────────────</color>");
            sb.AppendLine($"<color={white}>Type:</color>   <color={gold}>{data.type}</color>");

            if (pickup.quantity > 1)
                sb.AppendLine($"<color={white}>Qty:</color>    <color={gold}>{pickup.quantity}</color>");

            if (data.attackBonus > 0)
                sb.AppendLine($"<color={white}>Attack:</color> <color={gold}>+{data.attackBonus}</color>");
            if (data.defenseBonus > 0)
                sb.AppendLine($"<color={white}>Defense:</color><color={gold}>+{data.defenseBonus}</color>");
            if (data.healthBonus > 0)
                sb.AppendLine($"<color={white}>Health:</color> <color={gold}+{data.healthBonus}</color>");
            if (data.healAmount > 0)
                sb.AppendLine($"<color={white}>Heals:</color>  <color={gold}>{data.healAmount} HP</color>");
            if (data.value > 0)
                sb.AppendLine($"<color={white}>Value:</color>  <color={gold}>{data.value}</color>");
        }

        private ItemPickup FindPickupAt(int x, int y)
        {
            var pickups = ItemPickup.ActivePickups;
            if (pickups != null)
            {
                for (int i = 0; i < pickups.Count; i++)
                {
                    var pickup = pickups[i];
                    if (pickup != null && pickup.gridX == x && pickup.gridY == y)
                        return pickup;
                }
            }
            return null;
        }

        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas);
        }
    }
}
