using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using InkSim;
using UnityEngine.UI;

public class LedgerTests
{
    private GameObject _go;
    private LedgerPanel _panel;
    private FactionDefinition _ghosts;
    private FactionDefinition _inkbound;

    [SetUp]
    public void SetUp()
    {
        ReputationSystem.ClearForTests();
        _ghosts = ScriptableObject.CreateInstance<FactionDefinition>();
        _ghosts.id = "ghosts";
        _ghosts.displayName = "Ghosts";

        _inkbound = ScriptableObject.CreateInstance<FactionDefinition>();
        _inkbound.id = "inkbound";
        _inkbound.displayName = "Inkbound";

        ReputationSystem.SetRep(_ghosts.id, -10);
        ReputationSystem.SetRep(_inkbound.id, 20);

        _go = new GameObject("LedgerTestGO");
        _panel = _go.AddComponent<LedgerPanel>();
        _panel.Initialize(onClose: null, injectedFactions: new List<FactionDefinition> { _ghosts, _inkbound });
    }

    [TearDown]
    public void TearDown()
    {
        if (_panel != null) Object.DestroyImmediate(_panel.gameObject);
        ReputationSystem.ClearForTests();
    }

    #region Player-Faction Reputation Tests

    [Test]
    public void SetRep_StoresValueAndGetRepReturns()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetRep("testfaction", 42);
        Assert.AreEqual(42, ReputationSystem.GetRep("testfaction"));
    }

    [Test]
    public void SetRep_FiresEvent()
    {
        ReputationSystem.ClearForTests();
        int calls = 0;
        string capturedId = null;
        int capturedVal = 0;
        ReputationSystem.OnRepChanged += (id, val) => { calls++; capturedId = id; capturedVal = val; };
        
        ReputationSystem.SetRep("ghosts", 7);
        
        Assert.AreEqual(1, calls);
        Assert.AreEqual("ghosts", capturedId);
        Assert.AreEqual(7, capturedVal);
    }

    [Test]
    public void EnsureFaction_NoOverwriteExisting()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetRep("ghosts", 10);
        ReputationSystem.EnsureFaction("ghosts", 0);
        Assert.AreEqual(10, ReputationSystem.GetRep("ghosts"));
    }

    [Test]
    public void AddRep_AddsDelta()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetRep("ghosts", 5);
        
        ReputationSystem.AddRep("ghosts", 3);
        Assert.AreEqual(8, ReputationSystem.GetRep("ghosts"));
        
        ReputationSystem.AddRep("ghosts", -10);
        Assert.AreEqual(-2, ReputationSystem.GetRep("ghosts"));
    }

    [Test]
    public void NullFactionIdIgnored()
    {
        ReputationSystem.ClearForTests();
        int calls = 0;
        ReputationSystem.OnRepChanged += (_, __) => calls++;
        
        ReputationSystem.SetRep(null, 10);
        
        Assert.AreEqual(0, ReputationSystem.GetRep(null));
        Assert.AreEqual(0, calls);
    }

    #endregion

    #region Inter-Faction Reputation Tests

    [Test]
    public void SetInterRep_StoresValueAndGetInterRepReturns()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetInterRep("ghosts", "inkbound", -40);
        Assert.AreEqual(-40, ReputationSystem.GetInterRep("ghosts", "inkbound"));
    }

    [Test]
    public void InterRep_DefaultsToZero_WhenUnset()
    {
        ReputationSystem.ClearForTests();
        Assert.AreEqual(0, ReputationSystem.GetInterRep("ghosts", "inkbound"));
    }

    [Test]
    public void InterRep_FiresEventWithSourceAndTarget()
    {
        ReputationSystem.ClearForTests();
        int calls = 0;
        string capturedSrc = null;
        string capturedDst = null;
        int capturedVal = 0;
        ReputationSystem.OnInterRepChanged += (src, dst, val) => { calls++; capturedSrc = src; capturedDst = dst; capturedVal = val; };
        
        ReputationSystem.SetInterRep("ghosts", "inkbound", 25);
        
        Assert.AreEqual(1, calls);
        Assert.AreEqual("ghosts", capturedSrc);
        Assert.AreEqual("inkbound", capturedDst);
        Assert.AreEqual(25, capturedVal);
    }

    [Test]
    public void SetInterRep_DoesNotAffectPlayerRep()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetRep("ghosts", 10);
        ReputationSystem.SetInterRep("ghosts", "inkbound", -30);
        Assert.AreEqual(10, ReputationSystem.GetRep("ghosts"));
    }

    [Test]
    public void InterRep_NullIdsIgnored_NoEvent()
    {
        ReputationSystem.ClearForTests();
        int calls = 0;
        ReputationSystem.OnInterRepChanged += (_, __, ___) => calls++;
        
        ReputationSystem.SetInterRep(null, "inkbound", 10);
        ReputationSystem.SetInterRep("ghosts", null, 10);
        
        Assert.AreEqual(0, ReputationSystem.GetInterRep(null, "inkbound"));
        Assert.AreEqual(0, ReputationSystem.GetInterRep("ghosts", null));
        Assert.AreEqual(0, calls);
    }

    [Test]
    public void InterRep_BidirectionalIndependent()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetInterRep("ghosts", "inkbound", -30);
        ReputationSystem.SetInterRep("inkbound", "ghosts", 20);
        
        Assert.AreEqual(-30, ReputationSystem.GetInterRep("ghosts", "inkbound"));
        Assert.AreEqual(20, ReputationSystem.GetInterRep("inkbound", "ghosts"));
    }

    #endregion

    #region LedgerPanel UI Tests

    [Test]
    public void SelectFaction_ShowsCurrentRep()
    {
        Assert.IsTrue(_panel.IsVisible);
        Assert.AreEqual(-10, ReputationSystem.GetRep("ghosts"));
    }

    [Test]
    public void ApplyButton_WritesToReputationSystem()
    {
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 40;
        _panel.ApplyCurrent();
        Assert.AreEqual(40, ReputationSystem.GetRep("ghosts"));
    }

    [Test]
    public void ExternalRepChange_UpdatesUI()
    {
        ReputationSystem.SetRep("ghosts", -50);
        _panel.RevertCurrent();
        Assert.AreEqual(-50, ReputationSystem.GetRep("ghosts"));
    }

    [Test]
    public void PresetButtons_SetExpectedValues()
    {
        _panel.SetRepValue(50);
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        Assert.AreEqual(50, slider.value);
    }

    [Test]
    public void ExternalChangeUpdatesListLabelAndDetail()
    {
        ReputationSystem.SetRep("ghosts", -60);
        _panel.RevertCurrent();

        var label = FindListLabel("Ghosts");
        StringAssert.Contains("(-60)", label.text);

        var repValue = GetPrivate<Text>(_panel, "_repValue");
        Assert.AreEqual(new Color(0.85f, 0.3f, 0.3f), repValue.color);
    }

    [Test]
    public void SelectsSecondFaction_ShowsSecondRep()
    {
        _panel.SelectFaction(1);
        var repValue = GetPrivate<Text>(_panel, "_repValue");
        StringAssert.Contains("(Neutral)", repValue.text);
    }

    [Test]
    public void ApplyFiresOnRepChangedEvent()
    {
        int fired = 0;
        ReputationSystem.OnRepChanged += (_, __) => fired++;
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 15;
        _panel.ApplyCurrent();
        Assert.AreEqual(1, fired);
    }

    [Test]
    public void RevertRestoresExternalValue()
    {
        ReputationSystem.SetRep("ghosts", -5);
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 70;
        _panel.RevertCurrent();
        Assert.AreEqual(-5, Mathf.RoundToInt(slider.value));
    }

    [Test]
    public void NoSelection_ApplyRevertNoOp()
    {
        var emptyGo = new GameObject("EmptyLedgerTestGO");
        var emptyPanel = emptyGo.AddComponent<LedgerPanel>();
        emptyPanel.Initialize(onClose: null, injectedFactions: new List<FactionDefinition>());
        
        Assert.DoesNotThrow(() => emptyPanel.ApplyCurrent());
        Assert.DoesNotThrow(() => emptyPanel.RevertCurrent());
        
        Object.DestroyImmediate(emptyGo);
    }

    [Test]
    public void SliderWholeNumberClamp()
    {
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 12.7f;
        Assert.AreEqual(13, Mathf.RoundToInt(slider.value));
    }

    [Test]
    public void ListLabelsUpdateForAllFactions()
    {
        ReputationSystem.SetRep("inkbound", 55);
        var label = FindListLabel("Inkbound");
        StringAssert.Contains("(55)", label.text);
    }

    [Test]
    public void ApplyButton_UpdatesListLabel()
    {
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 40;
        _panel.ApplyCurrent();
        
        var label = FindListLabel("Ghosts");
        StringAssert.Contains("(40)", label.text);
    }

    [Test]
    public void PresetButtons_UpdateLabelAndColor()
    {
        _panel.SetRepValue(50);
        
        var repValue = GetPrivate<Text>(_panel, "_repValue");
        StringAssert.Contains("(Friendly)", repValue.text);
        Assert.AreEqual(new Color(0.35f, 0.8f, 0.45f), repValue.color);
    }

    #endregion

    #region Cross-Faction UI Tests

    [Test]
    public void SelectInterFactionPair_LoadsInterRep()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetInterRep("ghosts", "inkbound", -40);
        
        _panel.SelectInterFactionPair(0, 1); // ghosts -> inkbound
        
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        Assert.AreEqual(-40, Mathf.RoundToInt(slider.value));
    }

    [Test]
    public void ApplyInterRep_WritesToSystem()
    {
        ReputationSystem.ClearForTests();
        _panel.SelectInterFactionPair(0, 1); // ghosts -> inkbound
        
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 35;
        _panel.ApplyInterRep();
        
        Assert.AreEqual(35, ReputationSystem.GetInterRep("ghosts", "inkbound"));
    }

    [Test]
    public void RevertInterRep_ReloadsExternalChange()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetInterRep("ghosts", "inkbound", -10);
        _panel.SelectInterFactionPair(0, 1);
        
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 60;
        _panel.RevertInterRep();
        
        Assert.AreEqual(-10, Mathf.RoundToInt(slider.value));
    }

    [Test]
    public void PreventSelfTarget_NoOp()
    {
        ReputationSystem.ClearForTests();
        int calls = 0;
        ReputationSystem.OnInterRepChanged += (_, __, ___) => calls++;
        
        _panel.SelectInterFactionPair(0, 0); // ghosts -> ghosts (same)
        
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        slider.value = 50;
        _panel.ApplyInterRep();
        
        // Should not store or fire event for self-targeting
        Assert.AreEqual(0, ReputationSystem.GetInterRep("ghosts", "ghosts"));
        Assert.AreEqual(0, calls);
    }

    [Test]
    public void SwitchingPairs_PreservesValuesPerPair()
    {
        ReputationSystem.ClearForTests();
        ReputationSystem.SetInterRep("ghosts", "inkbound", -30);
        ReputationSystem.SetInterRep("inkbound", "ghosts", 20);
        
        _panel.SelectInterFactionPair(0, 1); // ghosts -> inkbound
        var slider = GetPrivate<Slider>(_panel, "_repSlider");
        Assert.AreEqual(-30, Mathf.RoundToInt(slider.value));
        
        _panel.SelectInterFactionPair(1, 0); // inkbound -> ghosts
        Assert.AreEqual(20, Mathf.RoundToInt(slider.value));
    }

    [Test]
    public void ExternalInterRepChange_UpdatesUI()
    {
        // Don't call ClearForTests here - it would clear the event subscription from Initialize()
        _panel.SelectInterFactionPair(0, 1); // ghosts -> inkbound
        
        // External change - this should trigger OnInterRepChanged and update the UI
        ReputationSystem.SetInterRep("ghosts", "inkbound", -70);
        
        var repValue = GetPrivate<Text>(_panel, "_repValue");
        StringAssert.Contains("-70", repValue.text);
    }

    [Test]
    public void InterRep_EmptyFactions_NoCrash()
    {
        var emptyGo = new GameObject("EmptyInterRepTestGO");
        var emptyPanel = emptyGo.AddComponent<LedgerPanel>();
        emptyPanel.Initialize(onClose: null, injectedFactions: new List<FactionDefinition>());
        
        Assert.DoesNotThrow(() => emptyPanel.SelectInterFactionPair(0, 1));
        Assert.DoesNotThrow(() => emptyPanel.ApplyInterRep());
        Assert.DoesNotThrow(() => emptyPanel.RevertInterRep());
        
        Object.DestroyImmediate(emptyGo);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void HostilityUsesUpdatedRep()
    {
        ReputationSystem.ClearForTests();
        
        var playerGo = new GameObject("TestPlayer");
        var player = playerGo.AddComponent<PlayerController>();
        var playerEntity = playerGo.AddComponent<GridEntity>();
        
        var npcGo = new GameObject("TestNPC");
        var npcEntity = npcGo.AddComponent<GridEntity>();
        var npcFactionMember = npcGo.AddComponent<FactionMember>();
        npcFactionMember.faction = _ghosts;
        
        ReputationSystem.SetRep(_ghosts.id, -30);
        Assert.IsTrue(HostilityService.IsHostile(player, npcEntity), "Should be hostile at rep -30");
        
        ReputationSystem.SetRep(_ghosts.id, 50);
        Assert.IsFalse(HostilityService.IsHostile(player, npcEntity), "Should not be hostile at rep +50");
        
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(npcGo);
    }

    [Test]
    public void InscribableSurfaceRaisesHeat()
    {
        var distDef = ScriptableObject.CreateInstance<DistrictDefinition>();
        distDef.id = "test_district";
        distDef.displayName = "Test District";
        distDef.minX = 0;
        distDef.maxX = 10;
        distDef.minY = 0;
        distDef.maxY = 10;
        
        var svcGo = new GameObject("TestDistrictControlService");
        var svc = svcGo.AddComponent<DistrictControlService>();
        svc.defaultHeat = 0.1f;
        svc.heatFromEdit = 0.15f;
        svc.heatBaselineDecay = 0.01f;
        svc.enableDebugLogs = false;
        
        var distDefsField = typeof(DistrictControlService).GetField("_districtDefs", BindingFlags.NonPublic | BindingFlags.Instance);
        var factionsField = typeof(DistrictControlService).GetField("_factions", BindingFlags.NonPublic | BindingFlags.Instance);
        var factionIndexField = typeof(DistrictControlService).GetField("_factionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        var statesField = typeof(DistrictControlService).GetField("_states", BindingFlags.NonPublic | BindingFlags.Instance);
        
        distDefsField.SetValue(svc, new List<DistrictDefinition> { distDef });
        factionsField.SetValue(svc, new List<FactionDefinition> { _ghosts });
        factionIndexField.SetValue(svc, new Dictionary<string, int> { { "ghosts", 0 } });
        
        var state = new DistrictState(distDef, 1);
        state.heat[0] = 0.1f;
        state.control[0] = 0.3f;
        state.patrol[0] = 0.3f;
        statesField.SetValue(svc, new List<DistrictState> { state });
        
        float initialHeat = state.heat[0];
        
        svc.ApplyPalimpsestEdit("test_district", 1f);
        svc.AdvanceDay();
        
        Assert.Greater(state.heat[0], initialHeat, "Heat should increase after palimpsest edit");
        
        Object.DestroyImmediate(svcGo);
        Object.DestroyImmediate(distDef);
    }

    [Test]
    public void ScrollContentSized()
    {
        var manyFactions = new List<FactionDefinition>();
        for (int i = 0; i < 25; i++)
        {
            var f = ScriptableObject.CreateInstance<FactionDefinition>();
            f.id = $"faction_{i}";
            f.displayName = $"Faction {i}";
            manyFactions.Add(f);
            ReputationSystem.SetRep(f.id, i * 4 - 50);
        }
        
        var scrollGo = new GameObject("ScrollTestGO");
        var scrollPanel = scrollGo.AddComponent<LedgerPanel>();
        scrollPanel.Initialize(onClose: null, injectedFactions: manyFactions);
        
        var scrollRect = scrollGo.GetComponentInChildren<ScrollRect>();
        Assert.IsNotNull(scrollRect, "ScrollRect should exist");
        Assert.IsNotNull(scrollRect.viewport, "Viewport should be assigned");
        Assert.IsNotNull(scrollRect.content, "Content should be assigned");
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        
        float contentHeight = scrollRect.content.rect.height;
        Assert.Greater(contentHeight, 0, "Content height should be positive");
        
        Object.DestroyImmediate(scrollGo);
        foreach (var f in manyFactions)
            Object.DestroyImmediate(f);
    }

    #endregion


    #region Inter-Faction Hostility Tests

    [Test]
    public void HostilityUsesInterRepWhenBothFactioned()
    {
        ReputationSystem.ClearForTests();
        
        // Create two NPCs with different factions
        var npc1Go = new GameObject("GhostNPC");
        var npc1Entity = npc1Go.AddComponent<GridEntity>();
        var npc1Faction = npc1Go.AddComponent<FactionMember>();
        npc1Faction.faction = _ghosts;
        
        var npc2Go = new GameObject("InkboundNPC");
        var npc2Entity = npc2Go.AddComponent<GridEntity>();
        var npc2Faction = npc2Go.AddComponent<FactionMember>();
        npc2Faction.faction = _inkbound;
        
        // Set asymmetric inter-faction rep
        ReputationSystem.SetInterRep("ghosts", "inkbound", -40); // ghosts hate inkbound
        ReputationSystem.SetInterRep("inkbound", "ghosts", 40);  // inkbound like ghosts
        
        // Ghost attacking Inkbound should be hostile (rep -40 <= -25 threshold)
        Assert.IsTrue(HostilityService.IsHostile(npc1Entity, npc2Entity), "Ghost should be hostile to Inkbound (rep -40)");
        
        // Inkbound attacking Ghost should NOT be hostile (rep +40 > -25 threshold)
        Assert.IsFalse(HostilityService.IsHostile(npc2Entity, npc1Entity), "Inkbound should not be hostile to Ghost (rep +40)");
        
        Object.DestroyImmediate(npc1Go);
        Object.DestroyImmediate(npc2Go);
    }

    [Test]
    public void InterFactionHostility_NeutralByDefault()
    {
        ReputationSystem.ClearForTests();
        // No inter-rep set - defaults to 0
        
        var npc1Go = new GameObject("GhostNPC2");
        var npc1Entity = npc1Go.AddComponent<GridEntity>();
        var npc1Faction = npc1Go.AddComponent<FactionMember>();
        npc1Faction.faction = _ghosts;
        
        var npc2Go = new GameObject("InkboundNPC2");
        var npc2Entity = npc2Go.AddComponent<GridEntity>();
        var npc2Faction = npc2Go.AddComponent<FactionMember>();
        npc2Faction.faction = _inkbound;
        
        // Neither should be hostile with default 0 rep
        Assert.IsFalse(HostilityService.IsHostile(npc1Entity, npc2Entity), "Ghost should not be hostile to Inkbound with neutral rep");
        Assert.IsFalse(HostilityService.IsHostile(npc2Entity, npc1Entity), "Inkbound should not be hostile to Ghost with neutral rep");
        
        Object.DestroyImmediate(npc1Go);
        Object.DestroyImmediate(npc2Go);
    }

    #endregion
    #region Helpers

    private Text FindListLabel(string displayName)
    {
        foreach (var txt in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
        {
            if (txt.text.StartsWith(displayName) && System.Text.RegularExpressions.Regex.IsMatch(txt.text, @"\(-?\d+\)"))
                return txt;
        }
        Assert.Fail("List label not found for " + displayName);
        return null;
    }

    private T GetPrivate<T>(object obj, string field) where T : class
    {
        return typeof(LedgerPanel).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                                  .GetValue(obj) as T;
    }

    #endregion
}
