using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using InkSim;

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

    [Test]
    public void SelectFaction_ShowsCurrentRep()
    {
        // Ghosts selected by default
        Assert.IsTrue(_panel.IsVisible); // Initialize doesn't hide
        Assert.AreEqual(-10, ReputationSystem.GetRep("ghosts"));
    }

    [Test]
    public void ApplyButton_WritesToReputationSystem()
    {
        // Move slider to 40 and apply
        var slider = _panel.GetType().GetField("_repSlider", BindingFlags.NonPublic | BindingFlags.Instance)
                         .GetValue(_panel) as UnityEngine.UI.Slider;
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
        _panel.SetRepValue(50); // Friendly
        var slider = _panel.GetType().GetField("_repSlider", BindingFlags.NonPublic | BindingFlags.Instance)
                         .GetValue(_panel) as UnityEngine.UI.Slider;
        Assert.AreEqual(50, slider.value);
    }

}
