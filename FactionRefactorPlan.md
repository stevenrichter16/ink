# Node-Based Dialogue System Checklist

## Scope
Introduce a node-based dialogue graph system that can coexist with the current `DialogueSequence` flow, add player choice UI, and integrate with faction reputation branching.

## Checklist

### 1) Data Model
- [ ] Add `DialogueGraph` ScriptableObject with entry node reference and node list.
- [ ] Add base `DialogueNode` ScriptableObject with GUID/id.
- [ ] Add node types:
  - [ ] `LineNode` (speaker, text, next)
  - [ ] `ChoiceNode` (prompt, choices[])
  - [ ] `BranchNode` (conditions -> next)
  - [ ] `EndNode`
- [ ] Define `DialogueChoice` data:
  - [ ] `text`
  - [ ] `next` (node reference)
  - [ ] optional `conditions`
  - [ ] optional `effects`
- [ ] Implement condition/effect primitives:
  - [ ] Reputation threshold condition
  - [ ] Quest state condition
  - [ ] Optional item/inventory condition
  - [ ] Reputation delta effect
  - [ ] Quest give/turn-in effect

### 2) Runtime Integration
- [ ] Extend `DialogueRunner` to accept either `DialogueSequence` or `DialogueGraph`.
- [ ] Add graph executor:
  - [ ] Set active node on `Begin()`
  - [ ] LineNode: advance on input
  - [ ] ChoiceNode: wait for selection
  - [ ] BranchNode: pick first valid condition
  - [ ] EndNode: close dialogue
- [ ] Add choice UI rendering (IMGUI) with 1–9 hotkeys.
- [ ] Apply choice effects when selected.
- [ ] Ensure dialogue open state blocks spell hotkeys.

### 3) Authoring Tools
- [ ] Create `DialogueGraphEditor` (custom inspector) with:
  - [ ] Add node buttons (Line/Choice/Branch/End)
  - [ ] Add nodes as sub-assets of the graph
  - [ ] Link nodes via object fields
- [ ] Add “Validate Graph” button:
  - [ ] Missing entry node
  - [ ] Null references
  - [ ] Unreachable nodes
  - [ ] Cycles without exit

### 4) Faction Integration
- [ ] Add graph fields to `FactionDefinition.RankDefinition`:
  - [ ] `neutralGraph`, `friendlyGraph`, `hostileGraph`
- [ ] Update `FactionMember.ApplyRank()` to prefer graphs when present.
- [ ] Keep sequence fallback if graph is null.
- [ ] Keep rep-based friendly/hostile branching before quest/default logic.

### 5) Migration & Compatibility
- [ ] Keep `DialogueSequence` unchanged and supported.
- [ ] Add a converter utility: `DialogueSequence -> DialogueGraph` (linear).
- [ ] Migrate Inkbound/Inkguard dialogues to graphs after runtime is stable.
- [ ] Update existing faction assets only when graphs are ready.

### 6) QA / Verification
- [ ] Graph-based dialogue opens/closes properly.
- [ ] Choices display and select via hotkeys.
- [ ] Conditions filter choices as expected.
- [ ] Effects update reputation and quests correctly.
- [ ] Friendly/hostile rep branching works with graphs.
- [ ] No spell casting while dialogue is open.

## Notes
- Use composition, not inheritance, for faction membership and dialogue logic.
- Keep the editor tooling minimal to avoid churn; expand later if needed.
