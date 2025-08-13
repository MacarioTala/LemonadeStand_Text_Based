using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
/// <summary>
/// This class includes tests for FullfilmentInfoExtensions
/// </summary>
[TestFixture]
public class MarketObserverTests
{
    Good Lemon;
    Good Lemonade;
    Good Water;
    Good Sugar;

    EconAgent Company1;
    EconAgent Company2;

    Market TestMarket;

    readonly iSupplyProvider TestSupplyProvider = new BasicSupplyProvider();
    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    
    [SetUp]
    public void Setup()
    {
        Lemon = Good.CreateInstance("Lemon", new PriceBand(0.5m, 1.0m), RarityEnum.Common);
        Lemonade = Good.CreateInstance("Lemonade", new PriceBand(5.0m, 10m), RarityEnum.Uncommon);
        Water = Good.CreateInstance("Water", new PriceBand(0.1m, 0.5m), RarityEnum.Common);
        Sugar = Good.CreateInstance("Sugar", new PriceBand(0.2m, 0.8m), RarityEnum.Common);
        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company 2", AgentLevelEnum.Beginner);

        TestMarket = Market.Factory.CreateMarket("Test Market"
                                                , AgentLevelEnum.Market
                                                )
                .WithDemandStrategy(TestDemandStrategy)
                .WithSupplyProvider(TestSupplyProvider)
                .WithTradeProcessor(new BasicTradeProcessor())
                .WithPriceManager(new BasicPriceManager())
                .WithTransactionManager(new BasicTransactionManager());
    
        TestSupplyProvider.Initialize(TestMarket);

        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TheEconomy.Instance);
        TestMarket = null;
        Company1 = null;
        Company2 = null;
        Lemon = null;
        Lemonade = null;
        
    }

    [TestCase(TestName = "CalculateFulfillmentRates returns empty list when both demand and supply are empty")]
    public void CFR_EmptyDemandAndSupply_ReturnsEmptyList()
    {
        // Arrange
        var marketOrders = new List<Order>();

        var expectedFulfillmentInfoList = new List<FulfillmentInfo>();
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(marketOrders);
        // Assert   
        Assert.AreEqual(expectedFulfillmentInfoList.Count, result.Count);
    }

    [TestCase(TestName = "CalculateFulfillmentRates returns 100 when demand and supply are equal")]
    public void CFR_EqualDemandAndSupply_ReturnsFulfillmentInfoWith1()
    {
        // Arrange
        Company1.SetCash(1000);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 10, 1, 0));
        var marketDemand = new Dictionary<Good, DemandData>
        {
            { Lemon, new DemandData { CurrentDemand = 10 } }
        };
        var ordersInPeriod = new List<Order>
        {
            new(Company1, null, Lemon, 10, 1){FilledQuantity = 10, SubmittingCompany = Company1},
            new(null, Company2, Lemon, 10, 1){FilledQuantity = 10, SubmittingCompany = Company2}
        };
        var expectedLemonFulfillmentRate = 100f;
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(ordersInPeriod);
        var lemonFulfillmentRate = result.Find(x => x.Good == Lemon).FulfillmentRate;
        // Assert   
        Assert.AreEqual(expectedLemonFulfillmentRate, lemonFulfillmentRate);

    }

    [TestCase(TestName = "CalculateFulfillmentRates returns 0 when demand is 0 and supply is greater than 0")]
    public void CFR_ZeroDemandAndPositiveSupply_ReturnsFulfillmentInfoWith0()
    {
        // Arrange
        var marketDemand = new Dictionary<Good, DemandData>
        {
            { Lemon, new DemandData { CurrentDemand = 0 } }
        };
        
        var ordersInPeriod = new List<Order>
        {
            new(null, Company1, Lemon, 10, 1){FilledQuantity = 0, SubmittingCompany = Company1}
        };
        var expectedLemonFulfillmentRate = 0.0f;
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(ordersInPeriod);
        var lemonFulfillmentRate = result.Find(x => x.Good == Lemon).FulfillmentRate;
        
        // Assert   
        Assert.AreEqual(expectedLemonFulfillmentRate, lemonFulfillmentRate);
    }

    [TestCase(TestName = "CalculateFulfillmentRates returns 0 when demand is greater than 0 and supply is 0")]
    public void CFR_PositiveDemandAndZeroSupply_ReturnsFulfillmentInfoWith0()
    {
        // Arrange
        var marketDemand = new Dictionary<Good, DemandData>
        {
            { Lemon, new DemandData { CurrentDemand = 10 } }
        };
        var ordersInPeriod = new List<Order>
        {
            new(Company1, null, Lemon, 10, 1){SubmittingCompany = Company1}
        };
        var expectedLemonFulfillmentRate = 0.0f;
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(ordersInPeriod);
        var lemonFulfillmentRate = result.Find(x => x.Good == Lemon).FulfillmentRate;
        
        // Assert   
        Assert.AreEqual(expectedLemonFulfillmentRate, lemonFulfillmentRate);
    }
    [TestCase(TestName = "CalculateFulfillmentRates returns 50 when demand is 2 and supply is 1 with no market demand")]
    public void CFR_PositiveDemandAndSupply_ReturnsFulfillmentInfoWith50()
    {
        // Arrange
        var ordersInPeriod = new List<Order>
        {
            new(Company1, null, Lemon, 2, 1){FilledQuantity = 1, SubmittingCompany = Company1},
            new(null, Company2, Lemon, 1, 1){FilledQuantity = 1,SubmittingCompany = Company2}
        };
        var expectedLemonFulfillmentRate = 50f;
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(ordersInPeriod);
        var lemonFulfillmentRate = result.Find(x => x.Good == Lemon).FulfillmentRate;
        
        // Assert   
        Assert.AreEqual(expectedLemonFulfillmentRate, lemonFulfillmentRate);
    }
    [TestCase(TestName = "CalculateFulfillmentRates returns 0 when demand is 0 and supply is nonzero, market demand is zero")]
    public void CFR_ZeroDemandAndSupply_ReturnsFulfillmentInfoWith0()
    {
        // Arrange
        var marketDemand = new Dictionary<Good, DemandData>
        {
            { Lemon, new DemandData { CurrentDemand = 0 } }
        };
        var ordersInPeriod = new List<Order>
        {
            new(Company1, null, Lemon, 10, 1)
        };
            
        var expectedLemonFulfillmentRate = 0.0f;
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(ordersInPeriod);
        var lemonFulfillmentRate = result.Find(x => x.Good == Lemon).FulfillmentRate;
        
        // Assert   
        Assert.AreEqual(expectedLemonFulfillmentRate, lemonFulfillmentRate);
    }
    [TestCase(TestName = "DemandFulfillmentRate returns 50 when demand is 2 and supply is 1(standalone)")]
    public void DemandFulfillmentRate_PositiveDemandAndSupply_Returns50()
    {
        // Arrange
        var fulfillmentInfo = new FulfillmentInfo(Lemon, 2, 1, 1, 1);
        var expectedSupplyShortageRate = 50f;
        // Act
        var result = fulfillmentInfo.DemandFulfillmentRate;
        // Assert   
        Assert.AreEqual(expectedSupplyShortageRate, result);
    }
    [TestCase(TestName = "SupplyFulfilmentRate returns 100 when demand is 2 and supply is 1(standalone)")]
    public void SupplyFulfilmentRate_Demand2Supply1_returns100()
    {
        // Arrange
        var fulfillmentInfo = new FulfillmentInfo(good: Lemon, totalDemand: 2,totalSupply: 1,
            filledSupply: 1, filledDemand: 1);
        var expectedSupplyExcessRate = 100f;
        // Act
        var result = fulfillmentInfo.SupplyFulfillmentRate;
        // Assert   
        Assert.AreEqual(expectedSupplyExcessRate, result);
    }
    [TestCase(TestName = "CalculateFulfillmentRates returns demand fulfillment rate of 50 when demand is 2 and supply is 1")]
    public void CFR_DemandFulfillmentRate_Demand2Supply1_Returns50()
    {
        // Arrange
        var marketDemand = new Dictionary<Good, DemandData>
        {
            { Lemon, new DemandData { CurrentDemand = 2 } }
        };
        var ordersInPeriod = new List<Order>
        {
            new(null, Company1, Lemon, 1, 1){FilledQuantity = 1, SubmittingCompany = Company1},
            new(Company2, null, Lemon, 2, 1){FilledQuantity = 1, SubmittingCompany = Company2}
        };
        var expectedLemonFulfillmentRate = 50f;
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(ordersInPeriod);
        var lemonFulfillmentRate = result.Find(x => x.Good == Lemon).DemandFulfillmentRate;
        
        // Assert   
        Assert.AreEqual(expectedLemonFulfillmentRate, lemonFulfillmentRate);
    }
    [TestCase(TestName = "CalculateFulfillmentRates returns SupplyExcessRate of 100 when demand is 1 and supply is 2. Market demand is 1")]
    public void CFR_SupplyExcessRate_Demand1Supply3_Returns100()
    {
        // Arrange
        var marketDemand = new Dictionary<Good, DemandData>
        {
            { Lemon, new DemandData { CurrentDemand = 1 } }
        };
        
        var ordersInPeriod = new List<Order>
        {
            new(null, Company1, Lemon, 2, 1){FilledQuantity = 1, SubmittingCompany = Company1},
            new(Company2, null, Lemon, 1, 1){FilledQuantity = 1, SubmittingCompany = Company2}
        };
        var expectedLemonSupplyExcessRate = 100f;
        // Act
        var result = MarketObserver.CalculateFulfillmentRates(ordersInPeriod);
        var actualLemonExcessRate = result.Find(x => x.Good == Lemon).SupplyExcessRate;
        
        // Assert   
        Assert.AreEqual(expectedLemonSupplyExcessRate, actualLemonExcessRate);
    }
#region FulfillmentInfoExtension tests
    [TestCase(TestName = "FulfillmentInfoExtension returns 0 when demand is 0 and supply is greater than 0")]
    public void FIE_ZeroDemandAndPositiveSupply_ReturnsFulfillmentInfoWith0()
    {
        // Arrange
        var fulfillmentInfo = new FulfillmentInfo(Lemon, 0, 10, 0, 0);
        var expectedFulfillmentRate = 0.0f;
        // Act
        var result = fulfillmentInfo.FulfillmentRate;
        // Assert   
        Assert.AreEqual(expectedFulfillmentRate, result);
    }
    
    
#endregion
}