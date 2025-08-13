using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class RefactorOutFulfillDemand
{
    TheEconomy TestEconomy;
    Market TestMarket;
    PopulationAgent TestPopulation;
    EconAgent TestCompany1;
    Good Lemon;
    Good Lemonade;
    iStrategy TestReduceEnnuiStrategy;
    iDemandStrategy TestDemandStrategy;
    readonly ITradeLogger TestTradeLogger = new TradeLoggerV1();

    int Period = 0;

    [SetUp]
    public void SetUp()
    {
        TheEconomy.SetupForTests(TestTradeLogger);
        TestEconomy = TheEconomy.Instance;
        var defaultMarket = TestEconomy.GetMarketByName("The First Market");
        TestEconomy.RemoveMarket(defaultMarket);

        Lemon = new GoodBuilder()
                .Named("Lemon")
                .Costing(.10m)
                .WithRarity(RarityEnum.Common)
                .Build();
        
        Lemonade = new GoodBuilder()
                .Named("Lemonade")
                .Costing(1m)
                .WithRarity(RarityEnum.Uncommon)
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

        var LemonDemand = new DemandData
            {
            MinDemand=0,
            MaxDemand=100,
            ConsumptionRate=0.5f,
            };

        var LemonadeDemand = new DemandData
            {
            MinDemand=0,
            MaxDemand=100,
            ConsumptionRate=0.5f,
            };
        
        var Demands = new Dictionary<Good, DemandData>
            {
                { Lemon, LemonDemand },
                { Lemonade, LemonadeDemand }
            };
        
        TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        TestReduceEnnuiStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                .WithAggressionLevel(.5m)
                .Build();
        
        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
                .Named("Test Population")
                .AtLevel(AgentLevelEnum.Beginner)
                .WithInitialCash(1000m)
                .Demanding(Demands)
                .WithPopulation(100)
                .WithEnnui(.90f)
                .WithBehaviourStrategy(TestReduceEnnuiStrategy)
                .WithFixedCostStrategy(new BasicFixedCostStrategy())
                .Build();

        TestCompany1 = EconAgentBuilder.For<EconAgent>()
                .Named("Test Company 1")
                .AtLevel(AgentLevelEnum.Beginner)
                .WithInitialCash(1000m)
                .WithFixedCostStrategy(new BasicFixedCostStrategy())
                .Build();

        TestReduceEnnuiStrategy.GenerateGoals(TestPopulation);

        TestMarket = Market.Factory
                .CreateMarket("Test Market", AgentLevelEnum.Market)
                .WithTradeProcessor(new BasicTradeProcessor())
                .WithDemandStrategy(TestDemandStrategy)
                .WithDemographicManager(new MockDemographicManager())
                .WithMarketDataManager(new BasicMarketDataManager())
                .WithPriceManager(new BasicPriceManager())
                .WithTradeProcessor(new BasicTradeProcessor())
                .WithTransactionManager(new BasicTransactionManager())
                .WithPriceModifier(new SupplyDemandModifier())
                .WithSupplyProvider(new MockSupplyProvider());
    }
    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(TestEconomy.gameObject);
        TestMarket = null;
        TestCompany1 = null;
        TestPopulation = null;
        TestReduceEnnuiStrategy = null;
        TestDemandStrategy = null;
        Lemon = null;
        Lemonade = null;
        Period = 0;
    }

    [Test]
    public void TestThatMarketsNoLongerNeedConsumptionManagerToFulfillDemand()
    {
        // Arrange
        TestMarket.RegisterMarketParticipant(TestPopulation);
        TestMarket.RegisterMarketParticipant(TestCompany1);

        const int lemonadeQuantity = 100;
        const decimal lemonadePrice = 1m;
        TestCompany1.GetInventory().AddGood(new InventoryEntry(Lemonade, lemonadeQuantity,lemonadePrice,Period));
        
        var lemonadeSale = new Order(null, TestCompany1, Lemonade, 100, .15m);
        TestCompany1.QueueOrder(CreateActionContext(lemonadeSale,TestMarket,Period));
        Exception actualException = null;

        // Act
        TestMarket.StartTradingPeriod();
        try
            {
                TestMarket.ProcessCompanyOrders();
                TestMarket.UnleashMarketForces(Period);
            }
        catch (Exception e)
            {
                actualException = e;
            }

        // Assert
        Assert.That(actualException, Is.Null, "No exception should be thrown when fulfilling demand without a ConsumptionManager.");  
        
    }
    [Test]
    public void TestThatTheEconomyCanHandleMarketsWithoutConsumptionManager()
    {
        // Arrange
        TestMarket.RegisterMarketParticipant(TestPopulation);
        TestMarket.RegisterMarketParticipant(TestCompany1);
        TestEconomy.RegisterCompany(TestMarket);

        const int lemonadeQuantity = 100;
        const decimal lemonadePrice = 1m;
        TestCompany1.GetInventory().AddGood(new InventoryEntry(Lemonade, lemonadeQuantity,lemonadePrice,Period));
        
        var lemonadeSale = new Order(null, TestCompany1, Lemonade, 100, .15m);
        TestCompany1.QueueOrder(CreateActionContext(lemonadeSale,TestMarket,Period));

        var company1Order = new Order(null, TestCompany1, Lemonade, 1000, .25m);
        var company1Context = new ActionContext{TradeToSubmit = company1Order,
                                                MarketToSubmitTo = TestMarket};
        Exception actualException = null;
      
        // Act
        TestCompany1.QueueOrder(company1Context);
        try
            {
                TestEconomy.StartTradingPeriod();
                TestEconomy.EndTradingPeriod(); // This will call UnleashMarketForces internally.
            }
        catch (Exception e)
            {
                actualException = e;
            }
        
        // Assert
        Assert.That(actualException, Is.Null, "No exception should be thrown when fulfilling demand without a ConsumptionManager in the economy.");
        
    }
}