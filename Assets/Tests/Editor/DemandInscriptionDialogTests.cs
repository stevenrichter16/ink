using NUnit.Framework;

namespace InkSim.Tests
{
    public class DemandInscriptionDialogTests
    {
        [Test]
        public void ItemOptions_ContainKnownItems()
        {
            ItemDatabase.Initialize();
            var options = DemandInscriptionDialog.GetItemOptions();
            Assert.IsNotNull(options);
            CollectionAssert.Contains(options, "potion");
            CollectionAssert.Contains(options, "gem");
        }
    }
}
