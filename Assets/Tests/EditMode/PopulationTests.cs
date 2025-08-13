using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class PopulationTests
{
    Good lemon;
    Good apple;
    Good water;
    Good sugar;
    PopulationAgent TestPopulation;

    readonly int initialPopulation = 1000;

    [SetUp]
    public void SetUp()
    {

        lemon = new GoodBuilder()
                .Named("Lemon")
                .WithRarity(RarityEnum.Common)
                .Build();
        apple = new GoodBuilder()
                .Named("Apple")
                .WithRarity(RarityEnum.Common)
                .Build();
        water = new GoodBuilder()
                .Named("Water")
                .WithRarity(RarityEnum.Common)
                .Build();
        sugar = new GoodBuilder()
                .Named("Sugar")
                .WithRarity(RarityEnum.Common)
                .Build();

        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
                    .Named("TestPopulation")
                    .WithPopulation(initialPopulation)
                    .AtLevel(AgentLevelEnum.Market)
                    .WithEnnui(.99f)
                    .Build();
    }

    [TearDown]
    public void TearDown()
    {
        lemon = null;
        apple = null;
        water = null;
        sugar = null;
        TheEconomy.Instance.ClearEconomy();
        TestPopulation = null;
    }

    [Test]
    public void PopulationCompany_Consume_RemovesItemsFromInventory()
    {
        // Arrange
        var inventory = new Inventory();
        const int initialLemonCount = 110;
        inventory.AddGood(new InventoryEntry(lemon, initialLemonCount, 1m, 0));

        var lemonDemandData = new DemandData
        {
            Ask = 1m,
            CurrentDemand = 100,
            FulfilmentRate = 1f,
            MinDemand = 0,
            MaxDemand = 1000,
            ConsumptionRate = .5f
        };

        var demandDictionary = new Dictionary<Good, DemandData>
        {
            { lemon, lemonDemandData }
        };

        var population = EconAgentBuilder.For<PopulationAgent>()
            .Named("Test Company")
            .AtLevel(AgentLevelEnum.Beginner)
            .WithPopulation(100)
            .WithInventory(inventory)
            .Demanding(demandDictionary)
            .WithEnnui(.99f)
            .Build();

        const int expectedLemonCount = 110 - 50; // 50 consumed by population of 100 with consumption rate of 0.5

        // Act
        population.Consume();
        var inventoryEntries = population.GetInventory().GetInventoryEntries();
        var lemonEntry = inventoryEntries.FirstOrDefault(entry => entry.good == lemon);
        var actualLemonCount = lemonEntry?.quantity ?? 0;

        // Assert
        Assert.AreEqual(expectedLemonCount, actualLemonCount);
    }

    [Test]
    public void PopulationCompany_ConsumeScalesWithPopulation()
    {
        // Arrange
        var inventory1 = new Inventory();
        const int initialLemonCount = 1000;
        inventory1.AddGood(new InventoryEntry(lemon, initialLemonCount, 1m, 0));

        var lemonDemandData = new DemandData
        {
            Ask = 1m,
            CurrentDemand = 100,
            FulfilmentRate = 1f,
            MinDemand = 0,
            MaxDemand = 1000,
            ConsumptionRate = .5f
        };

        var demandDictionary = new Dictionary<Good, DemandData>
        {
            { lemon, lemonDemandData }
        };

        var inventory2 = new Inventory();
        inventory2.AddGood(new InventoryEntry(lemon, initialLemonCount, 1m, 0));

        var population1 = EconAgentBuilder.For<PopulationAgent>()
            .Named("Test Company")
            .AtLevel(AgentLevelEnum.Beginner)
            .WithPopulation(100)
            .WithInventory(inventory1)
            .WithEnnui(.99f)
            .Demanding(demandDictionary)
            .Build();

        var population2 = EconAgentBuilder.For<PopulationAgent>()
            .Named("Test Company")
            .AtLevel(AgentLevelEnum.Beginner)
            .WithPopulation(200)
            .WithInventory(inventory2)
            .WithEnnui(.99f)
            .Demanding(demandDictionary)
            .Build();

        const int expectedPopulation1LemonCount = 950; // 100 population consumes 50 lemons, so 1000 - 50 = 950
        const int expectedPopulation2LemonCount = 900; // 200 population consumes 100 lemons, so 1000 - 100 = 900

        // Act
        population1.Consume();
        population2.Consume();

        var inventoryEntries1 = population1.GetInventory().GetInventoryEntries();
        var lemonEntryFor1 = inventoryEntries1.FirstOrDefault(entry => entry.good == lemon);
        var actualLemonCount1 = lemonEntryFor1?.quantity ?? 0;

        var inventoryEntries2 = population2.GetInventory().GetInventoryEntries();
        var lemonEntryFor2 = inventoryEntries2.FirstOrDefault(entry => entry.good == lemon);
        var actualLemonCount2 = lemonEntryFor2?.quantity ?? 0;

        // Assert
        Assert.AreEqual(expectedPopulation1LemonCount, actualLemonCount1, $"Expected {expectedPopulation1LemonCount} but got {actualLemonCount1} for population 1.");
        Assert.AreEqual(expectedPopulation2LemonCount, actualLemonCount2, $"Expected {expectedPopulation2LemonCount} but got {actualLemonCount2} for population 2.");
    }
    #region GetPerceivedCostOfGoods
    [Test]
    public void GetPerceivedCostOfGoodReturnsZeroIfNoRecipesPresentForProducedGood()
    {
        // Arrange
        var expected = 0m;
        var fruitPunch = new GoodBuilder()
                    .Named("Fruit Punch")
                    .WhichIsProducedGood()
                    .Build();
        var prices = new Dictionary<Good, decimal>()
                        {
                            { apple,2m },
                            { water,1m },
                            { sugar,1m },
                            { lemon,2m }
                        };
        // Act
        var actual = TestPopulation.GetPerceivedCostOfGood(fruitPunch, prices);
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void TestThatGoodEffectsAreAppliedByMarkets()
    {
        //Arrange
        var testMarket = Market.Factory.CreateStarterMarket("Test Market", AgentLevelEnum.Market, null)
                        .WithSupplyProvider(new MockSupplyProvider())
                        .WithDemographicManager(new MockDemographicManager())
                        ;
        testMarket.RegisterMarketParticipant(TestPopulation);
        var reduceEnnuiEffect = GoodEffectBuilder.Create()
                                .Named("Ennui Reducer")
                                .DescribedAs("Reduces Ennui by 10%")
                                .WithEffect(
                                    new MetricModifier<PopulationAgent>
                                    (
                                        x => x.Ennui,
                                        (x, newValue) => x.Ennui = newValue
                                    )
                                )
                                .WithEffectMagnitude(-.1f);
        var expectedEnnui = (float)Math.Round(TestPopulation.Ennui * .9f, 2);
        var Lemonade = new GoodBuilder()
                        .Named("Lemonade")
                        .WhichIsProducedGood()
                        .WithRarity(RarityEnum.Uncommon)
                        .Build();
        Lemonade.AddEffect(reduceEnnuiEffect);

        TestPopulation.GetInventory().AddGood(new InventoryEntry(Lemonade, 200, 2m, testMarket.CurrentPeriod));
        TestPopulation.InitializeDemandBasedOnPopulation(
            good: Lemonade,
            percentOfPopulation: 1f,
            consumptionRate: .2f
        );

        //Act
        testMarket.UnleashMarketForces(testMarket.CurrentPeriod);
        var actualEnnui = TestPopulation.Ennui;

        //Assert
        Assert.AreEqual(expectedEnnui, actualEnnui);

        //Teardown
        testMarket.RemoveMarketParticipant(TestPopulation);
        testMarket = null;
    }
    [Test]
    public void ConsumingFewerGoodsThanIdealHasReducedEffect()
    {
         //Arrange
        var testMarket = Market.Factory.CreateStarterMarket("Test Market", AgentLevelEnum.Market, null)
                        .WithSupplyProvider(new MockSupplyProvider())
                        .WithDemographicManager(new MockDemographicManager())
                        ;
        testMarket.RegisterMarketParticipant(TestPopulation);
        var reduceEnnuiEffect = GoodEffectBuilder.Create()
                                .Named("Ennui Reducer")
                                .DescribedAs("Reduces Ennui by 10%")
                                .WithEffect(
                                    new MetricModifier<PopulationAgent>
                                    (
                                        x => x.Ennui,
                                        (x, newValue) => x.Ennui = newValue
                                    )
                                )
                                .WithEffectMagnitude(-.1f);
        var expectedEnnui = (float)Math.Round(TestPopulation.Ennui * .9f, 2);
        var Lemonade = new GoodBuilder()
                        .Named("Lemonade")
                        .WhichIsProducedGood()
                        .WithRarity(RarityEnum.Uncommon)
                        .Build();
        Lemonade.AddEffect(reduceEnnuiEffect);

        TestPopulation.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 2m, testMarket.CurrentPeriod));
        TestPopulation.InitializeDemandBasedOnPopulation(
            good: Lemonade,
            percentOfPopulation: 1f,
            consumptionRate: .2f
        );

        //Act
        testMarket.UnleashMarketForces(testMarket.CurrentPeriod);
        var actualEnnui = TestPopulation.Ennui;

        //Assert
        Assert.IsTrue(expectedEnnui<actualEnnui);
        TestContext.WriteLine($"Full Effect{expectedEnnui}, Actual Effect {actualEnnui}");

        //Teardown
        testMarket.RemoveMarketParticipant(TestPopulation);
        testMarket = null;
    }
#endregion

}