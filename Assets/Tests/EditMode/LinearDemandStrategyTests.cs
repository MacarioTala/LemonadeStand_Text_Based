using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public partial class LinearDemandStrategyTests
{
    TheEconomy TestEconomy;
    Good Lemonade;
    Market TestMarket;
    EconAgent Company1;
    EconAgent Company2;
    PopulationAgent TestPopulation;
    iStrategy TestReduceEnnuiStrategy;

    MockMarketDataService TestMarketDataService;

    LinearDemandStrategy TestDemandStrategy;

    iDemographicManager TestDemographicManager;

    iSupplyProvider TestSupplyProvider= new BasicSupplyProvider();

    int Period = 0;

    [SetUp]
    public void SetUp()
    {
        // Set up the economy
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        //Set up goods
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
        //Set up market and population dependencies
        TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        TestMarketDataService = new MockMarketDataService();
        TestDemographicManager = new MockDemographicManager();
        TestReduceEnnuiStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                                        .WithAggressionLevel(.55m)
                                        .Build();
        //Set up population
        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
                                        .Named("TestPopulation")
                                        .AtLevel(AgentLevelEnum.Market)
                                        .WithInitialCash(10000)
                                        .WithBehaviourStrategy(TestReduceEnnuiStrategy)
                                        .WithEnnui(.99f)
                                        .WithPopulation(1000)
                                        .WithFixedCostStrategy(new BasicFixedCostStrategy())
                                        .Build();
        TestReduceEnnuiStrategy.GenerateGoals(TestPopulation);

        TestMarket = Market.Factory.CreateStarterMarket("Starter Market",
                                                        AgentLevelEnum.Market,
                                                        TestDemandStrategy);
        TestMarketDataService = new MockMarketDataService();
        TestDemographicManager = new MockDemographicManager();
        TestMarket.SetDemographicManager(TestDemographicManager);
        TestMarket.SetMarketDataService(TestMarketDataService);
        TestMarket.SetSupplyProvider(TestSupplyProvider);
        TestSupplyProvider.Initialize(TestMarket);

        Company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company2", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);
        TestMarket.RegisterMarketParticipant(TestPopulation);
    }
#region Adjust Demand Based On Elasticity
    [TestCase(TestName="If the good does not have the passed elasticity, demand should not change")]
    public void ADBE_IfElasticityDNEDoNotAdjustDemand()
    {
        //Arrange
        var expectedDemand = 500;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = expectedDemand };
        TestPopulation.SetDemand(Lemonade, demandForLemonade);
        
        var dummyMetric = 1;
        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(Period);
        TestDemandStrategy.AdjustDemandBasedOnElasticityAndHistory(TestMarket, Lemonade, ElasticityTypeEnum.PopulationElasticity,dummyMetric);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }

    [TestCase(TestName="If the saturation elasticity is 1, the change in metric is 100%, and increase is passed, demand should double")]
    public void ADBE_IfSaturationElasticityIsOneDemandShouldIncrease()
    {
        //Arrange
        var maxDemand = 1000;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = maxDemand };
        TestPopulation.SetDemand(Lemonade, demandForLemonade);

        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, 1);
        var dummyMetric = 1;

        var expectedDemand = 1000;
        var expectedResult = LemonadeStandResultObject.Success();
        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(Period);
        var actualResult = TestDemandStrategy.AdjustDemandBasedOnElasticityAndHistory(TestMarket, Lemonade, ElasticityTypeEnum.SaturationElasticity, dummyMetric);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
        Assert.AreEqual(expectedResult.Result, actualResult.Result);
    }
    [TestCase(TestName="If the saturation elasticity is .5, and increase is passed, demand should increase by 50%")]
    public void ADBE_IfSaturationElasticityIsPointFiveDemandShouldIncrease()
    {
        //Arrange
        var maxDemand = 750;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = maxDemand };
        TestPopulation.SetDemand(Lemonade, demandForLemonade);
        TestMarket.InitializeDemandForSpecificGood(Lemonade, 500);
        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, .5f);
        var dummyMetric = 1;

        var expectedDemand = 750;
        var expectedResult = LemonadeStandResultObject.Success();

        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(Period);
        var actualResult = TestDemandStrategy.AdjustDemandBasedOnElasticityAndHistory(TestMarket, Lemonade, ElasticityTypeEnum.SaturationElasticity,dummyMetric);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
        Assert.AreEqual(expectedResult.Result, actualResult.Result);
    }
    [TestCase(TestName="If the saturation elasticity is 1, and decrease is passed, demand should be eliminated")]
    public void ADBE_IfSaturationElasticityIsOneDemandShouldDisappear()
    {
        //Arrange
        var maxDemand = 500;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = maxDemand };
        TestPopulation.SetDemand(Lemonade, demandForLemonade);

        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, 1);
        var dummyMetric = -1;

        var expectedDemand = 0;
        var expectedResult = LemonadeStandResultObject.Success();
        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(Period);
        var actualResult = TestDemandStrategy.AdjustDemandBasedOnElasticityAndHistory(TestMarket, Lemonade, ElasticityTypeEnum.SaturationElasticity,dummyMetric);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
        Assert.AreEqual(expectedResult.Result, actualResult.Result);
    }
#endregion

#region Population tests
    [TestCase(TestName="If the population elasticity is .7, and population doubles, demand should increase by 70%")]
    public void ADFP_DemandIncreasesBySeventyPercentWhenPopulationDoubles()
    {
        //Arrange
        var maxDemand = 170;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = maxDemand };
        TestPopulation.SetDemand(Lemonade, demandForLemonade);
        Lemonade.Elasticities.Add(ElasticityTypeEnum.PopulationElasticity, .7f);
        
        TestMarket.CurrentPeriod = 1;

        ((MockDemographicManager)TestDemographicManager).SetPopulationHistory(new List<PopulationHistory>()
        {
            new() {Population = 100, Period = 0, MarketId = TestMarket.MarketId, Phase = TurnPhase.Beginning},
            new() {Population = 100, Period = 0, MarketId = TestMarket.MarketId, Phase = TurnPhase.End},
            new() {Population = 200, Period = 1, MarketId = TestMarket.MarketId , Phase = TurnPhase.Beginning},
            new() {Population = 200, Period = 1, MarketId = TestMarket.MarketId , Phase = TurnPhase.End}
        });

        var expectedDemand = 170;

        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(TestMarket.CurrentPeriod);
        TestDemandStrategy.AdjustDemandForPopulation(TestMarket, Lemonade);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    [TestCase(TestName="If the population elasticity is .7, and population halves, demand should decrease by 35%")]
    public void ADFP_DemandDecreasesByThirtyPercentWhenPopulationHalves()
    {
        //Arrange
        var maxDemand = 100;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = maxDemand };
        TestPopulation.SetDemand(Lemonade, demandForLemonade);
        Lemonade.Elasticities.Add(ElasticityTypeEnum.PopulationElasticity, .7f);
        
        TestMarket.CurrentPeriod = 1;

        ((MockDemographicManager)TestDemographicManager).SetPopulationHistory(new List<PopulationHistory>()
        {
            new() {Population = 200, Period = 0, MarketId = TestMarket.MarketId, Phase = TurnPhase.Beginning},
            new() {Population = 200, Period = 0, MarketId = TestMarket.MarketId, Phase = TurnPhase.End},
            new() {Population = 100, Period = 1, MarketId = TestMarket.MarketId, Phase = TurnPhase.Beginning},
            new() {Population = 100, Period = 1, MarketId = TestMarket.MarketId, Phase = TurnPhase.End}
        });

        var expectedDemand = 65;

        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        TestMarket.UpdateFulfillmentRates(TestMarket.CurrentPeriod);
        TestDemandStrategy.AdjustDemandForPopulation(TestMarket, Lemonade);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
#endregion
#region Saturation tests
    [TestCase(TestName="A good's demand should not change if the saturation elasticity is 1 and the good supply is equal to demand")]
    public void SaturatedGoodElasticityOneDemandUnchanged()
    {
        //Arrange
        Company1.GetInventory().AddGood(new(Lemonade, 100,10m,0));
        TestMarket.InitializeDemandForSpecificGood(Lemonade, 100);
            var marketDemand = TestMarket.GetPopulationDemand();
            var lemonadeDemand = marketDemand[Lemonade];

        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, 1);
        var Company1SellsLemonadeToAnyone = new Order(null,Company1,Lemonade,100,1)
                {
                    SubmittingCompany=Company1,
                    FilledQuantity=100
                };
        var result = Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone,TestMarket,Period));    
        var expectedDemand = 100;
        //Act
        LinearDemandStrategy.AdjustDemandForSaturation(TestMarket, Lemonade);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    [TestCase(TestName="A good's demand should double if the saturation elasticity is 1 and the good is supplied at zero")]
    public void UndersuppliedGoodElasticityOneDemandDoubles()
    {
        //Arrange
        TestMarket.InitializeDemandForSpecificGood(Lemonade, 100);
            var marketDemand = TestMarket.GetPopulationDemand();
            var lemonadeDemand = marketDemand[Lemonade];

        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, 1);
        var expectedDemand = 200;
        //Act
        LinearDemandStrategy.AdjustDemandForSaturation(TestMarket, Lemonade);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }

    [TestCase(TestName="A good's demand should decrease if the saturation elasticity is 1 and the good is supplied at greater than demand")]
    public void OverSuppliedDemandFalls()
    {
        //Arrange
        const int initialDemand = 100;
        Company1.GetInventory().AddGood(new(Lemonade, 200,10m,0));
        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, 1);
        TestMarket.InitializeDemandForSpecificGood(Lemonade, initialDemand);
            var marketDemand = TestMarket.GetPopulationDemand();
            var lemonadeDemand = marketDemand[Lemonade];
        
        var Company1SellsLemonadeToAnyone = new Order(null,Company1,Lemonade,200,1)
                    {
                        SubmittingCompany=Company1,
                        FilledQuantity=100
                    };
        
        var result = Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone,TestMarket,Period));
        //Act
        LinearDemandStrategy.AdjustDemandForSaturation(TestMarket, Lemonade);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.IsTrue(actualDemand < initialDemand,$"Demand was expected to decrease from {initialDemand} but was {actualDemand}");
    }
    [TestCase(TestName="A good's demand should increase,but not double if the saturation elasticity is .5 and the good is supplied at zero")]
    public void UndersuppliedGoodElasticityPointFiveDemandIncreases()
    {
        //Arrange
        TestMarket.InitializeDemandForSpecificGood(Lemonade, 100);

        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, .5f);
        var doubleDemand = 200;

        //Act
        LinearDemandStrategy.AdjustDemandForSaturation(TestMarket, Lemonade);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.IsTrue(0< actualDemand && actualDemand < doubleDemand,$"Demand was expected to increase less than double from 100, but was {actualDemand}");
    }
#endregion
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy.gameObject);
        TestMarket = null;
        Lemonade = null;
        TestMarketDataService = null;
        Company1 = null;
        Company2 = null;
    }
}