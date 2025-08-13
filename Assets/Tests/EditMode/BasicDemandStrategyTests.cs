using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class BasicDemandStrategyTests
{
    TheEconomy TestEconomy;
    Market TestMarket;
    PopulationAgent TestPopulation;
    EconAgent Company1;
    EconAgent Company2;
    const int Period = 0;

    iDemandStrategy TestDemandStrategy;
    iStrategy TestReduceEnnuiStrategy;

    Good lemon;
    Good water;
    Good sugar;
    Good lemonade;
    [SetUp]
    public void Setup ()
    {
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;
        TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        TestReduceEnnuiStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
            .WithAggressionLevel(.55m)
            .Build();

        lemonade = new GoodBuilder()
            .Named("Lemonade")
            .WithRarity(RarityEnum.Uncommon)
            .Costing(.5m)
            .WhichIsProducedGood()
            .Build();

        var reduceEnnuiEffect = new GoodEffect()
            .Named("Reduce Ennui")
            .DescribedAs("Lemonade reduces ennui")
            .Affecting(MetricEnum.Ennui)
            .WithEffectMagnitude(-.1f)
            .WithEffect(new MetricModifier<PopulationAgent>(
                           c => c.Ennui,
                           (c, newValue) => c.Ennui = newValue));
        lemonade.AddEffect(reduceEnnuiEffect);

        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
            .WithDemandStrategy(TestDemandStrategy)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithTransactionManager(new BasicTransactionManager())
            .WithDemographicManager(new MockDemographicManager());
        
        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company 2", AgentLevelEnum.Beginner);

        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Market)
            .WithPopulation(1000)
            .WithBehaviourStrategy(TestReduceEnnuiStrategy)
            .WithInitialCash(10000)
            .WithEnnui(0.5f)
            .Build();

        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);
        TestMarket.RegisterMarketParticipant(TestPopulation);

        TestReduceEnnuiStrategy.GenerateGoals(TestPopulation);

        lemon = Good.CreateInstance("Lemon", new PriceBand(.5m, 1.0m), RarityEnum.Common);
        water = Good.CreateInstance("Water", new PriceBand(.5m, 1.0m), RarityEnum.Common);
        sugar = Good.CreateInstance("Sugar", new PriceBand(.5m, 1.0m), RarityEnum.Common);
    }
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy.gameObject);
        Company1 = null;
        Company2 = null;
        TestMarket = null;
        TestEconomy = null;
    }

    [Test]
    public void GetTotalBoughtByPopulationReturnsOnlyMarketBuys()
    {
        // Arrange
        var lemonadeDemand = new DemandData { MinDemand = 1000, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, lemonadeDemand);
        
        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 2000,3m,Period));

        var Company1SellsLemonsToAnyone = new Order(null, Company1, lemonade, 1000, 3.0m);
        var Company2BuysLemonsFromAnyone = new Order(Company2,null, lemonade, 500, 3.0m);

        Company1.QueueOrder(CreateActionContext(Company1SellsLemonsToAnyone, TestMarket, Period));
        Company2.QueueOrder(CreateActionContext(Company2BuysLemonsFromAnyone, TestMarket, Period));

        const int expected = 1000;
        // Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();

        var actual = TestDemandStrategy.GetTotalBoughtByPopulation(TestMarket, lemonade, Period);
        // Assert
        Assert.AreEqual(expected, actual);
    }
#region default implementation tests

    [Test]
    public void CalculateDemandForPeriodReturnsBaseMarketDemandWhenNoOrdersExist()
    {
        // Arrange
        var lemonadeDemand = new DemandData { MinDemand = 1000, MaxDemand = 1000 };
        TestPopulation.SetDemand(lemonade, lemonadeDemand);
        var period = 0;
        var strategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();

        var expected = 1000;
        // Act
        TestMarket.StartTradingPeriod();//this queues up population demand
        TestMarket.ProcessCompanyOrders();
        var actual = ((iDemandStrategy)strategy).GetDemandInPeriod(TestMarket, period)[lemonade].CurrentDemand;
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void CalculateDemandForPeriodReturnsBaseMarketDemandPlusOrdersWhenOrdersExist()
    {
        // Arrange
        var maxDemand = 1000;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = maxDemand };
        TestPopulation.SetDemand(lemonade, demandForLemonade);
        var strategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        
        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 2000,3m,Period));

        var Company1BuysLemonadeFromAnyone = new Order(Company1, null, lemonade, 500, 3.0m);
        var lemonadeContext = new ActionContext { TradeToSubmit = Company1BuysLemonadeFromAnyone, MarketToSubmitTo = TestMarket , Period = Period};
        Company1.QueueOrder(lemonadeContext);
        var expected = 1500;

        // Act
        TestMarket.StartTradingPeriod();//this queues up population demand
        TestMarket.ProcessCompanyOrders();
        var actual = ((iDemandStrategy)strategy).GetDemandInPeriod(TestMarket, Period)[lemonade].CurrentDemand;
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void CalculateDemandForPeriodCountsOnlyOrdersIfNoMarketDemandExists()
    {
        // Arrange
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = 0 };
        TestPopulation.SetDemand(lemonade, demandForLemonade);
        var strategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        Company1.GetInventory().AddGood(new InventoryEntry(lemonade, 2000,3m,Period));

        var lemonOrder = new Order(Company1, null, lemonade, 500, 3.0m);
        var lemonContext = new ActionContext { TradeToSubmit = lemonOrder, MarketToSubmitTo = TestMarket , Period = Period};
        Company1.QueueOrder(lemonContext);

        var expected = 500;
        // Act
        TestMarket.StartTradingPeriod();//this queues up population demand
        var actual = ((iDemandStrategy)strategy).GetDemandInPeriod(TestMarket, Period)[lemonade].CurrentDemand;
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void CalculateSupplyForPeriodReturnsTotalQuantityOfAllFilledTrades()
    {
        //Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(lemon, 2000,3m,Period));
        Company2.GetInventory().AddGood(new InventoryEntry(water, 2000,3m,Period));

        var Company2BuysLemonsFromAnyone = new Order(Company2, null, lemon, 500, 3.0m);
        var Company1SellsLemonsToAnyone = new Order(null, Company1, lemon, 500, 3.0m);
        var Company2lemonContext = new ActionContext { TradeToSubmit = Company2BuysLemonsFromAnyone, MarketToSubmitTo = TestMarket , Period = Period};
        var Company1LemonContext = new ActionContext { TradeToSubmit = Company1SellsLemonsToAnyone, MarketToSubmitTo = TestMarket , Period = Period};
        Company2.QueueOrder(Company2lemonContext);
        Company1.QueueOrder(Company1LemonContext);

        var Company1BuysWaterFromAnyone = new Order(Company1, null, water, 500, 3.0m);
        var Company2SellsWaterToAnyone = new Order(null, Company2, water, 500, 3.0m);
        var Company1waterContext = new ActionContext { TradeToSubmit = Company1BuysWaterFromAnyone, MarketToSubmitTo = TestMarket , Period = Period};
        var Company2WaterContext = new ActionContext { TradeToSubmit = Company2SellsWaterToAnyone, MarketToSubmitTo = TestMarket , Period = Period};
        Company1.QueueOrder(Company1waterContext);
        Company2.QueueOrder(Company2WaterContext);

        var expected = new Dictionary<Good, int> {{lemon, 500}, {water, 500}};
        //Act
        TestMarket.ProcessCompanyOrders();
        var actual = ((iDemandStrategy)TestDemandStrategy).CalculateSupplyForPeriod(TestMarket, Period);
        //Assert
        Assert.AreEqual(expected, actual);
    }
#endregion


}