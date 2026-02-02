# TDD Plan — Ledger Cross-Faction Reputation Editing

Goal: Extend the Ledger so the player can edit any faction’s reputation **toward any other faction** (not just toward the player). Implement via TDD: write failing tests first, then code.

## Scope / Requirements
- Ledger UI gains a mode to select **source faction** and **target faction** and edit the numeric reputation from source → target.
- Changes should immediately reflect in HostilityService logic where applicable (if it later uses inter-faction rep).
- Existing player↔faction rep editing must keep working.
- Events fire on rep change; ledger list/detail refresh accordingly.
- Defensive behavior for null/invalid faction ids.

## Test Matrix (write first, see them fail)

### ReputationSystem (new inter-faction API)
1) **SetInterRep_StoresValueAndGetInterRepReturns**  
   - *Setup*: Clear; SetInterRep("ghosts","inkbound", -40).  
   - *Assert*: GetInterRep("ghosts","inkbound")==-40.
2) **InterRep_DefaultsToZero_WhenUnset**  
   - *Setup*: Clear.  
   - *Assert*: GetInterRep("ghosts","inkbound")==0.
3) **InterRep_FiresEventWithSourceAndTarget**  
   - *Setup*: subscribe OnInterRepChanged(idSrc,idDst,val);  
   - *Action*: SetInterRep("ghosts","inkbound",25).  
   - *Assert*: event fired once with matching ids and value.
4) **SetInterRep_DoesNotAffectPlayerRep**  
   - *Setup*: SetRep(player vs ghosts) to 10; SetInterRep(ghosts→inkbound,-30).  
   - *Assert*: GetRep(ghosts) still 10.
5) **NullIdsIgnored_NoEvent**  
   - *Action*: SetInterRep(null,"x",10) and SetInterRep("x",null,10).  
   - *Assert*: value remains 0; event not fired.

### LedgerPanel UI (new cross-faction editing)
6) **BuildUI_ShowsSourceAndTargetPickers**  
   - *Setup*: Inject 3 factions; initialize.  
   - *Assert*: source dropdown exists, target dropdown exists; defaults select different factions.
7) **SelectingSourceAndTargetLoadsInterRep**  
   - *Setup*: SetInterRep(ghosts→inkbound, -40).  
   - *Action*: select source=ghosts, target=inkbound.  
   - *Assert*: detail shows -40 and status color (hostile).
8) **ApplyInterRep_WritesToSystem**  
   - *Action*: set slider to 35; Apply; source=ghosts, target=inkbound.  
   - *Assert*: GetInterRep(ghosts,inkbound)==35; UI label updated.
9) **RevertInterRep_ReloadsExternalChange**  
   - *Setup*: SetInterRep(ghosts→inkbound,-10); change slider to 60.  
   - *Action*: Revert.  
   - *Assert*: slider/value show -10 again.
10) **ExternalInterRepChange_UpdatesUI**  
    - *Action*: SetInterRep(ghosts→inkbound,-70) after UI up; trigger refresh (OnInterRepChanged).  
    - *Assert*: detail and labels reflect -70.
11) **PreventSelfTarget_NoOpOrDisabled**  
    - *Action*: pick same source and target;  
    - *Assert*: Apply is disabled or ignored; no value stored; event not fired.
12) **SwitchingPairs_PreservesValuesPerPair**  
    - *Setup*: ghosts→inkbound=-30; inkbound→ghosts=20.  
    - *Action*: toggle between pairs.  
    - *Assert*: UI shows the correct value for each direction.

### Hostility (smoke, optional)
13) **HostilityUsesInterRepWhenBothFactioned**  
    - *Setup*: two NPCs with factions ghosts and inkbound; SetInterRep(ghosts→inkbound,-40); SetInterRep(inkbound→ghosts,+40).  
    - *Assert*: IsHostile(ghost, inkbound) true; IsHostile(inkbound, ghost) false. (Impl may be stubbed; write test to drive behavior.)

### UI Robustness / Edge
14) **EmptyFactions_NoCrash**  
    - *Setup*: init with empty list;  
    - *Assert*: panel builds, Apply/Revert no-op, no exceptions.
15) **ListLabelUpdatesWhenInterRepChanges**  
    - *Action*: change ghosts→inkbound via system;  
    - *Assert*: any label/badge for that pair updates (if shown in UI list).

## Implementation Steps (after tests exist and fail)
1) ReputationSystem: add inter-faction storage (e.g., Dictionary<(string src,string dst), int>), getters/setters, event `OnInterRepChanged`. Keep existing player-facing rep untouched.
2) LedgerPanel: add source/target selectors (dropdowns or list), an inter-rep slider, Apply/Revert wired to new API, and listen to `OnInterRepChanged`.
3) HostilityService (optional now): add a lookup that uses inter-rep when both entities have factions; fall back to player rep for player interactions.
4) UI polish: disable Apply when source==target; ensure colors/status reuse existing thresholds.
5) Persistence (later): save/load inter-reps with your save system.

## Notes
- Keep tests in EditMode where possible; Hostility smoke may need lightweight GameObjects with FactionMember.
- Use `ReputationSystem.ClearForTests()` to isolate state; add a similar `ClearInterRepForTests()` if needed.
- Inject factions into LedgerPanel to avoid Resources dependency in tests.
