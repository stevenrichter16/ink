using NUnit.Framework;
using System.Linq;

namespace InkSim.Tests
{
    /// <summary>
    /// TDD for item/merchant schema extras: rarity, weight, merchant filters.
    /// </summary>
    public class MerchantSchemaTests
    {
        [Test]
        public void ItemData_HasRarityAndWeight_Defaults()
        {
            var data = new ItemData("test_item", "Test", ItemType.Consumable, 0);
            Assert.IsNotNull(data, "ItemData should be constructible");
            Assert.AreEqual(ItemRarity.Common, data.rarity);
            Assert.AreEqual(0f, data.weight);
        }

        [Test]
        public void MerchantProfile_HasHomeDistrictAndAcceptedTypes()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<MerchantProfile>();
            profile.acceptedTypes = new System.Collections.Generic.List<ItemType> { ItemType.Weapon };
            profile.homeDistrictId = "outer_slums";
            Assert.AreEqual("outer_slums", profile.homeDistrictId);
            Assert.AreEqual(1, profile.acceptedTypes.Count);
        }

        [Test]
        public void MerchantProfile_EmptyAcceptedTypes_AllowsAll()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<MerchantProfile>();
            profile.acceptedTypes = new System.Collections.Generic.List<ItemType>(); // empty means all
            Assert.IsTrue(profile.CanTrade(ItemType.Consumable));
            Assert.IsTrue(profile.CanTrade(ItemType.Weapon));
        }

        [Test]
        public void MerchantProfile_FiltersUnacceptedTypes()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<MerchantProfile>();
            profile.acceptedTypes = new System.Collections.Generic.List<ItemType> { ItemType.Weapon, ItemType.Armor };
            Assert.IsTrue(profile.CanTrade(ItemType.Weapon));
            Assert.IsFalse(profile.CanTrade(ItemType.Consumable));
        }
    }
}
