using NUnit.Framework;
using UnityEngine;
using System;
using System.Linq;
using static TestHelpers;
using NUnit.Framework.Internal;

[TestFixture]
public class SupplyAndDemandTests
{
    int Period = 0;
    public Market TestMarket;
    EconAgent Company1;
    EconAgent Company2;
    public PopulationAgent TestPopulation;
    public iStrategy TestReduceEnnuiStrategy;

    Good lemon;
    Good water;
    Good sugar;
    Good lemonade;
    readonly ITradeLogger MockTradeLogger = new MockLogger();
    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    readonly iMarketDataService TestMarketDataService = new MockMarketDataService();
    readonly iDemographicManager TestDemographicManager = new MockDemographicManager();
    readonly iSupplyProvider TestSupplyProvider = new BasicSupplyProvider();

    TheEconomy TestEconomy;

    [SetUp]
    public void Setup()
    {
        // Set up the Economy
        TheEconomy.SetupForTests(MockTradeLogger);
        TestEconomy = TheEconomy.Instance;

        // Set up goods
        lemon = new GoodBuilder()
            .Named("Lemon")
            .WithRarity(RarityEnum.Common)
            .Costing(.1m)
            .Build();

        water = new GoodBuilder().Named("Water")
            .WithRarity(RarityEnum.Common)
            .Costing(.1m)
            .Build();

        sugar = new GoodBuilder().Named("Sugar")
            .WithRarity(RarityEnum.Common)
            .Costing(.1m)
            .Build();

        lemonade = new GoodBuilder().Named("Lemonade")
            .WithRarity(RarityEnum.Uncommon)
            .Costing(3m)
            .WhichIsProducedGood()
            .Build();

        var reduceEnnuiEffect = new GoodEffect()
            .Named("Reduce Ennui")
            .DescribedAs("Reduces ennui by 0.1")
            .Affecting(MetricEnum.Ennui)
            .WithEffect(new MetricModifier<PopulationAgent>(
                c => c.Ennui,
                (c, newValue) => c.Ennui = newValue))
            .WithEffectMagnitude(-0.4f);

        lemonade.AddElasticity(ElasticityTypeEnum.SaturationElasticity, 1f);
        lemonade.AddEffect(reduceEnnuiEffect);

        // set test_market to the Initial Market
        var existingMarket = TestEconomy.GetMarketByName("The First Market");
        TheEconomy.Instance.RemoveMarket(existingMarket);
        TestMarket = Market.Factory.CreateMarket("Supply and Demand Test Market"
                                                , AgentLevelEnum.Market)
                                                .WithDemandStrategy(TestDemandStrategy)
                                                .WithTradeProcessor(new BasicTradeProcessor())
                                                .WithTransactionManager(new BasicTransactionManager())
                                                .WithDataService(TestMarketDataService)
                                                .WithDemographicManager(TestDemographicManager)
                                                .WithSupplyProvider(TestSupplyProvider)
                                                .WithPriceManager(new BasicPriceManager())
                                                .WithPriceModifier(new SupplyDemandModifier());
        TestEconomy.RegisterCompany(TestMarket);
        TestSupplyProvider.Initialize(TestMarket);

        //Create population dependencies
        TestReduceEnnuiStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                .WithAggressionLevel(.55m)
                .Build();

        //Create a population company to use in tests
        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Market)
            .WithInitialCash(10000)
            .WithBehaviourStrategy(TestReduceEnnuiStrategy)
            .WithEnnui(.99f)
            .WithPopulation(1000)
            .Build();

        // Create companies
        Company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company2", AgentLevelEnum.Beginner);

        //Register market participants
        TestMarket.RegisterMarketParticipant(TestPopulation);
        TestMarket.RegisterMarketParticipant(Company1);

        //Initialise strategies
        TestReduceEnnuiStrategy.GenerateGoals(TestPopulation);
    }
    [TearDown]
    public void TearDown()
    {
        ScriptableObject.DestroyImmediate(TestMarket);
        ScriptableObject.DestroyImmediate(lemon);
        ScriptableObject.DestroyImmediate(water);
        ScriptableObject.DestroyImmediate(sugar);
        GameObject.DestroyImmediate(TestEconomy);
        TestPopulation = null;
        TestMarket = null;
        Company1 = null;
        TestReduceEnnuiStrategy = null;
    }
    #region MinDemand and MaxDemand tests

    [Test]
    public void WhenMaxDemandIsZeroNoOrdersAreProcessed()
    {
        //Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 0 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);
        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 2000, 3m, 0));
        var company1Order = new Order(null, Company1, lemonade, 1000, 3.5m);

        var expected = 0;
        //Act
        Company1.QueueOrder(CreateActionContext(company1Order, TestMarket, Period));
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actual = company1Order.FilledQuantity;
        //Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void TwoOrdersFillWhenLessThanMaxDemandedQuantity()
    {
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);

        //Arrange
        var company1Order = new Order(null, Company1, lemonade, 500, 3.5m);
        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 500, 3m, 0));
        var company2Order = new Order(null, Company2, lemonade, 500, 3.5m);
        Company2.GetInventory().AddGood(new InventoryEntry(lemonade, 500, 3m, 0));

        var expectedFilledQuantityForCompany1 = 500;
        var expectedFilledQuantityForCompany2 = 500;
        //Act
        var result1 = Company1.QueueOrder(CreateActionContext(company1Order, TestMarket, Period));
        var result2 = Company2.QueueOrder(CreateActionContext(company2Order, TestMarket, Period));

        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();

        var actualFilledQuantityForCompany1 = company1Order.FilledQuantity;
        var actualFilledQuantityForCompany2 = company2Order.FilledQuantity;
        //Assert
        Assert.AreEqual(expectedFilledQuantityForCompany1, actualFilledQuantityForCompany1);
        Assert.AreEqual(expectedFilledQuantityForCompany2, actualFilledQuantityForCompany2);
    }
    #endregion
    #region Order Priority tests
    [Test]
    public void PopulationFillsOrderWithLowestPriceFirst()
    {
        //Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);
        TestPopulation.SetCash(5000);
        const int company1Quantity = 900;
        const int company2Quantity = 1000;
        const decimal company1Ask = 2m;
        const decimal company2Ask = 3m;
        const decimal lemonadeAcquisitionCost = 1m;
        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, company1Quantity, lemonadeAcquisitionCost, 0));
        Company2.GetInventory().AddGood(new InventoryEntry(lemonade, company2Quantity, lemonadeAcquisitionCost, 0));

        var company1Order = new Order(null, Company1, lemonade, company1Quantity, company1Ask);
        var company2Order = new Order(null, Company2, lemonade, company2Quantity, company2Ask);
        var expectedCompany1FilledQuantity = 900;
        var expectedCompany2FilledQuantity = 100;
        //Act
        Company1.QueueOrder(CreateActionContext(company1Order, TestMarket, Period));
        Company2.QueueOrder(CreateActionContext(company2Order, TestMarket, Period));
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actualFilledQuantity = company1Order.FilledQuantity;
        //Assert
        Assert.AreEqual(expectedCompany1FilledQuantity, actualFilledQuantity);
        Assert.AreEqual(expectedCompany2FilledQuantity, company2Order.FilledQuantity);
    }
    [Test]
    public void PopulationFillsOrderIfNoCounterpartyBuys()
    {
        //Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);

        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 2000, 3m, 0));

        var Company1SellsLemonadeToAnyone = new Order(null, Company1, lemonade, 1000, 3.5m);

        var ExpectedCompany1LemonadeSellFillQuantity = 1000;

        //Act
        Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone, TestMarket, Period));
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var ActualCompany1LemonadeSellFillQuantity = Company1SellsLemonadeToAnyone.FilledQuantity;

        //Assert
        Assert.AreEqual(ExpectedCompany1LemonadeSellFillQuantity,
                        ActualCompany1LemonadeSellFillQuantity,
                        $"Expected {ExpectedCompany1LemonadeSellFillQuantity} but got {ActualCompany1LemonadeSellFillQuantity}");
    }
    [Test]
    public void PopulationFillsAllSellOrdersIfBelowMaxDemand()
    {
        //Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 2000 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);

        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 1000, 3m, 0));
        Company2.GetInventory().AddGood(new InventoryEntry(lemonade, 1000, 3m, 0));

        var company1Order = new Order(null, Company1, lemonade, 400, 3.5m);
        var company2Order = new Order(null, Company2, lemonade, 400, 3.5m);

        var expectedFilledQuantityForCompany1 = 400;
        var expectedFilledQuantityForCompany2 = 400;
        //Act
        Company1.QueueOrder(CreateActionContext(company1Order, TestMarket, Period));
        Company2.QueueOrder(CreateActionContext(company2Order, TestMarket, Period));
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actualFilledQuantityForCompany1 = company1Order.FilledQuantity;
        var actualFilledQuantityForCompany2 = company2Order.FilledQuantity;
        //Assert
        Assert.AreEqual(expectedFilledQuantityForCompany1, actualFilledQuantityForCompany1);
        Assert.AreEqual(expectedFilledQuantityForCompany2, actualFilledQuantityForCompany2);
    }
    [Test]
    public void PopulationOnlyPartiallyFillsWhenSupplyExceedsDemand()
    {
        //Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);
        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 2000, 3m, 0));

        var company1SellsLemonadeToAnyone = new Order(null, Company1, lemonade, 1500, 3.5m);
        var expected = 1000;
        //Act
        Company1.QueueOrder(CreateActionContext(company1SellsLemonadeToAnyone, TestMarket, Period));
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actual = company1SellsLemonadeToAnyone.FilledQuantity;
        //Assert
        Assert.AreEqual(expected, actual);
    }
    #endregion
    #region UpdatePrices tests
    [Test]
    public void UpdatePricesIncreasesPriceByPriceIncrementRateWhenDemandThresholdIsReached()
    {
        throw new NotImplementedException("Update to use PopulationCompany.");
        // Arrange
        var testMarket = TestMarket;
        var testPeriod = 0;
        var company1 = EconAgent.Factory.Create("Test Company 1", AgentLevelEnum.Beginner);
        company1.GetInventory().AddGood(new InventoryEntry(lemonade, 2000, 3.0m, 0));
        testMarket.RegisterMarketParticipant(company1);

        var buyLemonadeOrder = new Order(null, company1, lemonade, 500, 3.0m);
        var buyLemonadeContext = new ActionContext { TradeToSubmit = buyLemonadeOrder, MarketToSubmitTo = testMarket, Period = testPeriod };
        company1.QueueOrder(buyLemonadeContext);
        var currentLemonadePrice = lemonade.GetPrice();
        var priceIncrementRate = lemonade.Get_price_increment_rate();
        var expectedLemonPrice = Math.Round(currentLemonadePrice * (1 + priceIncrementRate), 2);
        // Act
        testMarket.ProcessCompanyOrders();
        testMarket.UpdatePrices();
        // Only one entry per good in market inventories
        var actualLemonade = testMarket.GetInventory().GetInventoryEntriesByGood(lemonade.GoodName).FirstOrDefault();
        var actualLemonadePrice = Math.Round(actualLemonade.good.GetPrice(), 2);
        // Assert
        Assert.AreEqual(expectedLemonPrice, actualLemonadePrice);
    }

    [Test]
    [Ignore("FulfillDemand is obsolete. We still need to update this test case.")]
    public void IfMarketBuyingInAPeriodExceedsDemandThresholdIncreasePrices()
    {
        throw new NotImplementedException("Update to use PopulationCompany.");
        // Arrange
        var marketToTest = Market.Factory.CreateStarterMarket("Market To Test", AgentLevelEnum.Market, TestDemandStrategy);
        marketToTest.InitializeDemandForSpecificGood(lemonade, 1000);
        var demographicManger = new MockDemographicManager();
        demographicManger.SetMarketInstability(1f);
        marketToTest.SetDemographicManager(demographicManger);

        var testPeriod = 0;
        var currentLemonadePrice = lemonade.GetPrice();
        var price_increment_rate = lemonade.Get_price_increment_rate();
        var expectedLemonadePrice = Math.Round(currentLemonadePrice * (1 + price_increment_rate), 2);
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        marketToTest.RegisterMarketParticipant(company);
        company.GetInventory().AddGood(new InventoryEntry(lemonade, 2000, 3.0m, 0));

        var buyLemonadeOrder = new Order(null, company, lemonade, 1500, 3.0m);
        var buyLemonadeContext = new ActionContext { TradeToSubmit = buyLemonadeOrder, MarketToSubmitTo = marketToTest, Period = testPeriod };
        company.QueueOrder(buyLemonadeContext);

        // Act
        marketToTest.ProcessCompanyOrders();
        marketToTest.UpdatePrices();
        var actualLemonade = marketToTest.GetInventory().GetInventoryEntriesByGood(lemonade.GoodName).FirstOrDefault();
        var actualLemonadePrice = Math.Round(actualLemonade.good.GetPrice(), 2);
        // Assert
        Assert.AreEqual(expectedLemonadePrice, actualLemonadePrice);
    }
    #endregion
    #region CalculateFulfillmentRate tests
    [Test]
    public void CalculateFulfillmentRateReturns100WhenDemandIsMet()
    {
        // Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);
        
        var sellingCompany = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        var expectedFulfillmentRate = 100f;
        
        var period = 0;

        //give the selling company some lemons
        sellingCompany.GetInventory().AddGood(new InventoryEntry(lemonade, 1000, 3.0m, 0));

        //create the ActionContext
        var testContext = new ActionContext
        {
            TradeToSubmit = new Order(null, sellingCompany, lemonade, 1000, 3.0m),
            MarketToSubmitTo = TestMarket,
            Period = period
        };

        //have the market buy the lemons
        sellingCompany.QueueOrder(testContext);
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(testContext.Period);

        // Act
        var actualFulfillmentRate = TestMarket.GetPopulationDemand()[lemonade].FulfilmentRate;
        // Assert
        Assert.AreEqual(expectedFulfillmentRate, actualFulfillmentRate);

    }
    [Test]
    public void CalculateFulfillmentRatesReturnsLessThan1WhenDemandIsNotMet()
    {
        // Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);
        var sellingCompany = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(sellingCompany);
        //give the selling company some lemons
        sellingCompany.GetInventory().AddGood(new InventoryEntry(lemonade, 1000, 3.0m, 0));
        var period = 0;

        //queue up a lemon sell order
        var company1SellsLemonade = new Order(null, sellingCompany, lemonade, 500, 3.0m)
        {
            SubmittingCompany = sellingCompany,
        };
        var lemonBuyingContext = new ActionContext { TradeToSubmit = company1SellsLemonade, MarketToSubmitTo = TestMarket, Period = period };
        sellingCompany.QueueOrder(lemonBuyingContext);

        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(lemonBuyingContext.Period);
        //Act
        var actualFulfillmentRate = TestMarket.GetPopulationDemand()[lemonade].FulfilmentRate;
        // Assert
        Assert.Less(actualFulfillmentRate, 100f);
    }
    #endregion
    [Test]
    public void UnleashMarketForcesKeepsDemandStableWhenDemandIs60PercentFilled()
    {
        //Assert
        //Make a market that demands lemons
        TestMarket.InitializeDemandForSpecificGood(lemon, 1000, 0, 1000, .8f);

        var expectedLemonDemand = 1000;
        var sellingCompany = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(sellingCompany);
        var period = 0;

        //give the selling company some lemons
        sellingCompany.GetInventory().AddGood(new InventoryEntry(lemon, 900, 3.0m, 0));
        var marketBuysLemons = new Order(null, sellingCompany, lemon, 600, 3.0m);
        var lemonBuyingContext = new ActionContext { TradeToSubmit = marketBuysLemons, MarketToSubmitTo = TestMarket, Period = period };

        //Act
        //have the market buy some lemons
        sellingCompany.QueueOrder(lemonBuyingContext);
        TestMarket.ProcessCompanyOrders();
        TestMarket.UnleashMarketForces(lemonBuyingContext.Period);
        var actualLemonDemand = TestMarket.GetPopulationDemand()[lemon].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedLemonDemand, actualLemonDemand);
    }

    [Test]
    public void UnleashMarketForcesKeepsDemandStableWhenDemandIs100PercentFilled()
    {
        //Assert
        var period = 0;

        TestMarket.SetCash(1000000);
        var initialLemonadeDemand = 1000;
        TestMarket.InitializeDemandForSpecificGood(lemonade, initialLemonadeDemand);
        var expectedLemonadeDemand = initialLemonadeDemand;
        var sellingCompany = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(sellingCompany);

        //give the selling company some lemonade
        sellingCompany.GetInventory().AddGood(new InventoryEntry(lemonade, 1000, 3.0m, 0));
        //Act
        //have the market buy some lemonade
        var marketBuysLemonade = new Order(null, sellingCompany, lemonade, 1000, 3.0m);
        var testContext = new ActionContext
        {
            TradeToSubmit = marketBuysLemonade,
            MarketToSubmitTo = TestMarket,
            Period = period
        };
        sellingCompany.QueueOrder(testContext);
        TestMarket.ProcessCompanyOrders();
        TestMarket.UnleashMarketForces(testContext.Period);
        var actualLemonadeDemand = TestMarket.GetPopulationDemand()[lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedLemonadeDemand, actualLemonadeDemand);

        //cleanup
        TestMarket.RemoveMarketParticipant(sellingCompany);
    }

    [Test]
    public void UnleashMarketForcesDecreasesDemandWhenSupplyExceedsDemand()
    {
        //Assert
        var period = 0;

        TestMarket.SetCash(1000000);
        var initialLemonadeDemand = 900;
        TestMarket.InitializeDemandForSpecificGood(lemonade, initialLemonadeDemand);
        var sellingCompany = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(sellingCompany);

        //give the selling company some lemonade
        sellingCompany.GetInventory().AddGood(new InventoryEntry(lemonade, 1000, 3.0m, 0));
        //Act
        //have the market buy some lemonade
        var companySellsLemonade = new Order(null, sellingCompany, lemonade, 1000, 3.0m);
        var testContext = new ActionContext
        {
            TradeToSubmit = companySellsLemonade,
            MarketToSubmitTo = TestMarket,
            Period = period
        };
        sellingCompany.QueueOrder(testContext);
        TestMarket.ProcessCompanyOrders();
        TestMarket.UnleashMarketForces(testContext.Period);
        var actualLemonadeDemand = TestMarket.GetPopulationDemand()[lemonade].CurrentDemand;
        //Assert
        Assert.IsTrue(actualLemonadeDemand < initialLemonadeDemand);

        //cleanup
        TestMarket.RemoveMarketParticipant(sellingCompany);
    }
#region Reporting on Population trading activities

    [Test]
    public void GetTotalBoughtByMarketReturnsZeroWhenNoneOfSoldGoodsIsDemanded()
    {   // Populations don't want sugar, so we expect no population orders to be recorded.
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(sugar, 2000,3m,Period));

        var Company1SellsSugarToCompany2 = new Order(Company2, Company1, sugar, 500, 3.0m);
        var SugarOrder2 = new Order(Company2, Company1, sugar, 500, 3.0m);
        
        Company1.QueueOrder(CreateActionContext(Company1SellsSugarToCompany2, TestMarket, Period));
        Company2.QueueOrder(CreateActionContext(SugarOrder2, TestMarket, Period));
        
        const int expected = 0;
        const int tradingPeriod = 0;

        // Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actual = ((iDemandStrategy)TestDemandStrategy).GetTotalBoughtByPopulation(TestMarket, sugar, tradingPeriod);
        // Assert
        Assert.AreEqual(expected, actual);
    }
#endregion
}