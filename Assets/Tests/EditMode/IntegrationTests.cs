using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class IntegrationTests
{
    TheEconomy TestEconomy;
    Market TestMarket;
    EconAgent Company1;
    EconAgent Company2;

    float initialEnnui = 0.5f;
    int initialPopulation = 1000;
    iStrategy testStrategy;

    iFixedCostStrategy testFixedCostStrategy;

    PopulationAgent TestPopulation;

    Good Lemonade;
    const int Period = 0;
    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    readonly iMarketDataService TestMarketDataService = new MockMarketDataService();
    readonly iSupplyProvider TestSupplyProvider = new MockSupplyProvider();
    readonly iDemographicManager TestDemographicManager = new MockDemographicManager();

    readonly ITradeLogger TestTradeLogger = new TradeLoggerV1();

    [SetUp]
    public void SetUp()
    {
        TheEconomy.SetupForTests(TestTradeLogger);
        TestEconomy = TheEconomy.Instance;

        var existingMarket = TheEconomy.Instance.GetMarketByName("The First Market");
        TheEconomy.Instance.RemoveMarket(existingMarket);

        TestMarket = Market.Factory.CreateMarket("The First Market", AgentLevelEnum.Market)
            .WithDemandStrategy(TestDemandStrategy)
            .WithDataService(TestMarketDataService)
            .WithSupplyProvider(TestSupplyProvider)
            .WithDemographicManager(TestDemographicManager)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithPriceManager(new BasicPriceManager())
            .WithTransactionManager(new BasicTransactionManager());
        
        TheEconomy.Instance.RegisterCompany(TestMarket);

        testFixedCostStrategy = new BasicFixedCostStrategy();
        testStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
            .WithAggressionLevel(0.5m)
            .Build();

        Company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company2", AgentLevelEnum.Beginner);

        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);

        Lemonade = new GoodBuilder()
                  .Named("Lemonade")
                  .WithRarity(RarityEnum.Uncommon)
                  .Costing(5m)
                  .WhichIsProducedGood()
                  .Build();
      var reduceEnnuiEffect = new GoodEffect()
                .Named("Reduce Ennui")
                .DescribedAs("Reduces ennui")
                .Affecting(MetricEnum.Ennui)
                .WithEffectMagnitude(-.40f)
                .WithEffect(new MetricModifier<PopulationAgent>(
                            c => c.Ennui,
                            (c, newValue) => c.Ennui = newValue));
      Lemonade.AddEffect(reduceEnnuiEffect);
    }
#region Recording Trades
    [Test]
    public void MarketRecordsBothSidesOfTrade()
    {
        // Arrange
        var buyer = Company1;
        var seller = Company2;
        var good =Good.CreateInstance("Good", new PriceBand(1m, 2m), RarityEnum.Common);
        var sellersInventory = seller.GetInventory();
        sellersInventory.AddGood(new InventoryEntry(good, 10, 5, 0));
        
        var buyerBuysGoodFromSeller = new Order(buyer, seller, good, 10, 5);
        var sellerSellsGoodToBuyer = new Order(buyer, seller, good, 10, 5);
        var queueOrderResult = buyer.QueueOrder(CreateActionContext(buyerBuysGoodFromSeller, TestMarket,Period));
        var queueOrderResult2 = seller.QueueOrder(CreateActionContext(sellerSellsGoodToBuyer, TestMarket,Period));
        // Act
        TestMarket.ProcessCompanyOrders();

        // Assert
        var recordedTrades = TestMarket.GetExecutionsInPeriod(0);
        Assert.AreEqual(queueOrderResult.Result, LemonadeStandResultObject.Success(ResultTypeEnum.Success).Result);
        Assert.AreEqual(queueOrderResult2.Result, LemonadeStandResultObject.Success(ResultTypeEnum.Success).Result);
        Assert.AreEqual(2, recordedTrades.Count);
        Assert.AreEqual(buyer, recordedTrades[0].RecordedTrade.Buyer);
        Assert.AreEqual(seller, recordedTrades[0].RecordedTrade.Seller);
        Assert.AreEqual(good, recordedTrades[0].RecordedTrade.Good);
        Assert.AreEqual(10, recordedTrades[0].RecordedTrade.Quantity);
    }
    [Test]
    public void MarketDoesNotAllowQueueingDuplicateTrades()
    {
        // Arrange
        var market = TestMarket;
        var buyer = EconAgent.Factory.Create("Buyer", AgentLevelEnum.Beginner);
        var seller = EconAgent.Factory.Create("Seller", AgentLevelEnum.Beginner);
        market.RegisterMarketParticipant(buyer);
        market.RegisterMarketParticipant(seller);
        var good =Good.CreateInstance("Good", new PriceBand(1m, 2m), RarityEnum.Common);
        var sellersInventory = seller.GetInventory();
        sellersInventory.AddGood(new InventoryEntry(good, 10, 5, 0));

        var actionContext = new ActionContext
        {
            TradeToSubmit = new Order(buyer, seller, good, 10, 5),
            MarketToSubmitTo = market,
            Period = 0
        };
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.DuplicateOrder, "Order already exists in the queue");
        // Act
        var actual = buyer.QueueOrder(actionContext);
        actual=buyer.QueueOrder(actionContext);

        // Assert
        Assert.AreEqual(expected, actual);
    }

#endregion
#region Time
    [Test]
    public void EndTradingPeriodIncrementsPeriod()
    {
        // Arrange
        var currentPeriod = TestEconomy.tradingPeriod;
        var expected = currentPeriod + 1;
        // Act
        TestEconomy.EndTradingPeriod();
        var actual = TestEconomy.tradingPeriod;
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void EndTradingPeriodIncrementsMarketPeriod()
    {
        // Arrange
        var market = TestMarket;
        var currentPeriod = market.CurrentPeriod;
        var expected = currentPeriod + 1;
        // Act
        TestEconomy.EndTradingPeriod();
        var actual = market.CurrentPeriod;
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void EndTradingPeriodIncrementsCompanyPeriod()
    {
        // Arrange
        var company = Company1;
        var currentPeriod = company.CurrentPeriod;
        var expected = currentPeriod + 1;
        
        // Act
        TestEconomy.EndTradingPeriod();
        var actual = company.CurrentPeriod;
        // Assert
        Assert.AreEqual(expected, actual);
    }
        
#endregion
#region UnleashMarketForces
  #region LocalAgentsAct 
    [Test]
    public void UnleashMarketForcesCausesLocalAgentsToAct_CalledFromMarket()
    {
        // Arrange
        var company1 = Company1;
        company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 2000, 3m,0));

        var demandForLemonade = new DemandData()
        {
            MinDemand=1000,
            MaxDemand=10000
            };
            
        var listOfDemands = new Dictionary<Good,DemandData>()
            {
              {Lemonade,demandForLemonade}
            };

        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
             .Named("Test Population")
             .WithInitialCash(1000)
             .WithEnnui(initialEnnui)
             .WithPopulation(initialPopulation)
             .WithBehaviourStrategy(testStrategy)
             .WithFixedCostStrategy(testFixedCostStrategy)
             .Demanding(listOfDemands)
             .Build();
        
        testStrategy.GenerateGoals(TestPopulation);// Add to initialization?
        TestMarket.RegisterMarketParticipant(TestPopulation);

        var company1Order = new Order(null, company1, Lemonade, 1000, .25m);
        var company1Context = new ActionContext{TradeToSubmit = company1Order,
                                                MarketToSubmitTo = TestMarket};
        const int period = 1;
        const int expectedOrderFillQuantity = 1000;

        // Act
        company1.QueueOrder(company1Context);
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();//Integrate with UnleashMarketForces?
        TestMarket.UnleashMarketForces(period);
        var actualOrderFillQuantity = company1Order.FilledQuantity;

        // Assert
        Assert.AreEqual(expectedOrderFillQuantity, actualOrderFillQuantity, 
            $"Expected order fill quantity {expectedOrderFillQuantity}, but got {actualOrderFillQuantity}");
        
        //cleanup
        TestMarket.RemoveMarketParticipant(TestPopulation);
    }

     [Test]
    public void UnleashMarketForcesCausesLocalAgentsToAct_CalledFromEconomy()
    {
        // Arrange
        var company1 = Company1;
        company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 2000, 3m,0));

        var demandForLemonade = new DemandData()
        {
            MinDemand=1000,
            MaxDemand=10000
            };
            
        var listOfDemands = new Dictionary<Good,DemandData>()
            {
              {Lemonade,demandForLemonade}
            };

        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
             .Named("Test Population")
             .WithInitialCash(1000)
             .WithEnnui(initialEnnui)
             .WithPopulation(initialPopulation)
             .WithBehaviourStrategy(testStrategy)
             .WithFixedCostStrategy(testFixedCostStrategy)
             .Demanding(listOfDemands)
             .Build();
        
        testStrategy.GenerateGoals(TestPopulation);// Add to initialization?
        TestMarket.RegisterMarketParticipant(TestPopulation);

        var company1Order = new Order(null, company1, Lemonade, 1000, .25m);
        var company1Context = new ActionContext{TradeToSubmit = company1Order,
                                                MarketToSubmitTo = TestMarket};
        const int expectedOrderFillQuantity = 1000;

        // Act
        company1.QueueOrder(company1Context);
        TestEconomy.StartTradingPeriod();
        TestEconomy.EndTradingPeriod(); // This will call UnleashMarketForces internally.

        var actualOrderFillQuantity = company1Order.FilledQuantity;

        // Assert
        Assert.AreEqual(expectedOrderFillQuantity, actualOrderFillQuantity, 
            $"Expected order fill quantity {expectedOrderFillQuantity}, but got {actualOrderFillQuantity}");
        
        //cleanup
        TestMarket.RemoveMarketParticipant(TestPopulation);
    }
    
  #endregion
#endregion
#region Trades
    [Test]
    public void EndTradingPeriodProcessesNonMarketTradesQueuedInTheMarket()
    {
        // Arrange
        var market = TestMarket;
        market.SetCash(1000000);
        
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 2000, 3m,0));
        Company2.SetCash(1000000);

        var trade1 = new Order(null, Company1, Lemonade, 500, 2.0m);
        var trade2 = new Order(Company2, null, Lemonade, 1000, 3.5m);
        
        Company1.QueueOrder(new ActionContext { TradeToSubmit = trade1, MarketToSubmitTo = market });
        Company2.QueueOrder(new ActionContext { TradeToSubmit = trade2, MarketToSubmitTo = market });
        var expected = 2;
        
        // Act
        TestEconomy.EndTradingPeriod();
        int actual=TestEconomy.GetAllTransactions(0)
                    .Sum(x=> x.Value.Count());
        // Assert
        Assert.AreEqual(expected, actual);
    }

     [Test]
    public void EndTradingPeriodSendsTradesThatMarketHasQueuedWhenTwoMarketsArePresent()
    {
        // Arrange
        var TestDemographicManager2 = new MockDemographicManager();
        var TestDataHandler2 = new MockPopulationHistoryDataHandler();
        TestDemographicManager2.SetPopulationHistoryHandler(TestDataHandler2);

        var SecondMarket = Market.Factory.CreateMarket("Second Market", AgentLevelEnum.Market)
            .WithDemandStrategy(TestDemandStrategy)
            .WithDataService(TestMarketDataService)
            .WithSupplyProvider(TestSupplyProvider)
            .WithDemographicManager(TestDemographicManager2)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithPriceManager(new BasicPriceManager());

        TheEconomy.Instance.RegisterCompany(SecondMarket);
        TestMarket.SetCash(1000000);
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 2000, 3m,0));
        Company2.SetCash(1000000);

        var trade1 = new Order(null, Company1, Lemonade, 500, 2.0m);
        var trade2 = new Order(Company2, null, Lemonade, 1000, 3.5m);

        Company1.QueueOrder(new ActionContext { TradeToSubmit = trade1, MarketToSubmitTo = TestMarket });
        Company2.QueueOrder(new ActionContext { TradeToSubmit = trade2, MarketToSubmitTo = TestMarket });
        var expectedTradeCount = 2;
        var expectedMarketCount = 2;
        // Act
        TestEconomy.EndTradingPeriod();
        var actualTradeCount = TestEconomy.GetAllTransactions(0).Sum(x=> x.Value.Count());
        var actualMarketCount = TheEconomy.Instance.companies.Where(c=>c is Market).Count();

        // Assert
        Assert.AreEqual(expectedTradeCount, actualTradeCount,$"Expected {expectedTradeCount} trades, got {actualTradeCount}");
        Assert.AreEqual(expectedMarketCount, actualMarketCount,$"Expected {expectedMarketCount} markets, got {actualMarketCount}");

        // Clean up
        TheEconomy.Instance.RemoveMarket(SecondMarket);
        TestDataHandler2 = null;
        TestDemographicManager2 = null;
        SecondMarket = null;
    }

    [Test]
    public void CompaniesCannotQueueTradesToThemselves()
    {
        //Arrange
        var period = 0;
        var good = Good.CreateInstance("Good", new PriceBand(1m, 2m), RarityEnum.Common);
        var company = EconAgent.Factory.Create("Company", AgentLevelEnum.Beginner);
        var market = TestMarket;
        market.RegisterMarketParticipant(company);
        company.GetInventory().AddGood(new InventoryEntry(good, 10, 1, 0));

        var goodOrder = new Order(company, company, good, 10, 1);
        var actionContext = new ActionContext
        {
            TradeToSubmit = goodOrder,
            MarketToSubmitTo = market,
            Period = period
        };
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.SelfTrade, "Cannot trade with yourself");
        //Act
        var actual = company.QueueOrder(actionContext);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }
    [Test]
    public void IfTradeInvolvesMarketThenTheSubmittingCompanyIsTheMarket()
    {
        //Arrange
        var period = 0;
        var good = Good.CreateInstance("Good", new PriceBand(1m, 2m), RarityEnum.Common);
        var company = EconAgent.Factory.Create("Company", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(company);
        company.GetInventory().AddGood(new InventoryEntry(good, 10, 1, 0));
        var MarketBuysGoodFromCompany1 = new Order(TestMarket, company, good, 10, 1);
        var actionContext = new ActionContext
        {
            TradeToSubmit = MarketBuysGoodFromCompany1,
            MarketToSubmitTo = TestMarket,
            Period = period
        };
        var expected = TestMarket;
        //Act
        TestMarket.QueueMarketOrder(actionContext);
        var actual = MarketBuysGoodFromCompany1.SubmittingCompany;
        //Assert
        Assert.AreEqual(expected, actual);
    }
#endregion
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy.gameObject);
        TestMarket = null;
        Company1 = null;
        Company2 = null;
    }
}