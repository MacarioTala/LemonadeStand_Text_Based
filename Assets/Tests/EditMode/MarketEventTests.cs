using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;

[TestFixture]
public class MarketEventTests
{
    TheEconomy TestEconomy;
    Good Lemonade;
    Market TestMarket;
    EconAgent Company1;
    EconAgent Company2;

    MockMarketDataService TestMarketDataService;

    LinearDemandStrategy Strategy;

    iDemographicManager TestDemographicManager;

    iSupplyProvider TestSupplyProvider;

    int Period;

    MarketEventSO MaraudersAttack;
    MarketEventSO GodzillaAttack;

    [SetUp]
    public void Setup()
    {
        Period = 0;
        Lemonade = Good.CreateInstance("Lemonade", new PriceBand(.5m, 2m), RarityEnum.Uncommon);
        Lemonade.IsProducedGood = true;

        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        Strategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        TestSupplyProvider= new BasicSupplyProvider();
        TestMarketDataService = new MockMarketDataService();
        TestDemographicManager = new BasicDemographicManager();
        TestDemographicManager.SetPopulationHistoryHandler(new MockPopulationHistoryDataHandler());

        TestMarket = Market.Factory.CreateStarterMarket("Starter Market",
                                                        AgentLevelEnum.Market,
                                                        Strategy)
                                    .WithDataService(TestMarketDataService)
                                    .WithDemographicManager(TestDemographicManager)
                                    .WithSupplyProvider(TestSupplyProvider)
                                    ;
        TestSupplyProvider.Initialize(TestMarket);

        Company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company2", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);

        MaraudersAttack = ScriptableObject.CreateInstance<MarketEventSO>();
        GodzillaAttack = ScriptableObject.CreateInstance<MarketEventSO>();

        MaraudersAttack.Initialize( eventName: "Marauders Attack",
                                    eventDescription: "Marauders attack the neighbourhood, reducing population.",
                                    eventChance: 100f,
                                    eventDuration: 2);
        MaraudersAttack.AddEffect(new ChangePopulationEffect(-10));

        GodzillaAttack.Initialize( eventName: "Godzilla Attack",
                                    eventDescription: "Godzilla attacks the neighbourhood, reducing population.",
                                    eventChance: 100f,
                                    eventDuration: 2);
        GodzillaAttack.AddEffect(new ChangePopulationEffect(-30));
        GodzillaAttack.AddTag("SuperDisaster");
        GodzillaAttack.AddTag("Monster");
        GodzillaAttack.AddEffect(new ModifyMonsterEffect(1, 2f));
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(TestEconomy.gameObject);
        UnityEngine.Object.DestroyImmediate(Strategy);
        TestMarket = null;
        Company1 = null;
        Company2 = null;
        TestMarketDataService = null;
        TestDemographicManager = null;
        TestSupplyProvider = null;
        MaraudersAttack = null;
        GodzillaAttack = null;
    }

    [Test]
    public void MaraudersAttackNeighbourhoodReducesPopulation()
    {
        //Arrange
        const int initialPopulation = 100;
        var expectedPopulation = 90;
        var testPopulation = EconAgentBuilder.For<PopulationAgent>()
            .WithPopulation(initialPopulation)
            .WithFixedCostStrategy(new BasicFixedCostStrategy())
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .Build();

        TestMarket.RegisterMarketParticipant(testPopulation);

        //Act
        MaraudersAttack.Invoke(TestMarket);
        var newPopulation = TestMarket.GetPopulation();
        
        //Assert
        Assert.AreEqual(expectedPopulation, newPopulation);
    }
#region MarketEventDuration tests
    [Test]
    public void IfEventStillActiveReapplyEffect()
    {
        //Arrange
        const int initialPopulation = 100;
        var testPopulation = EconAgentBuilder.For<PopulationAgent>()
            .WithPopulation(initialPopulation)
            .WithFixedCostStrategy(new BasicFixedCostStrategy())
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .Build();
        TestMarket.RegisterMarketParticipant(testPopulation);
        
        var expectedPopulation = 81;
        TestMarket.AddPotentialMarketEvent(MaraudersAttack);

        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        TestMarket.StartTradingPeriod();
        var actualPopulation = TestMarket.GetPopulation();

        //Assert
        Assert.AreEqual(expectedPopulation, actualPopulation);
    }
    [Test]
    public void MarketEventCanBeAddedToActiveEventWithoutMarketEffect()
    {
        //Arrange
        var initialPopulation = 100;
        var testPopulation = EconAgentBuilder.For<PopulationAgent>()
            .WithPopulation(initialPopulation)
            .WithFixedCostStrategy(new BasicFixedCostStrategy())
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .Build();
        TestMarket.RegisterMarketParticipant(testPopulation);

        var AppleTreesGrow = ScriptableObject.CreateInstance<MarketEventSO>();
        AppleTreesGrow.Initialize( eventName: "Apple Trees Grow",
                                    eventDescription: "Apple trees grow in the neighbourhood, increasing population.",
                                    eventChance: 100f,
                                    eventDuration: 1);
        
        var expectedPopulation = initialPopulation;
        TestMarket.AddPotentialMarketEvent(AppleTreesGrow);

        //Act
        TestMarket.StartTradingPeriod();

        //Assert
        Assert.AreEqual(expectedPopulation, TestMarket.GetPopulation());

    }
    [Test]
    public void MarketEventExpiresAfterDuration()
    {
        //Arrange
        const int initialPopulation = 100;
        var testPopulation = EconAgentBuilder.For<PopulationAgent>()
            .WithPopulation(initialPopulation)
            .WithFixedCostStrategy(new BasicFixedCostStrategy())
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .Build();
        TestMarket.RegisterMarketParticipant(testPopulation);
        var expectedPopulation = 81;
        TestMarket.AddPotentialMarketEvent(MaraudersAttack);

        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        TestMarket.StartTradingPeriod(); //Period should be 2 here. No additional population reduction.
        var actualPopulation = TestMarket.GetPopulation();

        //Assert
        Assert.AreEqual(2,TestMarket.CurrentPeriod);
        Assert.AreEqual(expectedPopulation, actualPopulation);
    }
    [Test]
    public void CannotAddADuplicateOfACurrentlyActiveEvent()
    {
        //Arrange
        var initialNumberOfActiveEvents = TestMarket.GetActiveMarketEvents().Count;
        var expectedNumberOfActiveEvents = initialNumberOfActiveEvents+1;
        TestMarket.AddPotentialMarketEvent(MaraudersAttack);

        //Act
        TestMarket.RollForEvents();
        TestMarket.RollForEvents(); //try to add it twice
        var actualNumberOfActiveEvents = TestMarket.GetActiveMarketEvents().Count;

        //Assert
        Assert.AreEqual(expectedNumberOfActiveEvents, actualNumberOfActiveEvents);  
    }
    [Test]
    public void CanAddADifferentEventEvenWithActiveEvents()
    {
        //Arrange
        var expectedNumberOfActiveEvents = 2;

        //Act
        TestMarket.AddPotentialMarketEvent(MaraudersAttack);
        TestMarket.RollForEvents();
        TestMarket.AddPotentialMarketEvent(GodzillaAttack);
        TestMarket.RollForEvents(); 
        var actualNumberOfActiveEvents = TestMarket.GetActiveMarketEvents().Count;
        //Assert
        Assert.AreEqual(expectedNumberOfActiveEvents, actualNumberOfActiveEvents);
    }
#endregion
#region Integration tests with Market
    [Test]
    public void MaraudersAttackNeighbourhoodReducesPopulationFromMarket()
    {
        //Arrange
        const int initialPopulation = 100;
        var testPopulation = EconAgentBuilder.For<PopulationAgent>()
            .WithPopulation(initialPopulation)
            .WithFixedCostStrategy(new BasicFixedCostStrategy())
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .Build();
        TestMarket.RegisterMarketParticipant(testPopulation);
        var expectedPopulation = 90;

        TestMarket.AddPotentialMarketEvent(MaraudersAttack);
        
        //Act
        TestMarket.StartTradingPeriod();
        var actual = TestMarket.GetPopulation();
        //Assert
        Assert.AreEqual(expectedPopulation, actual);

    }
    [Test]
    public void MarketEventsAreRecordedInMarketEventHistory()
    {
        //Arrange
        TestMarket.AddPotentialMarketEvent(MaraudersAttack);
        var duration = TestMarket.CurrentPeriod + MaraudersAttack.GetDuration();
        var expectedEventEnd = duration;
        var expected = new List<(iMarketEvent Event, int PeriodStart,int periodEnd)>{
            (MaraudersAttack,TestMarket.CurrentPeriod,expectedEventEnd)
        };

        //Act
        for(var i = 0; i <= duration; i++)
        {
            TestMarket.StartTradingPeriod();
            TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        }
        var actual = TestMarket.GetMarketEventHistory();
        //Assert
        Assert.AreEqual(expected.Count, actual.Count,"counts are different");
        Assert.AreEqual(expected[0].Event, actual[0].Event,"events are different");
        Assert.AreEqual(expected[0].PeriodStart, actual[0].PeriodStart,"period start is different");
        Assert.AreEqual(expected[0].periodEnd, actual[0].periodEnd,"period end is different");
    }
    [Test]
    public void IncompatibleEffectsCannotOccurTogether()
    {
        //Arrange
        var ElNino = ScriptableObject.CreateInstance<MarketEventSO>();
        ElNino.Initialize(  eventName: "El Nino",
                            eventDescription: "El Nino causes a drought, increasing water prices.",
                            eventChance: 100f,
                            eventDuration: 2);
        ElNino.AddTag("WeatherDry");
        var LaNina = ScriptableObject.CreateInstance<MarketEventSO>();
        LaNina.Initialize(  eventName: "La Nina",
                            eventDescription: "La Nina causes a flood, dampening demand for Lemonade.",
                            eventChance: 100f,
                            eventDuration: 2);
        LaNina.AddTag("WeatherWet");
        var expectedEvent = ElNino;
        TestMarket.AddPotentialMarketEvent(ElNino);
        
        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        TestMarket.AddPotentialMarketEvent(LaNina);
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        var actual = TestMarket.GetActiveMarketEvents();
        var actualEvent = actual.FirstOrDefault().Event;

        //Assert
        Assert.AreEqual(1, actual.Count);
        Assert.AreEqual(expectedEvent, actualEvent);
    }
    [Test]
    public void MarketEventCanModifyAnotherEvent()
    {
        //Arrange
        var MothraAttacks = ScriptableObject.CreateInstance<MarketEventSO>();
        MothraAttacks.Initialize( eventName: "Mothra Attacks",
                                    eventDescription: "Mothra attacks the neighbourhood, reducing population.",
                                    eventChance: 100f,
                                    eventDuration: 3);
        MothraAttacks.AddTag("SuperDisaster");    
        MothraAttacks.AddTag("Monster");
        MothraAttacks.AddEffect(new ChangePopulationEffect(-20));
        MothraAttacks.AddEffect(new ModifyMonsterEffect(1, 2f));
        var expectedGodzillaAttackDuration = 1;
        var expectedMothraAttackDuration = 1;
        
        TestMarket.AddPotentialMarketEvent(GodzillaAttack);
        TestMarket.AddPotentialMarketEvent(MothraAttacks);

        //Act
        var duration = Math.Max(GodzillaAttack.GetDuration(), MothraAttacks.GetDuration());
        for(var i = 0; i < duration; i++)//deliberately run for less than the duration of the event.
                                        // to prevent effect from resetting
        {
            TestMarket.StartTradingPeriod();
            TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        }
        var actualGodzillaAttackDuration = GodzillaAttack.GetDuration();
        var actualMothraAttackDuration = MothraAttacks.GetDuration();
        
        //Assert
        Assert.AreEqual(expectedGodzillaAttackDuration, actualGodzillaAttackDuration,"Godzilla attack duration is different");
        Assert.AreEqual(expectedMothraAttackDuration, actualMothraAttackDuration,"Mothra attack duration is different");
    }
    
    [Test]
    public void EnsureThatEffectsWithModifyMonsterEffectAreAppliedOnlyIfThereIsMoreThanOneMonsterPresent()
    {
        //Arrange
        const int expectedGodzillaAttackDuration = 2;
        TestMarket.AddPotentialMarketEvent(GodzillaAttack);
        
        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        var actualGodzillaAttackDuration = GodzillaAttack.GetDuration();

        //Assert
        Assert.AreEqual(expectedGodzillaAttackDuration, actualGodzillaAttackDuration,"Godzilla attack duration is different");
    }

    [Test]
    public void EffectsThatHaveBeenModifiedRevertToOriginalEffectsWhenEffectEnds()
    {
        //Arrange
        var MothraAttacks = ScriptableObject.CreateInstance<MarketEventSO>();
        MothraAttacks.Initialize( eventName: "Mothra Attacks",
                                    eventDescription: "Mothra attacks the neighbourhood, reducing population.",
                                    eventChance: 100f,
                                    eventDuration: 3);
        MothraAttacks.AddTag("SuperDisaster");    
        MothraAttacks.AddTag("Monster");
        MothraAttacks.AddEffect(new ChangePopulationEffect(-20));
        MothraAttacks.AddEffect(new ModifyMonsterEffect(1, 2f));
        var expectedGodzillaAttackDuration = GodzillaAttack.GetDuration();

        //Act
        TestMarket.AddPotentialMarketEvent(GodzillaAttack);
        TestMarket.AddPotentialMarketEvent(MothraAttacks);
        for(var i = 0; i < expectedGodzillaAttackDuration; i++)
        {
            TestMarket.StartTradingPeriod();
            TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
        }
        var actualGodzillaAttackDuration = GodzillaAttack.GetDuration();

        //Assert
        Assert.AreEqual(expectedGodzillaAttackDuration, actualGodzillaAttackDuration,"Godzilla attack duration is different");
    }
#endregion

}