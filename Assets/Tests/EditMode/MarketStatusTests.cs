using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using static TestHelpers;
[TestFixture]
public class MarketStatusTests
{
    TheEconomy TestEconomy;
    Market TestMarket;
    EconAgent Company1;
    EconAgent Company2;
    int Period;

    Good lemon;
    Good water;
    Good sugar;

    readonly TestComparer<Execution> ExecutionComparer=new(new string[] { "CounterPartyTrades" });

    [SetUp]
    public void Setup ()
    {
        Period = 0;
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithTransactionManager(new BasicTransactionManager())
            .WithPriceManager(new BasicPriceManager())
            .WithSupplyProvider(new BasicSupplyProvider())
            .WithDemandStrategy(ScriptableObject.CreateInstance<LinearDemandStrategy>())
            .WithDemographicManager(new BasicDemographicManager());
        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company 2", AgentLevelEnum.Beginner);
        
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);

        lemon = Good.CreateInstance("Lemon", new PriceBand(.5m, 1.0m), RarityEnum.Common);
        water = Good.CreateInstance("Water", new PriceBand(.5m, 1.0m), RarityEnum.Common);
        sugar = Good.CreateInstance("Sugar", new PriceBand(.5m, 1.0m), RarityEnum.Common);
    }
    [Test]
    public void GetOrdersExecutedInPeriodReturnsAllExecutedOrders()
    {
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(lemon, 2000,3m,Period));
        Company1.SetCash(5000);

        Company2.GetInventory().AddGood(new InventoryEntry(water, 2000,3m,Period));
        Company2.SetCash(5000);

        var company1SellLemonOrder = new Order(null, Company1, lemon, 500, 3.0m);
        var company2SellWaterOrder = new Order(null, Company2, water, 500, 3.0m);
        var company1BuyWaterOrder = new Order(Company1,null,water,500,3.0m);
        var company2BuyLemonOrder = new Order(Company2,null,lemon,500,3.0m);
        
        Company1.QueueOrder(CreateActionContext(company1SellLemonOrder, TestMarket, Period));
        Company1.QueueOrder(CreateActionContext(company1BuyWaterOrder, TestMarket, Period));
        Company2.QueueOrder(CreateActionContext(company2SellWaterOrder, TestMarket, Period));
        Company2.QueueOrder(CreateActionContext(company2BuyLemonOrder, TestMarket, Period));

        var expected = new List<(Order Order,int Period)>
        {
            new(company1SellLemonOrder, Period),
            new(company2SellWaterOrder, Period),
            new(company1BuyWaterOrder, Period),
            new(company2BuyLemonOrder, Period)
        };
        // Act
        TestMarket.ProcessCompanyOrders();
        var actual=TestMarket.GetOrdersExecutedInPeriod(Period);
        // Assert
        Assert.AreEqual(expected.Count, actual.Count);
        CollectionAssert.AreEquivalent(expected,actual);
    }
    [Test]
    public void GetOrdersExecutedInPeriodOnlyReturnsTradesForTheCurrentPeriod()
    {
        // Arrange
        var strategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        var marketToTest = Market.Factory.CreateStarterMarket("Market To Test", AgentLevelEnum.Market,strategy);
        
        Company1.GetInventory().AddGood(new InventoryEntry(lemon, 2000,3m,Period));
        Company1.SetCash(5000);

        Company2.GetInventory().AddGood(new InventoryEntry(water, 2000,3m,Period));
        Company2.SetCash(5000);

        //Period 0 orders
        var company1SellLemonOrder = new Order(null, Company1, lemon, 500, 3.0m);
        var company2BuyLemonOrder = new Order(Company2,null,lemon,500,3.0m);

        //Period 1 orders
        var company2SellWaterOrder = new Order(null, Company2, water, 500, 3.0m);
        var company1BuyWaterOrder = new Order(Company1,null,water,500,3.0m);
        
        Company1.QueueOrder(CreateActionContext(company1SellLemonOrder, marketToTest, Period));
        Company2.QueueOrder(CreateActionContext(company2BuyLemonOrder, marketToTest, Period));

        
        var expected = new List<(Order Order,int Period)>
        {
            new(company1SellLemonOrder, Period),
            new(company2BuyLemonOrder, Period)
        };
        // Act
        marketToTest.ProcessCompanyOrders();
        Company2.QueueOrder(CreateActionContext(company2SellWaterOrder, marketToTest, Period+1));
        Company1.QueueOrder(CreateActionContext(company1BuyWaterOrder, marketToTest, Period+1));
        marketToTest.CurrentPeriod++;
        marketToTest.ProcessCompanyOrders();
        var actual=marketToTest.GetOrdersExecutedInPeriod(Period);
        // Assert
        Assert.AreEqual(expected.Count, actual.Count);
        CollectionAssert.AreEquivalent(expected,actual);
    }

    [Test]
    public void PeriodsWithNoCounterPartiesRecordNoTrades()
    {
        // Arrange
        var Company1BuyLemonOrder = new Order(Company1,null,lemon,500,3.0m);
        var Company2BuyWaterOrder = new Order(Company2,null,water,500,3.0m);
        Company1.QueueOrder(CreateActionContext(Company1BuyLemonOrder, TestMarket, Period));
        Company2.QueueOrder(CreateActionContext(Company2BuyWaterOrder, TestMarket, Period));
        // Act
        TestMarket.ProcessCompanyOrders();
        var actual=TestMarket.GetExecutionsInPeriod(Period);
        // Assert
        Assert.IsEmpty(actual);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy.gameObject);
        TestMarket = null;
        Company1 = null;
        Company2 = null;
    }
}