using System;
using System.Reflection;
using NUnit.Framework;

namespace InkSim.Tests
{
    /// <summary>
    /// Red tests for the economic prerequisites (TDD). These must fail until the
    /// economic plumbing is implemented.
    /// </summary>
    public class EconomicPrereqTests
    {
        [Test]
        public void EconomicPriceResolver_TypeExists()
        {
            var t = Type.GetType("InkSim.EconomicPriceResolver, Assembly-CSharp");
            Assert.IsNotNull(t, "Expected EconomicPriceResolver class to exist in InkSim namespace.");
        }

        [Test]
        public void PalimpsestLayer_ContainsEconomicFields()
        {
            var t = Type.GetType("InkSim.PalimpsestLayer, Assembly-CSharp");
            Assert.IsNotNull(t, "PalimpsestLayer type missing.");

            Assert.IsNotNull(t.GetField("taxDelta", BindingFlags.Public | BindingFlags.Instance),
                "PalimpsestLayer should expose public float taxDelta");
            Assert.IsNotNull(t.GetField("priceMultiplier", BindingFlags.Public | BindingFlags.Instance),
                "PalimpsestLayer should expose public float priceMultiplier");
        }

        [Test]
        public void PalimpsestRules_ContainsEconomicFields()
        {
            var t = Type.GetType("InkSim.OverlayResolver+PalimpsestRules, Assembly-CSharp");
            Assert.IsNotNull(t, "OverlayResolver.PalimpsestRules type missing.");

            Assert.IsNotNull(t.GetField("taxModifier"), "PalimpsestRules should contain taxModifier field.");
            Assert.IsNotNull(t.GetField("priceMultiplier"), "PalimpsestRules should contain priceMultiplier field.");
        }

        [Test]
        public void DistrictState_HasProsperityField()
        {
            var t = Type.GetType("InkSim.DistrictState, Assembly-CSharp");
            Assert.IsNotNull(t, "DistrictState type missing.");
            Assert.IsNotNull(t.GetField("prosperity", BindingFlags.Public | BindingFlags.Instance),
                "DistrictState should expose public float prosperity.");
        }
    }
}
