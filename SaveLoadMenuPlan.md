# Save/Load Menu Implementation Plan

## Goals
- Add a Tab-accessible menu dedicated to saving and loading with a single save slot.
- Keep gameplay input paused/ignored while the menu is open; resume cleanly when closed.
- Store saves in a human-readable, versioned format to allow future migrations.

## UX and Input
- Toggle: `Tab` opens/closes the menu; ignore if other full-screen UIs are open (e.g., inventory).
- Closing shortcuts: `Esc` or clicking outside the panel (optional) hides the menu.
- Visuals: overlay `Canvas` with a centered panel containing a title, one slot line, and buttons for `Save`, `Load`, and `Close`.
- Interactivity: use a `CanvasGroup` (`alpha`, `interactable`, `blocksRaycasts`) to show/hide without destroying UI.

## Scene Objects
- `SaveLoadMenu` (MonoBehaviour) attached to a root `GameObject`:
  - Creates/holds the overlay `Canvas` and `CanvasGroup`.
  - Builds the panel UI elements once in `Start`/`Awake`.
  - Exposes `Open()`, `Close()`, `Toggle()`; tracks `IsOpen` static flag for other systems.
- `SaveLoadController` (MonoBehaviour):
  - Polls `Keyboard.current.tabKey` in `Update`.
  - Skips toggling if `InventoryUI.IsOpen` (and any other modal UIs).
  - Pauses gameplay input by notifying relevant systems or checking `SaveLoadMenu.IsOpen` in input handlers.

## UI Layout (single slot)
- Title text: “Save / Load”.
- Slot row: status label (e.g., “Empty slot” or “Saved at HH:MM:SS, Day #”), and `Save` and `Load` buttons.
- State cues: disable `Load` button when no save file exists; show a short error label for failures.
- Optional confirm on overwrite: simple prompt or second click behavior.

## Save System Structure
- Static `SaveSystem` utility:
  - `const string SavePath = Application.persistentDataPath + "/save_slot1.json";`
  - Methods: `bool Save(GameState data)`, `bool TryLoad(out GameState data)`, `bool SaveExists()`.
  - Handles serialization/deserialization via `JsonUtility` or `System.Text.Json` (if allowed); wrap in try/catch and log errors.
- `GameState` DTO:
  - `int version`.
  - Player: position (grid coords), stats (HP, attack, defense), inventory (item ids + quantities), equipment (item ids in slots).
  - World: grid dimensions, walkable map (bitset/array), occupants (enemies/NPCs) with positions and current HP/state, item pickups on ground (id, quantity, position).
  - Turn manager state (whose turn, if needed).
- Versioning: increment `version` on format changes; when loading, validate and bail with user-facing message if incompatible.

## Data Collection (Save)
- Player snapshot:
  - Grid position and facing (if relevant).
  - Current health and base stats; include derived equipment bonuses if needed for restoration.
  - Inventory items list (id, quantity).
  - Equipped item ids for weapon/armor/accessory slots.
- World snapshot:
  - Walkable map from `GridWorld` (`bool[,]` → flatten to `byte[]` or string).
  - Entities: enemies/NPCs with type/lootTableId, grid position, current health/state.
  - Items on ground: pickup id, quantity, grid position.
- Meta:
  - Timestamp (`DateTime.UtcNow`), playtime if available; used for display in UI.

## Restoration (Load)
- Validate file exists and version matches; show UI error and keep menu open if not.
- Clear current world state safely:
  - Destroy existing entities/items; reset `GridWorld` occupancy and walkable map.
- Rebuild:
  - Restore walkable grid.
  - Spawn player at saved coords; set stats and equipment/inventory contents.
  - Spawn enemies/NPCs with saved health/state; register with `TurnManager`.
  - Spawn pickups and place them on the grid.
- Update systems:
  - Refresh UI elements (health, inventory, equipment).
  - Sync turn manager to a sane starting point (e.g., player turn).

## Menu Wiring
- `SaveLoadMenu` buttons:
  - `Save`: call `SaveSystem.Save(AssembleState())`; on success, update status label; on failure, show error text.
  - `Load`: call `TryLoad`; on success, `ApplyState`; on failure, show error and leave menu open.
  - `Close`: just `Close()`.
- Button interactable states reflect `SaveExists()` and in-flight operations (disable during save/load).

## Testing Checklist
- Fresh run: open menu, verify `Load` is disabled and status shows “Empty slot”.
- Save with default scene; restart play mode; load and confirm player/enemy/item positions match previous state.
- Save mid-combat/low HP; load and verify HP and enemy HP/state persist.
- Attempt load with corrupted file → shows error and does not crash.
- Tab toggle while inventory open → menu does not open; gameplay input resumes after closing menu.

## Future Extensions (not required now)
- Multiple slots with scroll/list UI.
- Async save/load to avoid frame hitching.
- Cloud or platform saves.
- Screenshot/thumbnail per slot.
