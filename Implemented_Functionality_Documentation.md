# Implemented Functionality Documentation

## Overview

This document details all functionality implemented in the Ink project, explained through the TDD test suite. Each test serves as executable documentation of expected behavior.

---

## Table of Contents

1. [Reputation System](#1-reputation-system)
2. [Inter-Faction Reputation](#2-inter-faction-reputation)
3. [Ledger UI Panel](#3-ledger-ui-panel)
4. [Cross-Faction UI Controls](#4-cross-faction-ui-controls)
5. [Hostility Service Integration](#5-hostility-service-integration)
6. [Species-Faction System](#6-species-faction-system)
7. [Faction Registry](#7-faction-registry)
8. [Territory & District Control](#8-territory--district-control)
9. [Palimpsest Layer System](#9-palimpsest-layer-system)

---

## 1. Reputation System

**File:** `LedgerTests.cs:43-104`

### 1.1 Core Storage & Retrieval

```csharp
[Test]
public void SetRep_StoresValueAndGetRepReturns()
{
    ReputationSystem.SetRep("testfaction", 42);
    Assert.AreEqual(42, ReputationSystem.GetRep("testfaction"));
}
```

**Functionality:** The `ReputationSystem` maintains a dictionary of faction IDs to integer reputation values. `SetRep()` stores values, `GetRep()` retrieves them.

### 1.2 Event System

```csharp
[Test]
public void SetRep_FiresEvent()
{
    ReputationSystem.OnRepChanged += (id, val) => { /* callback */ };
    ReputationSystem.SetRep("ghosts", 7);
    // Event fires with faction ID and new value
}
```

**Functionality:** Every reputation change fires `OnRepChanged(string factionId, int newValue)`. This enables reactive UI updates and game logic responses.

### 1.3 Idempotent Initialization

```csharp
[Test]
public void EnsureFaction_NoOverwriteExisting()
{
    ReputationSystem.SetRep("ghosts", 10);
    ReputationSystem.EnsureFaction("ghosts", 0);  // Tries to set default 0
    Assert.AreEqual(10, ReputationSystem.GetRep("ghosts"));  // Still 10
}
```

**Functionality:** `EnsureFaction(id, defaultValue)` only sets reputation if the faction doesn't exist. Used during NPC spawning to avoid overwriting player-modified values.

### 1.4 Delta Modifications

```csharp
[Test]
public void AddRep_AddsDelta()
{
    ReputationSystem.SetRep("ghosts", 5);
    ReputationSystem.AddRep("ghosts", 3);   // Now 8
    ReputationSystem.AddRep("ghosts", -10); // Now -2
}
```

**Functionality:** `AddRep(id, delta)` modifies reputation by a relative amount. Used for combat penalties (`repOnHit`, `repOnKill`) and quest rewards.

### 1.5 Null Safety

```csharp
[Test]
public void NullFactionIdIgnored()
{
    ReputationSystem.SetRep(null, 10);
    Assert.AreEqual(0, ReputationSystem.GetRep(null));  // Returns 0, no crash
    // No event fired
}
```

**Functionality:** Null faction IDs are silently ignored. Returns 0 for unknown factions. Prevents crashes from malformed data.

---

## 2. Inter-Faction Reputation

**File:** `LedgerTests.cs:106-176`

### 2.1 Asymmetric Relationships

```csharp
[Test]
public void SetInterRep_StoresValueAndGetInterRepReturns()
{
    ReputationSystem.SetInterRep("ghosts", "inkbound", -40);
    Assert.AreEqual(-40, ReputationSystem.GetInterRep("ghosts", "inkbound"));
}
```

**Functionality:** Inter-faction reputation is stored as directed pairs. "Ghosts→Inkbound" is independent of "Inkbound→Ghosts".

### 2.2 Default Neutrality

```csharp
[Test]
public void InterRep_DefaultsToZero_WhenUnset()
{
    Assert.AreEqual(0, ReputationSystem.GetInterRep("ghosts", "inkbound"));
}
```

**Functionality:** Unknown faction pairs default to 0 (neutral). Factions are neither hostile nor friendly until explicitly set.

### 2.3 Bidirectional Independence

```csharp
[Test]
public void InterRep_BidirectionalIndependent()
{
    ReputationSystem.SetInterRep("ghosts", "inkbound", -30);  // Ghosts hate Inkbound
    ReputationSystem.SetInterRep("inkbound", "ghosts", 20);   // Inkbound like Ghosts

    Assert.AreEqual(-30, ReputationSystem.GetInterRep("ghosts", "inkbound"));
    Assert.AreEqual(20, ReputationSystem.GetInterRep("inkbound", "ghosts"));
}
```

**Functionality:** Relationships are asymmetric. Faction A can hate Faction B while B likes A. This enables complex political dynamics like unrequited alliances or one-sided grudges.

### 2.4 Separate Event Channel

```csharp
[Test]
public void InterRep_FiresEventWithSourceAndTarget()
{
    ReputationSystem.OnInterRepChanged += (src, dst, val) => { /* callback */ };
    ReputationSystem.SetInterRep("ghosts", "inkbound", 25);
    // Event fires with source faction, target faction, and new value
}
```

**Functionality:** Inter-faction changes fire `OnInterRepChanged(string source, string target, int value)`. Separate from player reputation events.

### 2.5 Isolation from Player Rep

```csharp
[Test]
public void SetInterRep_DoesNotAffectPlayerRep()
{
    ReputationSystem.SetRep("ghosts", 10);  // Player↔Ghosts
    ReputationSystem.SetInterRep("ghosts", "inkbound", -30);  // Ghosts↔Inkbound
    Assert.AreEqual(10, ReputationSystem.GetRep("ghosts"));  // Unchanged
}
```

**Functionality:** Player reputation and inter-faction reputation are completely separate data stores. Changing one never affects the other.

---

## 3. Ledger UI Panel

**File:** `LedgerTests.cs:178-303`

### 3.1 Faction Selection Display

```csharp
[Test]
public void SelectFaction_ShowsCurrentRep()
{
    Assert.IsTrue(_panel.IsVisible);
    Assert.AreEqual(-10, ReputationSystem.GetRep("ghosts"));
}
```

**Functionality:** The Ledger panel displays a scrollable list of factions. Selecting a faction shows its current reputation in a detail pane.

### 3.2 Apply Changes

```csharp
[Test]
public void ApplyButton_WritesToReputationSystem()
{
    slider.value = 40;
    _panel.ApplyCurrent();
    Assert.AreEqual(40, ReputationSystem.GetRep("ghosts"));
}
```

**Functionality:** The "Apply" button commits the current slider value to `ReputationSystem`. Changes are immediately reflected in game logic.

### 3.3 Revert Changes

```csharp
[Test]
public void RevertRestoresExternalValue()
{
    ReputationSystem.SetRep("ghosts", -5);
    slider.value = 70;  // User changed it
    _panel.RevertCurrent();
    Assert.AreEqual(-5, Mathf.RoundToInt(slider.value));  // Back to -5
}
```

**Functionality:** The "Revert" button discards uncommitted changes and reloads the current reputation from `ReputationSystem`.

### 3.4 External Change Synchronization

```csharp
[Test]
public void ExternalChangeUpdatesListLabelAndDetail()
{
    ReputationSystem.SetRep("ghosts", -60);  // External change
    _panel.RevertCurrent();

    var label = FindListLabel("Ghosts");
    StringAssert.Contains("(-60)", label.text);  // List updated
    Assert.AreEqual(new Color(0.85f, 0.3f, 0.3f), repValue.color);  // Red for hostile
}
```

**Functionality:** The panel subscribes to `OnRepChanged`. When reputation changes externally (combat, quests), the UI updates automatically. Color coding: Red (hostile), Amber (neutral), Green (friendly).

### 3.5 Preset Buttons

```csharp
[Test]
public void PresetButtons_SetExpectedValues()
{
    _panel.SetRepValue(50);  // "Friendly" preset
    Assert.AreEqual(50, slider.value);
}

[Test]
public void PresetButtons_UpdateLabelAndColor()
{
    _panel.SetRepValue(50);
    StringAssert.Contains("(Friendly)", repValue.text);
    Assert.AreEqual(new Color(0.35f, 0.8f, 0.45f), repValue.color);  // Green
}
```

**Functionality:** Quick preset buttons set common values:
- **Friendly (+50):** Green, "(Friendly)" label
- **Neutral (0):** Amber, "(Neutral)" label
- **Hostile (-50):** Red, "(Hostile)" label

### 3.6 Multi-Faction Navigation

```csharp
[Test]
public void SelectsSecondFaction_ShowsSecondRep()
{
    _panel.SelectFaction(1);  // Switch to Inkbound
    StringAssert.Contains("(Neutral)", repValue.text);
}
```

**Functionality:** Clicking faction names in the list switches the detail pane. Each faction's reputation is loaded independently.

### 3.7 Slider Constraints

```csharp
[Test]
public void SliderWholeNumberClamp()
{
    slider.value = 12.7f;
    Assert.AreEqual(13, Mathf.RoundToInt(slider.value));
}
```

**Functionality:** Reputation is integer-only. Slider values are rounded. Range is -100 to +100.

### 3.8 Empty State Handling

```csharp
[Test]
public void NoSelection_ApplyRevertNoOp()
{
    var emptyPanel = new LedgerPanel();
    emptyPanel.Initialize(injectedFactions: new List<FactionDefinition>());

    Assert.DoesNotThrow(() => emptyPanel.ApplyCurrent());
    Assert.DoesNotThrow(() => emptyPanel.RevertCurrent());
}
```

**Functionality:** With no factions loaded, Apply/Revert do nothing gracefully. No null reference exceptions.

### 3.9 Scroll Support

```csharp
[Test]
public void ScrollContentSized()
{
    // Create 25 factions
    var manyFactions = new List<FactionDefinition>();
    for (int i = 0; i < 25; i++) { /* create faction */ }

    var scrollPanel = new LedgerPanel();
    scrollPanel.Initialize(injectedFactions: manyFactions);

    Assert.IsNotNull(scrollRect.viewport);
    Assert.IsNotNull(scrollRect.content);
    Assert.Greater(scrollRect.content.rect.height, 0);
}
```

**Functionality:** When many factions exist, the list becomes scrollable. Content height adjusts dynamically.

---

## 4. Cross-Faction UI Controls

**File:** `LedgerTests.cs:306-406`

### 4.1 Inter-Faction Pair Selection

```csharp
[Test]
public void SelectInterFactionPair_LoadsInterRep()
{
    ReputationSystem.SetInterRep("ghosts", "inkbound", -40);

    _panel.SelectInterFactionPair(0, 1);  // ghosts → inkbound

    Assert.AreEqual(-40, Mathf.RoundToInt(slider.value));
}
```

**Functionality:** `SelectInterFactionPair(sourceIndex, targetIndex)` switches the panel to inter-faction mode. The slider shows the relationship from source→target.

### 4.2 Apply Inter-Faction Changes

```csharp
[Test]
public void ApplyInterRep_WritesToSystem()
{
    _panel.SelectInterFactionPair(0, 1);
    slider.value = 35;
    _panel.ApplyInterRep();

    Assert.AreEqual(35, ReputationSystem.GetInterRep("ghosts", "inkbound"));
}
```

**Functionality:** `ApplyInterRep()` commits the slider value as the inter-faction reputation for the selected pair.

### 4.3 Revert Inter-Faction Changes

```csharp
[Test]
public void RevertInterRep_ReloadsExternalChange()
{
    ReputationSystem.SetInterRep("ghosts", "inkbound", -10);
    _panel.SelectInterFactionPair(0, 1);

    slider.value = 60;  // User modified
    _panel.RevertInterRep();

    Assert.AreEqual(-10, Mathf.RoundToInt(slider.value));  // Restored
}
```

**Functionality:** `RevertInterRep()` discards local changes and reloads from `ReputationSystem`.

### 4.4 Self-Targeting Prevention

```csharp
[Test]
public void PreventSelfTarget_NoOp()
{
    _panel.SelectInterFactionPair(0, 0);  // ghosts → ghosts (invalid)

    slider.value = 50;
    _panel.ApplyInterRep();

    Assert.AreEqual(0, ReputationSystem.GetInterRep("ghosts", "ghosts"));
    // No event fired
}
```

**Functionality:** A faction's relationship with itself is always 0 and cannot be modified. The UI silently ignores self-targeting attempts.

### 4.5 Pair Switching Preserves Data

```csharp
[Test]
public void SwitchingPairs_PreservesValuesPerPair()
{
    ReputationSystem.SetInterRep("ghosts", "inkbound", -30);
    ReputationSystem.SetInterRep("inkbound", "ghosts", 20);

    _panel.SelectInterFactionPair(0, 1);  // -30
    Assert.AreEqual(-30, Mathf.RoundToInt(slider.value));

    _panel.SelectInterFactionPair(1, 0);  // 20
    Assert.AreEqual(20, Mathf.RoundToInt(slider.value));
}
```

**Functionality:** Switching between pairs loads the correct values. Each direction (A→B vs B→A) is independent.

### 4.6 External Inter-Rep Updates

```csharp
[Test]
public void ExternalInterRepChange_UpdatesUI()
{
    _panel.SelectInterFactionPair(0, 1);

    ReputationSystem.SetInterRep("ghosts", "inkbound", -70);  // External change

    StringAssert.Contains("-70", repValue.text);  // UI updated
}
```

**Functionality:** The panel subscribes to `OnInterRepChanged`. External changes (e.g., from events or other systems) update the displayed value in real-time.

---

## 5. Hostility Service Integration

**File:** `LedgerTests.cs:409-568`

### 5.1 Player↔Faction Hostility

```csharp
[Test]
public void HostilityUsesUpdatedRep()
{
    ReputationSystem.SetRep(_ghosts.id, -30);
    Assert.IsTrue(HostilityService.IsHostile(player, npcEntity));  // Hostile at -30

    ReputationSystem.SetRep(_ghosts.id, 50);
    Assert.IsFalse(HostilityService.IsHostile(player, npcEntity));  // Not hostile at +50
}
```

**Functionality:** `HostilityService.IsHostile()` queries `ReputationSystem` in real-time. Threshold is -25: below = hostile, above = not hostile.

### 5.2 Inter-Faction Hostility

```csharp
[Test]
public void HostilityUsesInterRepWhenBothFactioned()
{
    ReputationSystem.SetInterRep("ghosts", "inkbound", -40);  // Ghosts hate Inkbound
    ReputationSystem.SetInterRep("inkbound", "ghosts", 40);   // Inkbound like Ghosts

    // Ghost attacking Inkbound
    Assert.IsTrue(HostilityService.IsHostile(ghostNPC, inkboundNPC));   // Hostile (-40)

    // Inkbound attacking Ghost
    Assert.IsFalse(HostilityService.IsHostile(inkboundNPC, ghostNPC));  // Not hostile (+40)
}
```

**Functionality:** When two factioned NPCs interact, hostility is determined by the **attacker's** inter-faction reputation toward the **target's** faction. Asymmetric relationships create complex dynamics.

### 5.3 Default Neutrality Between Factions

```csharp
[Test]
public void InterFactionHostility_NeutralByDefault()
{
    // No inter-rep set - defaults to 0

    Assert.IsFalse(HostilityService.IsHostile(ghostNPC, inkboundNPC));
    Assert.IsFalse(HostilityService.IsHostile(inkboundNPC, ghostNPC));
}
```

**Functionality:** Factions with no explicit relationship (inter-rep = 0) are neutral. They won't attack each other on sight.

---

## 6. Species-Faction System

**File:** `SpeciesFactionTests.cs:107-821`

### 6.1 Automatic Faction Assignment

```csharp
[Test]
public void SpeciesMember_EnsuresDefaultFaction_WhenNull()
{
    var species = new SpeciesDefinition { displayName = "TestCreature", defaultFaction = null };

    speciesMember.species = species;
    speciesMember.EnsureDefaultFaction();

    Assert.IsNotNull(species.defaultFaction);  // Now assigned
}
```

**Functionality:** `SpeciesMember.EnsureDefaultFaction()` auto-assigns a faction matching the species name if none exists. Called on Awake.

### 6.2 Faction Name Matching

```csharp
[Test]
public void SpeciesMember_EnsuresDefaultFaction_MatchesSpeciesDisplayName()
{
    var species = new SpeciesDefinition { displayName = "DeltaCreature" };

    speciesMember.EnsureDefaultFaction();

    Assert.AreEqual("DeltaCreature", species.defaultFaction.displayName);
}
```

**Functionality:** Auto-created factions inherit the species' `displayName` as both their ID and display name.

### 6.3 Existing Faction Preservation

```csharp
[Test]
public void SpeciesMember_EnsuresDefaultFaction_PreservesExistingFaction()
{
    species.defaultFaction = existingFaction;

    speciesMember.EnsureDefaultFaction();

    Assert.AreSame(existingFaction, species.defaultFaction);  // Not replaced
}
```

**Functionality:** If a species already has a `defaultFaction` assigned (in the inspector), `EnsureDefaultFaction()` does nothing.

### 6.4 FactionMember Inheritance

```csharp
[Test]
public void FactionMember_InheritsFactionFromSpecies()
{
    speciesMember.species = species;
    factionMember.faction = null;

    speciesMember.EnsureDefaultFaction();
    factionMember.ApplyRank();

    Assert.AreEqual("ZetaCreature", factionMember.faction.displayName);
}
```

**Functionality:** When `FactionMember.ApplyRank()` runs with no explicit faction, it pulls from `SpeciesMember.species.defaultFaction`.

### 6.5 Existing Faction Lookup

```csharp
[Test]
public void Integration_ExistingFactionUsed_WhenSpeciesMatchesFactionName()
{
    var ghostFaction = FactionRegistry.GetByName("Ghost");  // Pre-existing
    species.displayName = "Ghost";  // Matches

    speciesMember.EnsureDefaultFaction();

    Assert.AreSame(ghostFaction, species.defaultFaction);  // Uses existing, doesn't create new
}
```

**Functionality:** If a faction with the species name already exists in `FactionRegistry`, it's reused rather than creating a duplicate.

### 6.6 Explicit Faction Override

```csharp
[Test]
public void FactionMember_ExplicitFaction_NotOverwrittenBySpecies()
{
    speciesMember.species = slimeSpecies;
    factionMember.faction = ghostFaction;  // Explicitly set

    speciesMember.EnsureDefaultFaction();
    factionMember.ApplyRank();

    Assert.AreSame(ghostFaction, factionMember.faction);  // Still Ghost, not Slime
}
```

**Functionality:** If `FactionMember.faction` is explicitly set before `ApplyRank()`, species defaults don't override it. This enables "defector" NPCs.

### 6.7 Runtime Faction Switching

```csharp
[Test]
public void FactionMember_SetFaction_CanChangeToDifferentFactionThanSpecies()
{
    // Start as SlimeDefector faction
    factionMember.ApplyRank();
    Assert.AreEqual("SlimeDefector", factionMember.faction.displayName);

    // Defect to Ghost
    factionMember.SetFaction(ghostFaction);

    Assert.AreEqual("Ghost", factionMember.faction.displayName);
    Assert.AreEqual("SlimeDefector", speciesMember.species.displayName);  // Species unchanged
}
```

**Functionality:** `FactionMember.SetFaction()` changes allegiance at runtime. Species identity remains constant—a Slime can join the Ghost faction but is still a Slime.

### 6.8 Same Species, Different Factions

```csharp
[Test]
public void FactionMember_SameSpecies_DifferentFactions_CanBeHostile()
{
    // Two Slimes: one loyalist, one defector
    loyalistFaction.faction = slimeFaction;
    defectorFaction.faction = ghostFaction;

    ReputationSystem.SetInterRep(slimeFaction.id, ghostFaction.id, -50);

    Assert.IsTrue(HostilityService.IsHostile(loyalist, defector));
    Assert.IsTrue(HostilityService.IsHostile(defector, loyalist));
}
```

**Functionality:** Hostility is based on **faction**, not species. Two Slimes in opposing factions will fight each other. Enables civil war scenarios.

### 6.9 Different Species, Same Faction

```csharp
[Test]
public void FactionMember_DifferentSpecies_SameFaction_NotHostile()
{
    // Slime and Snake both in "GrandAlliance" faction
    slimeFactionMember.faction = allianceFaction;
    snakeFactionMember.faction = allianceFaction;

    Assert.IsFalse(HostilityService.IsHostile(slimeEntity, snakeEntity));
}
```

**Functionality:** Faction membership trumps species. A Slime and Snake in the same faction are allies, not enemies.

---

## 7. Faction Registry

**File:** `SpeciesFactionTests.cs:30-105`

### 7.1 Existing Faction Lookup

```csharp
[Test]
public void FactionRegistry_GetOrCreate_ReturnsExistingFaction()
{
    var existingFaction = FactionRegistry.GetByName("Ghost");
    var result = FactionRegistry.GetOrCreate("Ghost");

    Assert.AreSame(existingFaction, result);
}
```

**Functionality:** `GetOrCreate(name)` first checks for existing factions by display name. If found, returns the existing one.

### 7.2 Runtime Faction Creation

```csharp
[Test]
public void FactionRegistry_GetOrCreate_CreatesRuntimeFaction()
{
    var existing = FactionRegistry.GetByName("BrandNewSpecies");
    Assert.IsNull(existing);  // Doesn't exist

    var result = FactionRegistry.GetOrCreate("BrandNewSpecies");

    Assert.IsNotNull(result);  // Now created
}
```

**Functionality:** If no faction exists with that name, `GetOrCreate()` creates a new `FactionDefinition` at runtime.

### 7.3 Runtime Faction Properties

```csharp
[Test]
public void FactionRegistry_GetOrCreate_RuntimeFactionHasCorrectId()
{
    var faction = FactionRegistry.GetOrCreate("TestSpeciesAlpha");
    Assert.AreEqual("TestSpeciesAlpha", faction.id);
}

[Test]
public void FactionRegistry_GetOrCreate_RuntimeFactionHasCorrectDisplayName()
{
    var faction = FactionRegistry.GetOrCreate("TestSpeciesBeta");
    Assert.AreEqual("TestSpeciesBeta", faction.displayName);
}
```

**Functionality:** Runtime-created factions have their `id` and `displayName` set to the provided name.

### 7.4 Runtime Faction Registration

```csharp
[Test]
public void FactionRegistry_GetOrCreate_RuntimeFactionIsRegistered()
{
    var created = FactionRegistry.GetOrCreate("UniqueTestFaction");

    var byName = FactionRegistry.GetByName("UniqueTestFaction");
    var byId = FactionRegistry.GetById("UniqueTestFaction");

    Assert.AreSame(created, byName);
    Assert.AreSame(created, byId);
}
```

**Functionality:** Runtime factions are immediately registered and queryable via both `GetByName()` and `GetById()`.

### 7.5 Singleton Pattern

```csharp
[Test]
public void FactionRegistry_GetOrCreate_CalledTwice_ReturnsSameInstance()
{
    var first = FactionRegistry.GetOrCreate("RepeatedSpecies");
    var second = FactionRegistry.GetOrCreate("RepeatedSpecies");

    Assert.AreSame(first, second);
}
```

**Functionality:** Multiple calls to `GetOrCreate()` with the same name return the same instance. No duplicates created.

---

## 8. Territory & District Control

**File:** `TerritoryDebugPanelTests.cs:1-110`, `LedgerTests.cs:436-477`

### 8.1 District State Structure

```csharp
// Setup shows the data model
_state = new DistrictState(distDef, factionCount: 2);
_state.control[0] = 0.30f;  // Faction 0 has 30% control
_state.patrol[0] = 0.30f;   // Faction 0 has 30% patrol presence
_state.heat[0] = 0.10f;     // Faction 0 has 10% heat (unrest)
```

**Functionality:** Each district tracks per-faction:
- **Control (C):** Political/economic dominance (0.0 - 1.0)
- **Patrol (P):** Military presence (0.0 - 1.0)
- **Heat (H):** Instability/unrest from player actions (0.0 - 1.0)
- **LossStreak:** Consecutive days below control threshold

### 8.2 Patrol Adjustment

```csharp
[Test]
public void AdjustPatrol_ClampsAndRecomputesControl()
{
    _svc.AdjustPatrol("market", factionIndex: 0, delta: +0.10f);

    Assert.AreEqual(0.40f, _state.patrol[0]);  // Increased and clamped

    // Control immediately recomputed using formula:
    // C' = C + g*P*(1-C) - h*H*C
    float expected = Mathf.Clamp01(0.30f + 0.08f * 0.40f * (1f - 0.30f) - 0.10f * 0.10f * 0.30f);
    Assert.AreEqual(expected, _state.control[0]);
}
```

**Functionality:** `AdjustPatrol()` modifies patrol presence and **immediately** recalculates control using the GDD formula. Patrol is clamped to [0, 1].

### 8.3 Daily Tick

```csharp
[Test]
public void AdvanceDay_AppliesEditsCleanupAndLossStreak()
{
    _svc.ApplyPalimpsestEdit("market", intensity: 1f);  // Pending heat increase
    _svc.ApplyCleanup("market", intensity: 0.5f);       // Pending heat decrease
    _state.control[0] = 0.1f;  // Below threshold

    _svc.AdvanceDay();

    Assert.LessOrEqual(_state.heat[0], 1f);  // Heat updated
    Assert.Greater(_state.patrol[0], 0f);     // Patrol adjusted
    Assert.AreEqual(prevLoss + 1, _state.lossStreak[0]);  // Loss streak incremented
}
```

**Functionality:** `AdvanceDay()` processes:
1. Pending palimpsest edits → increase heat
2. Pending cleanups → decrease heat
3. Heat baseline decay
4. Control recalculation
5. Loss streak tracking for territories below threshold

### 8.4 Palimpsest Heat Integration

```csharp
[Test]
public void InscribableSurfaceRaisesHeat()
{
    float initialHeat = state.heat[0];

    _svc.ApplyPalimpsestEdit("test_district", intensity: 1f);
    _svc.AdvanceDay();

    Assert.Greater(state.heat[0], initialHeat);
}
```

**Functionality:** Inscribing palimpsest layers increases district heat. The `InscribableSurface` component calls `DistrictControlService.ApplyPalimpsestEdit()` when layers are created.

---

## 9. Palimpsest Layer System

**File:** Documented via `OverlayResolver.cs` and integration tests

### 9.1 Layer Registration

```csharp
// From OverlayResolver.cs
public static int RegisterLayer(PalimpsestLayer layer)
{
    layer.id = _nextId++;
    ParseTokens(layer);
    _layers.Add(layer);
    return layer.id;
}
```

**Functionality:** Layers are registered with auto-incrementing IDs. Tokens are parsed on registration.

### 9.2 Token Parsing

```csharp
// Supported tokens:
"TRUCE"              → layer.truce = true
"ALLY:faction_id"    → layer.allyFactionId = "faction_id"
"HUNT:faction_id"    → layer.huntFactionId = "faction_id"
```

**Functionality:** Tokens modify layer flags:
- **TRUCE:** Suppresses all hostility in radius
- **ALLY:** Treats specified faction as friendly
- **HUNT:** Marks faction for targeting

### 9.3 Spatial Query

```csharp
public static PalimpsestRules GetRulesAt(int x, int y)
{
    // For each layer:
    //   - Check if (x,y) is within layer.radius of layer.center
    //   - Combine effects: truce is OR'd, ally/hunt use highest priority
}
```

**Functionality:** `GetRulesAt()` aggregates all overlapping layers at a position. Multiple layers stack according to priority.

### 9.4 Decay System

```csharp
public static void TickDecay()
{
    // For each layer:
    //   - Decrement turnsRemaining
    //   - Remove layer when turnsRemaining == 0
}
```

**Functionality:** Layers expire over time. Each turn, `turnsRemaining` decreases. At 0, the layer is removed.

### 9.5 Hostility Override

```csharp
// In HostilityService.IsHostile():
var rules = OverlayResolver.GetRulesAt(target.gridX, target.gridY);
if (rules.truce)
    return false;  // TRUCE suppresses hostility

if (rules.allyFactionId == attackerFaction.id)
    return false;  // ALLY makes attacker's faction non-hostile
```

**Functionality:** Palimpsest rules can override normal hostility checks. TRUCE creates peace zones; ALLY creates faction-specific safe areas.

---

## Summary: Test Coverage Statistics

| System | Test Count | Coverage |
|--------|------------|----------|
| Player-Faction Reputation | 5 | Core CRUD, events, null safety |
| Inter-Faction Reputation | 6 | Asymmetry, defaults, events, isolation |
| Ledger UI | 12 | Selection, apply, revert, presets, scroll |
| Cross-Faction UI | 6 | Pair selection, apply, revert, self-target |
| Hostility Integration | 3 | Player↔faction, faction↔faction, defaults |
| Species-Faction | 20+ | Auto-assign, inheritance, defection, hostility |
| Faction Registry | 6 | Lookup, create, register, singleton |
| Territory Control | 3 | Patrol, daily tick, heat |
| **Total** | **60+** | **Comprehensive** |

---

## Architectural Insights from Tests

### 1. Event-Driven UI
The Ledger subscribes to `OnRepChanged` and `OnInterRepChanged`, enabling real-time updates without polling.

### 2. Composition over Inheritance
`SpeciesMember` and `FactionMember` are separate components. An entity's species (what it IS) is independent of its faction (who it's WITH).

### 3. Asymmetric Relationships
Inter-faction reputation is directional. This enables nuanced political dynamics like one-sided alliances, grudges, and betrayals.

### 4. Graceful Degradation
Null checks, default values, and empty state handling prevent crashes. Unknown factions return 0 reputation, not errors.

### 5. Test Isolation
`ClearForTests()` methods reset static state. Tests don't pollute each other.

### 6. Integration Testing
Tests verify cross-system behavior (e.g., `HostilityUsesUpdatedRep` confirms that `HostilityService` actually queries `ReputationSystem`).
