using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты строительства: инвентарь, рецепты, стройплощадка.</summary>
    public class BuildingTests
    {
        // ---------- инвентарь ----------

        [Test]
        public void Inventory_AddGetConsumeTake()
        {
            var inv = new Inventory();
            Assert.AreEqual(0, inv.Get(ResourceId.Wood));
            inv.Add(ResourceId.Wood, 100);
            Assert.AreEqual(100, inv.Get(ResourceId.Wood));
            Assert.IsTrue(inv.Has(ResourceId.Wood, 50));
            Assert.IsFalse(inv.Has(ResourceId.Wood, 200));

            Assert.IsFalse(inv.TryConsume(ResourceId.Wood, 200), "нельзя списать больше, чем есть");
            Assert.IsTrue(inv.TryConsume(ResourceId.Wood, 30));
            Assert.AreEqual(70, inv.Get(ResourceId.Wood));

            Assert.AreEqual(70, inv.Take(ResourceId.Wood, 999), "взять можно не больше, чем есть");
            Assert.AreEqual(0, inv.Get(ResourceId.Wood));
        }

        [Test]
        public void Inventory_IgnoresNonPositive()
        {
            var inv = new Inventory();
            inv.Add(ResourceId.Stone, -5);
            inv.Add(ResourceId.Stone, 0);
            Assert.AreEqual(0, inv.Get(ResourceId.Stone));
        }

        // ---------- рецепты ----------

        [Test]
        public void Recipes_OakHouseMatchesSpec()
        {
            var oak = BuildRecipes.ById("oak_house");
            Assert.IsNotNull(oak);
            Assert.AreEqual(500, oak.Cost(ResourceId.Wood));
            Assert.AreEqual(250, oak.Cost(ResourceId.Stone));
            Assert.AreEqual(150, oak.Cost(ResourceId.Branch));
            Assert.AreEqual(100, oak.Cost(ResourceId.Clay));
            Assert.AreEqual(1000, oak.TotalCost());
            Assert.AreEqual("house_wood", oak.sprite);
        }

        [Test]
        public void Recipes_AllHaveCostAndSprite()
        {
            Assert.Greater(BuildRecipes.All.Length, 0);
            foreach (var b in BuildRecipes.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(b.id));
                Assert.IsFalse(string.IsNullOrEmpty(b.sprite));
                Assert.Greater(b.TotalCost(), 0, $"{b.id}: нулевая стоимость");
            }
            Assert.IsNull(BuildRecipes.ById("nope"));
        }

        // ---------- стройплощадка ----------

        [Test]
        public void Site_DepositClampsAndCompletes()
        {
            var site = new ConstructionSite(BuildRecipes.ById("oak_house"));
            Assert.IsFalse(site.IsComplete);
            Assert.AreEqual(0f, site.Progress, 1e-4f);

            // внести больше требуемого — лишнее не уходит
            Assert.AreEqual(500, site.Deposit(ResourceId.Wood, 999));
            Assert.AreEqual(0, site.Remaining(ResourceId.Wood));
            Assert.AreEqual(0, site.Deposit(ResourceId.Wood, 100), "когда заполнено — больше не принимает");

            site.Deposit(ResourceId.Stone, 250);
            site.Deposit(ResourceId.Branch, 150);
            Assert.IsFalse(site.IsComplete, "ещё не хватает глины");

            site.Deposit(ResourceId.Clay, 100);
            Assert.IsTrue(site.IsComplete);
            Assert.AreEqual(1f, site.Progress, 1e-4f);
        }

        [Test]
        public void Site_PartialDepositProgress()
        {
            var site = new ConstructionSite(BuildRecipes.ById("oak_house"));
            site.Deposit(ResourceId.Wood, 250);   // половина от 500
            // 250 из 1000 всего → ~0.25
            Assert.AreEqual(0.25f, site.Progress, 1e-3f);
            Assert.AreEqual(250, site.Remaining(ResourceId.Wood));
        }
    }
}
