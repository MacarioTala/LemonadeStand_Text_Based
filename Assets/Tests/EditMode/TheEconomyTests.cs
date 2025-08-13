using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class TheEconomyTests
{
    private TheEconomy testEconomy;

    private Market testInitialMarket;
    Good lemon;
    Good water;
    Good sugar;

    readonly PriceBand band1 = new(.5m, 1.0m);
    readonly PriceBand band2 = new(1.0m, 3.0m);
    readonly ITradeLogger trade_logger = new MockLogger();
    readonly List<Good> test_goods = new();

    [SetUp]
    public void SetUp()
    {
        //Create the economy
        var market_object = new GameObject();
        testEconomy = market_object.AddComponent<TheEconomy>();
        testEconomy.Initialize(trade_logger);
        testInitialMarket = (Market)testEconomy.GetGlobalMarket(); 

        lemon = Good.CreateInstance("Lemon", band2, RarityEnum.Common);
        water = Good.CreateInstance("Water", band1, RarityEnum.Common);
        sugar = Good.CreateInstance("Sugar", band1, RarityEnum.Common);
        lemon.ExpiresAfterPeriods = 1;
        test_goods.Add(lemon);
        test_goods.Add(water);
        test_goods.Add(sugar);

        //make the market demand a thousand lemons
        testInitialMarket.InitializeDemandForSpecificGood(lemon, 1000);
    }
#region  Initialization tests
  [Test]
    public void Make_sure_the_first_market_exists_in_TheEconomy_with_proper_params()
    {
        // Arrange
        var expected_name = "The First Market";
        // Act
        var actual = testEconomy.GetGlobalMarket().Name;
        // Assert
        Assert.AreEqual(expected_name, actual);
    }

    [Test]
    public void The_initial_market_should_be_instantiated_as_a_market()
    {
        // Arrange
        var expected = typeof(Market);
        // Act
        var actual = testInitialMarket.GetType();
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void WhenTheEconomyInitializesGoodsAreCreatedInInitialMarket()
    {
        // Arrange
        var expected = "Lemon";
        // Act
        testEconomy.CreateInitialGoods(test_goods);
        var actual = testInitialMarket.GetInventory().GetInventoryEntries().FirstOrDefault(x => x.good.GoodName == "Lemon").good.GoodName;
        // Assert
        Assert.AreEqual(expected, actual);
    }
#endregion
    [Test]
    public void Register_Company_adds_company_to_companies_list_if_no_companies_are_registered()
    {
        // Arrange
        var company = ScriptableObject.CreateInstance<EconAgent>();
        company.Name = "Test Company";
        var expected = testEconomy.companies.Count + 1;
        // Act
        testEconomy.RegisterCompany(company);
        var actual = testEconomy.companies.Count;
        
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void Register_Company_does_not_add_company_to_companies_list_if_company_already_registered()
    {
        // Arrange
        var company = ScriptableObject.CreateInstance<EconAgent>();
        company.Name = "Test Company";
        testEconomy.RegisterCompany(company);
        var company2 = ScriptableObject.CreateInstance<EconAgent>();
        company2.Name = "Test Company";
        // Act
        // Assert
        Assert.Throws<TheEconomy_CompanyException>(() => testEconomy.RegisterCompany(company2));
    }

    [Test]
    public void CreateInitialGoodsCreates1to1000GoodsIfRarityIsCommon()
    {
        // Arrange
        const int expectedFloor = 1;
        const int expectedCeiling = 1000;
        testEconomy.ClearEconomy();
        var testInitialMarket = (Market)testEconomy.GetGlobalMarket();
        testInitialMarket.GetInventory().Clear();
        // Act
        testEconomy.CreateInitialGoods(test_goods);
        var actualGood = testInitialMarket.GetInventory().GetInventoryEntriesByGood(lemon.GoodName).FirstOrDefault();
        var actualQuantity = actualGood.quantity;
        // Assert
        Assert.IsTrue(actualQuantity >= expectedFloor && actualQuantity <= expectedCeiling, 
        "Expected between:"+expectedFloor+" and "+
        expectedCeiling + 
        "Actual quantity: " + actualQuantity ); 
        
    }

    [TearDown]
    public void TearDown()
    {
        testEconomy.ClearEconomy();
        Object.DestroyImmediate(testEconomy.gameObject);
        Object.DestroyImmediate(testInitialMarket);
        typeof(TheEconomy).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, null);
    }


}
