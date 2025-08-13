using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;


[TestFixture]
public class RecordingDemographicHistoryTests
{
    private TheEconomy testEconomy;
    Market TestMarket;
    EconAgent Company1;

    readonly iFixedCostStrategy TestFixedCostStrategy = new BasicFixedCostStrategy();
    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    readonly iMarketDataService TestMarketDataService = new MockMarketDataService();
    iDemographicManager TestDemographicManager;
    iDataHandler<PopulationHistory> TestPopulationHistoryDataHandler;
    readonly iSupplyProvider TestSupplyProvider = new MockSupplyProvider();

    [SetUp]
    public void Setup()
    {
        TheEconomy.SetupForTests(new MockLogger());
        testEconomy = TheEconomy.Instance;
        var existingMarket = TheEconomy.Instance.GetMarketByName("The First Market");
        testEconomy.RemoveMarket(existingMarket);

        TestPopulationHistoryDataHandler= new MockPopulationHistoryDataHandler();
        TestDemographicManager = new BasicDemographicManager();
        TestDemographicManager.SetPopulationHistoryHandler(TestPopulationHistoryDataHandler);

        TestMarket= Market.Factory.CreateStarterMarket("Test Market", AgentLevelEnum.Market, TestDemandStrategy)
            .WithDataService(TestMarketDataService)
            .WithDemographicManager(TestDemographicManager)
            .WithSupplyProvider(TestSupplyProvider);
        
        TestSupplyProvider.Initialize(TestMarket);
        
        testEconomy.RegisterCompany(TestMarket);

        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner, null, TestFixedCostStrategy);
        TestMarket.RegisterMarketParticipant(Company1);
    }
    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(testEconomy);
        TestMarket = null;
        Company1 = null;
        TestDemographicManager = null;
        TestPopulationHistoryDataHandler = null;
    }

    [Test]
    public void PopulationHistoryRetrievedFromRepositoryWhenSetViaDataHandler()
    {
        //Arrange
        var expected = new List<PopulationHistory>(){
            new() {MarketId=TestMarket.MarketId,Period=0, Population=1000, Phase=TurnPhase.Beginning},
            new() {MarketId=TestMarket.MarketId,Period=0, Population=1000, Phase=TurnPhase.End},
            new() {MarketId=TestMarket.MarketId,Period=1, Population=1000, Phase=TurnPhase.Beginning},
            new() {MarketId=TestMarket.MarketId,Period=1, Population=1000, Phase=TurnPhase.End},
            new() {MarketId=TestMarket.MarketId,Period=2, Population=1000, Phase=TurnPhase.Beginning},
            new() {MarketId=TestMarket.MarketId,Period=2, Population=1000, Phase=TurnPhase.End},
        };
        var initialPopulationHistory = TestMarket.GetPopulationHistory();
        ((MockPopulationHistoryDataHandler)TestPopulationHistoryDataHandler)
            .SetPopulationHistory(expected);
        var emptySet = new List<PopulationHistory>();
        
        //Act
        var actual = TestMarket.GetPopulationHistory();
        //Assert
        Assert.That(initialPopulationHistory, Is.EqualTo(emptySet));
        Assert.AreEqual(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(expected[i].MarketId, actual[i].MarketId);
            Assert.AreEqual(expected[i].Period, actual[i].Period);
            Assert.AreEqual(expected[i].Population, actual[i].Population);
        }
    }

    [Test]
    public void StartTradingPeriodAddsRowToPopulationHistory()
    {
        //Arrange
        var testPopulation = EconAgentBuilder.For<PopulationAgent>()
            .WithPopulation(1000)
            .WithFixedCostStrategy(TestFixedCostStrategy)
            .Named("Test Population")
            .Build();
        TestMarket.RegisterMarketParticipant(testPopulation);
        var expectedPopulationHistory = new List<PopulationHistory>(){
            new() {MarketId=TestMarket.MarketId,Period=0, Population=1000},
        };
        //Act
        TestMarket.SetPopulation(1000, testPopulation);
        TestMarket.StartTradingPeriod();
        var actualPopulationHistory = TestMarket.GetPopulationHistory();
        //Assert
        Assert.AreEqual(expectedPopulationHistory.Count, actualPopulationHistory.Count);
        for (int i = 0; i < expectedPopulationHistory.Count; i++)
        {
            Assert.AreEqual(expectedPopulationHistory[i].MarketId, actualPopulationHistory[i].MarketId);
            Assert.AreEqual(expectedPopulationHistory[i].Period, actualPopulationHistory[i].Period);
            Assert.AreEqual(expectedPopulationHistory[i].Population, actualPopulationHistory[i].Population);
        }
    }
    [Test]
    public void UnleashMarketForcesCapturesChangeInPopulation()
    {
        //Arrange
        var populationFixedCostStrategy = new BasicFixedCostStrategy();
        var peopleInTheMarket = EconAgentBuilder.For<PopulationAgent>()
            .WithPopulation(1000)
            .WithFixedCostStrategy(populationFixedCostStrategy)
            .Named("People in the Market")
            .WithInitialCash(100)
            .Build();
        TestMarket.RegisterMarketParticipant(peopleInTheMarket);
        var MaraudersAttack = ScriptableObject.CreateInstance<MarketEventSO>();
        MaraudersAttack.Initialize( "Marauders Attack", 
                                    "Marauders attack the market",
                                    100f,
                                    1);
        var populationChangeEffect = new ChangePopulationEffect(-10f);
        MaraudersAttack.AddEffect(populationChangeEffect);

        TestMarket.AddPotentialMarketEvent(MaraudersAttack);

        const int expectedPopulation = 900;
        var expectedPopulationHistory = new List<PopulationHistory>(){
            new() {MarketId=TestMarket.MarketId,Period=0, Population=1000, Phase=TurnPhase.Beginning},
            new() {MarketId=TestMarket.MarketId,Period=0, Population=900, Phase=TurnPhase.End},
        };

        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(0);
        var actualPopulation = TestMarket.GetPopulation();
        var actualPopulationHistory = TestMarket.GetPopulationHistory();

        //Assert
        Assert.AreEqual(expectedPopulation, actualPopulation);
        Assert.AreEqual(expectedPopulationHistory.Count, actualPopulationHistory.Count);
        for (int i = 0; i < expectedPopulationHistory.Count; i++)
        {
            Assert.AreEqual(expectedPopulationHistory[i].MarketId, actualPopulationHistory[i].MarketId);
            Assert.AreEqual(expectedPopulationHistory[i].Period, actualPopulationHistory[i].Period);
            Assert.AreEqual(expectedPopulationHistory[i].Population, actualPopulationHistory[i].Population);
        }
    }

[Test]
public void MultiplePeriodsRecordDistinctSnapshots()
{
    // Arrange
    var testPopulation = EconAgentBuilder.For<PopulationAgent>()
        .WithPopulation(1000)
        .WithFixedCostStrategy(TestFixedCostStrategy)
        .Named("Test Population")
        .Build();
    TestMarket.RegisterMarketParticipant(testPopulation);

    // Act
    for (int i = 0; i < 3; i++)
    {
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(i);
    }

    // Assert
    var history = TestMarket.GetPopulationHistory();
    var beginCount = history.Count(h => h.Phase == TurnPhase.Beginning);
    var endCount = history.Count(h => h.Phase == TurnPhase.End);

    Assert.AreEqual(3, beginCount);
    Assert.AreEqual(3, endCount);
    Assert.That(history.Select(h => h.Period).Distinct().Count(), Is.EqualTo(3));
}

}