# Ledger Test Suite Plan (What / Why / How)

This document outlines the intended tests for the Ledger (Faction Reputation Book) feature, including rationale and approach.

## Scope
- ReputationSystem correctness (data + events)
- LedgerPanel UI interactions (Apply/Revert, presets, selection)
- Event propagation (external rep changes reflected in UI)
- Optional integrations (Hostility, Palimpsest heat hook) for later

## Tests (with technical steps)

### ReputationSystem
1) **SetRep_StoresValueAndGetRepReturns**  
   - *Setup*: Call ReputationSystem.ClearForTests(); SetRep("f", 42).  
   - *Assert*: GetRep("f") == 42.

2) **SetRep_FiresEvent**  
   - *Setup*: Clear; int calls=0; subscribe OnRepChanged += (id,val)=>{calls++; capturedId=id; capturedVal=val;}.  
   - *Action*: SetRep("ghosts", 7).  
   - *Assert*: calls==1; capturedId=="ghosts"; capturedVal==7.

3) **EnsureFaction_NoOverwriteExisting**  
   - *Setup*: Clear; SetRep("ghosts", 10); EnsureFaction("ghosts", 0).  
   - *Assert*: GetRep("ghosts")==10.

4) **AddRep_AddsDelta**  
   - *Setup*: Clear; SetRep("ghosts", 5).  
   - *Action*: AddRep("ghosts", 3) → expect 8; AddRep("ghosts", -10) → expect -2.

### LedgerPanel UI
5) **BuildUI_SelectsFirstFactionAndShowsRep**  
   - *Setup*: Inject two ScriptableObject factions (ghosts rep -10, inkbound rep 20). Initialize panel.  
   - *Assert*: Selected index 0; detail text shows -10/Hostile/Neutral status matching rep; IsVisible true.

6) **SelectFaction_UpdatesDetailPane**  
   - *Action*: panel.SelectFaction(1).  
   - *Assert*: Detail name == inkbound, rep text shows 20 (Neutral), color neutral.

7) **PresetButtons_SetCorrectValues**  
   - *Action*: panel.SetRepValue(50) or invoke preset button;  
   - *Assert*: slider value == 50; label text contains "(Friendly)" and friendly color.

8) **ApplyButton_WritesToReputationSystem**  
   - *Action*: set slider to 40; panel.ApplyCurrent();  
   - *Assert*: ReputationSystem.GetRep("ghosts")==40; list label for Ghosts shows "(40)".

9) **RevertButton_ReloadsExternalChange**  
   - *Setup*: SetRep("ghosts", -5); change slider to 70;  
   - *Action*: panel.RevertCurrent();  
   - *Assert*: slider rounds to -5; detail text matches -5.

10) **ExternalRepChange_UpdatesUI**  
    - *Action*: ReputationSystem.SetRep("ghosts", -60); panel.RevertCurrent();  
    - *Assert*: list label shows -60; detail color == hostile color.

11) **ApplyFiresOnRepChangedEvent**  
    - *Setup*: subscribe counter;  
    - *Action*: slider=15; ApplyCurrent();  
    - *Assert*: event fired once.

12) **NoSelection_ApplyRevertNoOp**  
    - *Setup*: Initialize panel with empty faction list;  
    - *Action*: ApplyCurrent(), RevertCurrent();  
    - *Assert*: no exception; rep dictionary remains empty.

13) **SliderWholeNumberClamp**  
    - *Action*: set slider to 12.7f;  
    - *Assert*: Mathf.RoundToInt(slider.value) == 13; detail text shows 13.

14) **ListLabelsUpdateForAllFactions**  
    - *Action*: SetRep("inkbound", 55); panel.RevertCurrent();  
    - *Assert*: label for Inkbound shows "(55)" even if not selected.

### Hostility integration (optional)
15) **HostilityUsesUpdatedRep**  
    - *Setup*: Create two GameObjects with FactionMember, assign same faction def, mock ReputationSystem rep values; player vs NPC.  
    - *Action/Assert*: Rep -30 ⇒ HostilityService.IsHostile(player,npc)==true; SetRep +50 ⇒ false.

### Palimpsest / Territory hook (optional)
16) **InscribableSurfaceRaisesHeat**  
    - *Setup*: Instantiate DistrictControlService with one district; set defaultHeat=0.1, heatFromEdit>0; create InscribableSurface with districtId and call RegisterLayer();  
    - *Action*: AdvanceDay();  
    - *Assert*: state.heat for that district > initialHeat (unless baseline decay cancels; pick coefficients accordingly).

### UI robustness (optional)
17) **ScrollContentSized**  
    - *Setup*: Inject 20+ factions; Initialize panel;  
    - *Assert*: ScrollRect has viewport/content assigned; content preferred height > viewport height; ensures scrollable.

### Negative / Edge
18) **NullFactionIdIgnored**  
    - *Action*: SetRep(null, 10);  
    - *Assert*: GetRep(null)==0; OnRepChanged not fired (subscribe counter==0).

## Notes
- Most tests are EditMode; Hostility integration may require lightweight scene objects or stubs.  
- Input/B-key toggle tests would need PlayMode + InputTestFixture (not planned yet).  
- Reuse `ReputationSystem.ClearForTests()` for isolation.  
- Inject factions into LedgerPanel via the testing hook to avoid Resources dependency.
