# Ledger (Faction Reputation Book) — Design Plan

## Purpose
- Give the player a diegetic “book” they can open with **B** to view and adjust faction reputation toward the player.
- Primary use: flip a hostile faction to neutral/friendly for testing or narrative beats.
- Keep it clear, large, and quick to operate; no buried controls.

## UX / Flow
1. Player presses **B** → Ledger overlay appears (pauses player input; optional world pause).
2. Left column: list of factions (scrollable if >8). Each entry shows:
   - Faction display name
   - Current rep value (numeric) with color chip (Hostile/Neutral/Friendly)
3. Selecting a faction populates the right pane with controls:
   - Big name + emblem (if available)
   - Current reputation, both number and status word
   - Three preset buttons: **Friendly (+50)**, **Neutral (0)**, **Hostile (-50)**
   - Slider: range -100 .. +100 with tick marks every 25
   - Apply button (commits slider value)
   - Revert button (reloads current value from ReputationSystem)
4. Footer: Close button + “Press B or Esc to close”

## Visual Design (large & readable)
- Canvas overlay, dark 80% backdrop.
- Panel centered; min width 1200px, height 720px on 1080p; scales with anchors for lower resolutions.
- Fonts: use your monospaced legacy font for numbers; a clean sans (LegacyRuntime.ttf) for labels.
- Text sizes:
  - Title: 32–36pt bold
  - Section headers: 24pt
  - Body / list items: 18–20pt
  - Buttons: 20–22pt
- Spacing: 12–16px padding on panels; 8px between controls; 20px between columns.
- Color cues:
  - Hostile rep (< -25): #D9534F (red)
  - Neutral: #F0AD4E (amber)
  - Friendly (> 25): #5CB85C (green)
  - Slider track: neutral gray; handle adopts the status color.
- Buttons large (min 140×44), with hover highlight and pressed state.
- Scrollbars: thick enough to grab; always visible on the faction list.

## Data & Logic
- Source factions via `Resources.LoadAll<FactionDefinition>("Factions")`.
- Read rep via `ReputationSystem.GetRep(faction.id)`.
- Write rep via a new setter `ReputationSystem.SetRep(faction.id, value)` (clamp -100..100). If already present, reuse.
- Hostility rules already check reputation in `HostilityService`; no extra cache needed.
- When Apply is pressed:
  - Set rep
  - Optionally toast: “Set Ghosts to Friendly (+50)”
  - If the Ledger was opened in combat, effects should be immediate (no turn delay).
- Revert reloads the current stored rep.

## Input Handling
- Toggle open/close with **B**; also close with **Esc** or Close button.
- While open, block player movement/attacks; UI consumes mouse/keyboard.
- Optional: allow game to keep running; for testing it’s fine to pause.

## Accessibility / Usability
- Large fonts and high contrast.
- Status colors accompanied by text labels.
- Keyboard support: arrow keys to move list focus; Enter to Apply; Esc to Close.
- Click targets big enough for controller/steamdeck; later: d-pad navigation.

## Implementation Steps
1. Create `LedgerPanel` prefab (Canvas → Panel) with two columns and footer.
2. Script `LedgerPanel.cs`:
   - Populate faction list
   - Bind selection to detail pane
   - Handle slider/preset buttons
   - Call ReputationSystem setter
   - Public `Show()` / `Hide()` methods
3. Script `LedgerController.cs`:
   - Singleton (DontDestroyOnLoad)
   - Listen for B/Esc (Input System)
   - Toggle panel, manage input lock
4. Optional polish:
   - Toast/feedback
   - Remember last selected faction
   - “Write Decree” button that, later, could emit a palimpsest ALLY token instead of direct rep write.

## Future Hooks (optional)
- Integrate with Palimpsest: writing in the ledger creates an inscription layer (`ALLY:<faction>` token) near the player, decaying over turns.
- Costs/ink: require consumables to change rep.
- Audit log: list recent changes with timestamps for debugging.
