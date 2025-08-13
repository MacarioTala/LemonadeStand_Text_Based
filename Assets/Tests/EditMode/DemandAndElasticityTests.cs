using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class DemandAndElasticityTests
{
    Good Lemonade;
    Good Lemon;
    Good Sugar;
    Good Ice;
    Good Water;

    Recipe LemonadeRecipe;

    GoodEffect ReduceEnnuiEffect;
    PopulationAgent TestPopulation;
    EconAgent Company1;
    Market TestMarket;
    iStrategy testPopulationStrategy;
    iDemandStrategy TestDemandStrategy;

    [SetUp]
    public void Setup()
    {
        // This method is called before each test.
        // You can initialize any common resources here
        var initialPopulation = 1000;
        Lemon = new GoodBuilder()
            .Named("Lemon")
            .WithRarity(RarityEnum.Common)
            .Costing(.5m)
            .Build();

        Sugar = new GoodBuilder()
            .Named("Sugar")
            .WithRarity(RarityEnum.Common)
            .Costing(.2m)
            .Build();

        Water = new GoodBuilder()
            .Named("Water")
            .WithRarity(RarityEnum.Common)
            .Costing(.1m)
            .Build();

        Lemonade = new GoodBuilder()
            .Named("Lemonade")
            .WithRarity(RarityEnum.Uncommon)
            .WhichIsProducedGood()
            .Build();

        ReduceEnnuiEffect = new GoodEffect()
                    .Named("Reduce Ennui")
                    .DescribedAs("Reduces the ennui of the population by 0.3")
                    .WithEffectMagnitude(0.3f)
                    .WithEffect(new MetricModifier<PopulationAgent>(
                           c => c.Ennui,
                           (c, newValue) => c.Ennui = newValue));

        Lemonade.AddEffect(ReduceEnnuiEffect);

        var ingredients = new List<Ingredient>()
        {
            new(Lemon, 1),
            new(Sugar, 1),
            new(Water, 3),
        };

        LemonadeRecipe = new Recipe("Lemonade", Lemonade, ingredients);

        testPopulationStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                    .WithAggressionLevel(0.9m)
                    .Build();
        
        TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();

        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
                    .Named("Test Population")
                    .WithPopulation(initialPopulation)
                    .WithEnnui(.99f)
                    .WithBehaviourStrategy(testPopulationStrategy)
                    .Build();

        TestMarket = Market.Factory.CreateStarterMarket("Test Market", AgentLevelEnum.Market, TestDemandStrategy);
        TestMarket.RegisterMarketParticipant(TestPopulation);
    }
    [TearDown]
    public void TearDown()
    {
        // This method is called after each test.
        // You can clean up any resources initialized in Setup here.
        Object.DestroyImmediate(TestMarket);
        TheEconomy.Instance.ClearEconomy();
        TestPopulation = null;
        Lemonade = null;
        Lemon = null;
        Sugar = null;
        Water = null;
        LemonadeRecipe = null;
        ReduceEnnuiEffect = null;
    }

    #region Gen elastic demand components > T_0
    /// <summary>
    /// Elasticity is a change response measure. 
    /// Meaning it needs an underlying change in some state for it to evaluate.
    /// The market at period 0 will not have any state change.
    /// Use <#placeholder> methods to test demand for period 0.
    /// </summary>
    [Test]
    public void ElasticDemandComponentsWithZeroElasticityDoNotChangeDemand()
    {
        // Arrange
        var elasticity = 0f; // Zero elasticity
        var percentChangeInFactor = 0.1f; // 10% increase
        var currentDemand = 100; // Current demand
        var zeroElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity // Set zero elasticity
        };
        var expectedDemandAdjustment = 0; // No change in demand

        // Act
        var actualDemandAdjustment = zeroElasticityComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandAdjustment, actualDemandAdjustment, "Demand should not change when elasticity is zero.");
    }

    [Test]
    public void IfFactorWithElasticityOneIncreases10Percent_ThenDemandDecreases10Percent()
    {
        // Arrange
        var elasticity = -1.0f; // Elasticity of 1
        var percentChangeInFactor = 0.10f; // 10% increase
        var currentDemand = 100; // Current demand
        var expectedDemandDecrease = -10; // 10% of 100
        var unitaryElasticComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity
        };

        // Act
        var actualDemandIncrease = unitaryElasticComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandDecrease, actualDemandIncrease, "Demand increase should be 10% of current demand when elasticity is 1.");
    }
    [Test]
    public void FactorOfElasticityPointFiveIncreases10Percent_ThenDemandDecreases5Percent()
    {
        // Arrange
        var elasticity = -0.5f; // Elasticity of 0.5
        var percentChangeInFactor = 0.10f; // 10% increase
        var currentDemand = 100; // Current demand
        var expectedDemandDecrease = -5; // 5% of 100
        var relativelyElasticComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity
        };

        // Act
        var actualDemandIncrease = relativelyElasticComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandDecrease, actualDemandIncrease, "Demand increase should be 5% of current demand when elasticity is 0.5.");
    }

    [Test]
    public void FactorOfElasticityPointFiveDecreases10Percent_ThenDemandIncreases5Percent()
    {
        // Arrange
        var elasticity = -0.5f; // Elasticity of 0.5
        var percentChangeInFactor = -0.10f; // 10% decrease
        var currentDemand = 100; // Current demand
        var expectedDemandIncrease = 5; // 5% of 100
        var relativelyElasticComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity
        };

        // Act
        var actualDemandIncrease = relativelyElasticComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandIncrease, actualDemandIncrease, "Demand increase should be 5% of current demand when elasticity is 0.5.");
    }
    [Test]
    public void FactorOfElasticityOnePointFiveIncreases10Percent_ThenDemandDecreases15Percent()
    {
        // Arrange
        var elasticity = -1.5f; // Elasticity of 1.5
        var percentChangeInFactor = 0.10f; // 10% increase
        var currentDemand = 100; // Current demand
        int expectedDemandDecrease = -15; // 15% of 100
        var elasticDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity
        };

        // Act
        var actualDemandIncrease = elasticDemandComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandDecrease, actualDemandIncrease, "Demand increase should be 15% of current demand when elasticity is 1.5.");
    }
    [Test]
    public void ElasticityPushesAdjustmentBeyondMax_OnlyMaxReturned()
    {
        // Arrange
        var elasticity = -5f; // Very elastic
        var percentChangeInFactor = 0.5f; // 50% increase
        var currentDemand = 100; // Current demand
        var maxPercentageChange = -0.3f; // Clamp upper bound at -30%

        // Without clamping, this would be -250% change (i.e., -250 units)
        // But we should clamp to -30% of 100 = -30

        var component = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            MaxPercentageChange = maxPercentageChange
        };
        var expectedDemandAdjustment = -30; // Should clamp to max allowed decrease

        // Act
        var actualDemandAdjustment = component.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandAdjustment, actualDemandAdjustment,
            "Demand adjustment should be clamped to the maximum allowed percentage decrease.");
    }

    [Test]
    public void UnboundedComponentWith100ElasticityAndDoubledFactorIncreasesDemand100X()
    {
        // Arrange
        var elasticity = -100f; // Extremely elastic
        var percentChangeInFactor = 1f; // 100% increase
        var currentDemand = 100; // Current demand
        var component = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity
        };
        var expectedDemandAdjustment = -10000;

        //Act
        var actualDemandAdjustment = component.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemandAdjustment, actualDemandAdjustment,
            "Demand adjustment should be 100 times the current demand when elasticity is -100 and factor is doubled.");
    }

    [Test]
    public void AtBlackSwanLevelsOfChangeDemandCollapsesToZero()
    {
        // Arrange
        var elasticity = -1f;
        var blackSwanLevelOfChange = 1f; // Black Swan level of increase
        var percentChangeInFactor = blackSwanLevelOfChange; // Black Swan level of increase
        var currentDemand = 100; // Current demand
        var blackSwanComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            BlackSwanToZeroLevel = 1f, // Set Black Swan level for collapse to zero
        };
        var expectedDemandChange = -100; // Demand collapses to zero at Black Swan levels of change

        // Act
        var actualDemand = blackSwanComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemandChange, actualDemand, "Demand should collapse to zero at Black Swan levels of change.");
    }
    [Test]
    public void AtBlackSwanLevelsOfChangeDemandGoesToVerticalLevel()
    {
        //Arrange
        var elasticity = -1f;
        var blackSwanLevelOfChange = -1f; // Black Swan level of increase
        var percentChangeInFactor = blackSwanLevelOfChange; // Black Swan level of increase
        var currentDemand = 100; // Current demand
        var blackSwanComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            BlackSwanToVerticalLevel = blackSwanLevelOfChange, // Set Black Swan level for vertical level
        };
        var expectedDemand = int.MaxValue; // Demand goes to vertical level at Black Swan levels of change

        // Act
        var actualDemand = blackSwanComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemand, actualDemand, "Demand should go to vertical level at Black Swan levels of change.");
    }
    [Test]
    public void GoodWithMaxDemandAdjustMentSetOnlyReducesDemandByMaxAdjustMent()
    {
        //Arrange
        var elasticity = -1f; // Elasticity of 1
        var percentChangeInFactor = 0.5f; // 50% increase
        var currentDemand = 100; // Current demand
        var maxDemandAdjustment = -.25f; // Maximum demand adjustment
        var maxDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            MaxPercentageChange = maxDemandAdjustment // Set maximum demand adjustment
        };
        int expectedDemandDecrease = -25;

        // Act
        var actualDemandAdjustment = maxDemandComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemandDecrease, actualDemandAdjustment, "Demand should only be reduced by the maximum demand adjustment.");
    }
    [Test]
    public void GoodWithMaxDemandAdjustMentNotMetSetsNormalAdjustment()
    {
        //Arrange
        var elasticity = -1f; // Elasticity of 1
        var percentChangeInFactor = 0.5f; // 50% increase
        var currentDemand = 100; // Current demand
        var maxDemandAdjustment = -.6f; // Maximum demand adjustment
        var maxDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            MaxPercentageChange = maxDemandAdjustment // Set maximum demand adjustment
        };
        int expectedDemandDecrease = -50;

        // Act
        var actualDemandAdjustment = maxDemandComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemandDecrease, actualDemandAdjustment, "Demand should only be reduced by the maximum demand adjustment.");
    }

    [Test]
    public void GoodWithMinDemandAdjustMentSetDecreasesDemandAtLeastByMinAdjustMent()
    {
        // Arrange
        var elasticity = -1f; // Elasticity of 1
        var percentChangeInFactor = 0.00001f; // Very small increase
        var currentDemand = 100; // Current demand
        var minDemandAdjustment = -0.1f; // Minimum demand adjustment
        var minDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            MinPercentageChange = minDemandAdjustment // Set minimum demand adjustment
        };
        int expectedDemandDecrease = -10; // Minimum demand adjustment of 10% of current demand

        // Act
        var actualDemandAdjustment = minDemandComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandDecrease, actualDemandAdjustment, "Demand should be reduced by at least the minimum demand adjustment.");
    }

    [Test]
    public void GoodWithMinDemandAdjustMentNotMetSetsNormalAdjustment()
    {
        // Arrange
        var elasticity = -1f; // Elasticity of 1
        var percentChangeInFactor = .5f; // 50% increase
        var currentDemand = 100; // Current demand
        var minDemandAdjustment = -0.1f; // Minimum demand adjustment
        var minDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            MinPercentageChange = minDemandAdjustment // Set minimum demand adjustment
        };
        int expectedDemandDecrease = -50; // 50% of current demand

        // Act
        var actualDemandAdjustment = minDemandComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemandDecrease, actualDemandAdjustment, "Demand should be reduced by the normal adjustment when minimum demand adjustment is not met.");
    }

    [Test]
    public void IfBothBlackSwanToZeroAndVerticalMet_ThenZeroTakesPrecedence()
    {
        // Arrange
        var elasticity = 1f; // Veblen good (positive elasticity)
        var percentChangeInFactor = 1f; // Extreme positive change (e.g., massive price spike)
        var currentDemand = 100;

        var demandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            BlackSwanToZeroLevel = 1f,      // Hit exactly
            BlackSwanToVerticalLevel = 1f   // Also hit exactly
        };

        var expectedDemandChange = -currentDemand; // Zero takes precedence

        // Act
        var actualDemand = demandComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);

        // Assert
        Assert.AreEqual(expectedDemandChange, actualDemand, "When both Black Swan thresholds are met, demand should collapse to zero.");
    }

    [Test]
    public void VeblenGoodSetsDemandToMaxAtBlackSwanLevelOfIncreaseInFactor()
    {
        // Arrange
        var elasticity = 1;// Elasticity of 1 for Veblen good
        var blackSwanLevelOfChange = 1f; // Black Swan level of increase
        var percentChangeInFactor = blackSwanLevelOfChange; // Black Swan level of increase
        var currentDemand = 100; // Current demand
        var veblenGoodComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            BlackSwanToVerticalLevel = 1f, // Set Black Swan level for Veblen good
        };
        var expectedDemand = int.MaxValue; // Demand goes to vertical level at Black Swan levels of change

        // Act
        var actualDemand = veblenGoodComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemand, actualDemand, "Demand should go to vertical level at Black Swan levels of change for Veblen goods.");
    }

    [Test]
    public void VeblenGoodSetsDemandToZeroAtBlackSwanLevelOfDecreaseInFactor()
    {
        // Arrange
        var elasticity = 1;// Elasticity of 1 for Veblen good
        var blackSwanLevelOfChange = -1f; // Black Swan level of decrease
        var percentChangeInFactor = blackSwanLevelOfChange; // Black Swan level of decrease
        var currentDemand = 100; // Current demand
        var veblenGoodComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = elasticity,
            BlackSwanToZeroLevel = blackSwanLevelOfChange, // Set Black Swan level for Veblen good
        };
        var expectedDemandChange = -100; // Demand collapses to zero at Black Swan levels of change for Veblen goods

        // Act
        var actualDemand = veblenGoodComponent.GetDemandAdjustment(percentChangeInFactor, currentDemand);
        // Assert
        Assert.AreEqual(expectedDemandChange, actualDemand, "Demand should collapse to zero at Black Swan levels of change for Veblen goods.");
    }
    #endregion

    #region Population Elasticity tests

    [TestCase(1000, 1f, 1f,  .1f, 100, TestName = "From Elastic D Comps. Pop Elasticity:1, Population:1000, PercentChange:0.1, Expectedchange:100")]
    [TestCase(1000, 1f, 1f,   1f, 1000, TestName = "From Elastic D Comps. Pop Elasticity:1, Population:1000, PercentChange: double, Expectedchange:1000")]
    [TestCase(1000, 1f, 1f, -.5f, -500, TestName = "From Elastic D Comps. Pop Elasticity:1, Population:1000, PercentChange:halved, Expectedchange:-500")]
    [TestCase(1000, 1f,.7f,  .1f, 70, TestName = "From Elastic D Comps. Pop Elasticity:0.7, Population:1000, PercentChange:0.1, Expectedchange:70")]
    public void DemandAdjustmentTests_Population_FromElasticDemandComponents(
        int initialPopulation,
        float percentOfPopulationWantingThisGood,
        float populationElasticity,
        float percentChangeInPopulation,
        int expectedDemandChange
    )
    {
        //Arrange
        TestPopulation.Population = initialPopulation;
        var populationDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Population,
            Elasticity = populationElasticity
        };
        var elasticDemandComponents = new List<ElasticDemandComponent>
        {
            populationDemandComponent
        };
        var result = TestPopulation.InitializeDemandBasedOnPopulation(good: Lemonade, percentOfPopulation: percentOfPopulationWantingThisGood, elasticDemandComponents: elasticDemandComponents);
        Assert.AreEqual(result, LemonadeStandResultObject.Success(), result.Message);
        var stateChanges = new Dictionary<ElasticDemandComponentEnum, float>
        {
            { ElasticDemandComponentEnum.Population, percentChangeInPopulation } // change in population
        };
        var expectedDemand = expectedDemandChange;
        var initialDemand = TestPopulation.GetDemandFor(Lemonade).CurrentDemand;

        //Act
        var actualDemand = TestPopulation.GetDemandFor(Lemonade)
                        .ElasticDemandComponents
                        .Find(c => c.Type == ElasticDemandComponentEnum.Population)
                        .GetDemandAdjustment(stateChanges[ElasticDemandComponentEnum.Population]
                                             , initialDemand);
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    [TestCase(1000, 1f, 1f, .1f, 1100, TestName = "From DemandData. Pop Elasticity:1, Population:1000, PercentChange:0.1, Expected Demand:1100")]
    [TestCase(1000, 1f, 1f,  1f, 2000, TestName = "From DemandData. Pop Elasticity:1, Population:1000, PercentChange: double, Expected Demand:2000")]
    [TestCase(1000, 1f, 1f,-.5f, 500, TestName = "From DemandData. Pop Elasticity:1, Population:1000, PercentChange:halved, Expected Demand:500")]
    [TestCase(1000, 1f,.7f, .1f, 1070, TestName = "From DemandData. Pop Elasticity:0.7, Population:1000, PercentChange:0.1, Expected Demand:1070")]
    public void DemandAdjustmentTests_Population_FromDemandData(
        int initialPopulation,
        float percentOfPopulationWantingThisGood,
        float populationElasticity,
        float percentChangeInPopulation,
        int expectedDemandChange
    )
    {
         //Arrange
        TestPopulation.Population = initialPopulation;
        var populationDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Population,
            Elasticity = populationElasticity
        };
        var elasticDemandComponents = new List<ElasticDemandComponent>
        {
            populationDemandComponent
        };
        var result = TestPopulation.InitializeDemandBasedOnPopulation(good: Lemonade, percentOfPopulation: percentOfPopulationWantingThisGood, elasticDemandComponents:elasticDemandComponents);
        Assert.AreEqual(result, LemonadeStandResultObject.Success(), result.Message);
        var stateChanges = new Dictionary<ElasticDemandComponentEnum, float>
        {
            { ElasticDemandComponentEnum.Population, percentChangeInPopulation } // change in population
        };
        var expectedDemand = expectedDemandChange;

        //Act
        var actualDemand = TestPopulation.GetDemandFor(Lemonade)
                        .GetAdjustedDemand(stateChanges);
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    #endregion

    #region Income Elasticity tests
    [TestCase(1000, 1f, 1f,.1f, 1100, TestName = "From DemandData. Income Elasticity:1, Population:1000, PercentChange:0.1, Expectedchange:100")]
    [TestCase(1000, 1f, 1f, 1f, 2000, TestName = "From DemandData. Income Elasticity:1, Population:1000, PercentChange: double, Expectedchange:1000")]
    [TestCase(1000, 1f, 1f,-.5f, 500, TestName = "From DemandData. Pop Elasticity:1, Population:1000, PercentChange:halved, Expectedchange:-500")]
    [TestCase(1000, 1f,.7f, .1f, 1070, TestName = "From DemandData. Pop Elasticity:0.7, Population:1000, PercentChange:0.1, Expectedchange:70")]

    public void DemandAdjustmentTests_income_FromDemandData(
        int initialPopulation,
        float percentOfPopulationWantingThisGood,
        float incomeElasticity,
        float percentChangeInIncome,
        int expectedDemandChange
    )
    {
        //Arrange
        TestPopulation.Population = initialPopulation;

        var incomeDemandComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Income,
            Elasticity = incomeElasticity
        };

        var elasticDemandComponents = new List<ElasticDemandComponent>
        {
            incomeDemandComponent
        };

        var result = TestPopulation.InitializeDemandBasedOnPopulation(good: Lemonade,
                                                                      percentOfPopulation: percentOfPopulationWantingThisGood,
                                                                      elasticDemandComponents: elasticDemandComponents);
        Assert.AreEqual(result, LemonadeStandResultObject.Success(), result.Message);
        var stateChanges = new Dictionary<ElasticDemandComponentEnum, float>
        {
            { ElasticDemandComponentEnum.Income, percentChangeInIncome }
        };
        var expectedDemand = expectedDemandChange;

        //Act
        var actualDemand = TestPopulation.GetDemandFor(Lemonade)
                        .GetAdjustedDemand(stateChanges);
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    #endregion

    #region Multiple Elastic Demand Components tests
    [TestCase(1000, 1f,
                1f, .1f,
                1f, 0,
               -1f, 0
                , float.MinValue, float.MinValue, float.MinValue
                , float.MaxValue, float.MaxValue, float.MaxValue
                , float.MinValue, float.MinValue, float.MinValue
                , float.MinValue, float.MinValue, float.MinValue
                , 1100
                , TestName = "Demand adjustment only comes from factors that changed. One component changed.")]
    [TestCase(1000, 1f,
                1f, .1f,
                1f, .1f,
               -1f, 0
                , float.MinValue, float.MinValue, float.MinValue
                , float.MaxValue, float.MaxValue, float.MaxValue
                , float.MinValue, float.MinValue, float.MinValue
                , float.MinValue, float.MinValue, float.MinValue
                , 1200
                , TestName = "Demand adjustment only comes from factors that changed. Two components changed.")]
    [TestCase(1000, 1f,
                1f, 1f,
                1f, .1f,
               -1f, 0
                , float.MinValue, float.MinValue, float.MinValue
                , .5f, float.MaxValue, float.MaxValue
                , float.MinValue, float.MinValue, float.MinValue
                , float.MinValue, float.MinValue, float.MinValue
                , 1600
                , TestName = "Demand adjustment only comes from factors that changed. Two components changed. One Capped")]
    [TestCase(1000,1f, 
                1f,1f,
                1f,1f, 
               -1f,0 
                , float.MinValue, float.MinValue, float.MinValue
                , float.MaxValue, float.MaxValue, float.MaxValue
                , float.MinValue, float.MinValue, float.MinValue
                , float.MinValue, float.MinValue, float.MinValue
                , 3000
                , TestName = "Demand adjustment only comes from factors that changed. Two components changed. No Caps")]
    public void MultipleElasticDemandComponentsTests_FromDemandData(
        int initialPopulation,
        float percentoOfPopulationWantingThisGood,
        float populationElasticity,
        float percentChangeInPopulation,
        float incomeElasticity,
        float percentChangeInIncome,
        float priceElasticity,
        float percentChangeInPrice,
        float minPercentageChangePopulation,
        float minPercentageChangeIncome,
        float minPercentageChangePrice,
        float maxPercentageChangePopulation,
        float maxPercentageChangeIncome,
        float maxPercentageChangePrice,
        float blackSwanToZeroLevelPopulation,
        float blackSwanToZeroLevelIncome,
        float blackSwanToZeroLevelPrice,
        float blackSwanToVerticalLevelPopulation,
        float blackSwanToVerticalLevelIncome,
        float blackSwanToVerticalLevelPrice,
        int expectedDemandChange)
    {
        //Arrange
        TestPopulation.Population = initialPopulation;

        var incomeElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Income,
            Elasticity = incomeElasticity,
            MinPercentageChange = minPercentageChangeIncome,
            MaxPercentageChange = maxPercentageChangeIncome,
            BlackSwanToZeroLevel = blackSwanToZeroLevelIncome,
            BlackSwanToVerticalLevel = blackSwanToVerticalLevelIncome
        };

        var populationElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Population,
            Elasticity = populationElasticity,
            MinPercentageChange = minPercentageChangePopulation,
            MaxPercentageChange = maxPercentageChangePopulation,
            BlackSwanToZeroLevel = blackSwanToZeroLevelPopulation,
            BlackSwanToVerticalLevel = blackSwanToVerticalLevelPopulation
        };

        var priceElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = priceElasticity,
            MinPercentageChange = minPercentageChangePrice,
            MaxPercentageChange = maxPercentageChangePrice,
            BlackSwanToZeroLevel = blackSwanToZeroLevelPrice,
            BlackSwanToVerticalLevel = blackSwanToVerticalLevelPrice
        };

        var elasticDemandComponents = new List<ElasticDemandComponent>
        {
            incomeElasticityComponent,
            populationElasticityComponent,
            priceElasticityComponent
        };

        var result = TestPopulation.InitializeDemandBasedOnPopulation(
                                                good: Lemonade,
                                                percentOfPopulation: percentoOfPopulationWantingThisGood,
                                                elasticDemandComponents: elasticDemandComponents);
        Assert.AreEqual(result, LemonadeStandResultObject.Success(), result.Message);
        var stateChanges = new Dictionary<ElasticDemandComponentEnum, float>
        {
            { ElasticDemandComponentEnum.Population, percentChangeInPopulation },
            { ElasticDemandComponentEnum.Income, percentChangeInIncome },
            { ElasticDemandComponentEnum.Price, percentChangeInPrice }
        };

        //Act
        var actualDemandChange = TestPopulation.GetDemandFor(Lemonade)
                        .GetAdjustedDemand(stateChanges);
        //Assert
        Assert.AreEqual(expectedDemandChange, actualDemandChange);
    }
    #endregion

    #region Capped Elastic Demand Components tests
    [TestCase(  1f, 1000,1f,
                1f,
               .5f, float.MinValue,
               1500, TestName = "Single Component: Demand adjustment does not exceed max cap.")]
    [TestCase(  1f, 1000,1f,
                0f,
                float.MaxValue,.1f,
                1100, TestName = "Single Component: Demand adjustment does not fall below min cap.")]
    public void CappedDemandAdjustmentTests_FromDemandData(
        float incomeElasticity,
        int initialPopulation,
        float percentOfPopulationWantingThisGood,
        float percentChangeInIncome,
        float maxPercentageChange,
        float minPercentageChange,
        int expectedDemandChange
    )
    {
        //Arrange
        TestPopulation.Population = initialPopulation;

        var incomeElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Income,
            Elasticity = incomeElasticity,
            MinPercentageChange = minPercentageChange,
            MaxPercentageChange = maxPercentageChange
        };
        var elasticDemandComponents = new List<ElasticDemandComponent>
        {
            incomeElasticityComponent
        };

        var result = TestPopulation.InitializeDemandBasedOnPopulation(
                                                good: Lemonade,
                                                percentOfPopulation: percentOfPopulationWantingThisGood,
                                                elasticDemandComponents: elasticDemandComponents);
        Assert.AreEqual(result, LemonadeStandResultObject.Success(), result.Message);
        var stateChanges = new Dictionary<ElasticDemandComponentEnum, float>
        {
            { ElasticDemandComponentEnum.Income, percentChangeInIncome }
        };
        var expectedDemand = expectedDemandChange;

        //Act
        var actualDemand = TestPopulation.GetDemandFor(Lemonade)
                        .GetAdjustedDemand(stateChanges);
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    #endregion

    #region DemandData Max and Min tests
    [TestCase(1000, 1f,
                1f, 1f,
                1f, 1f,
                -1f, 0f,
                -1f, 0f,
                1500, int.MinValue,
                1500,
        TestName = "Demand is capped at max when two factors increase it beyond max.")]
    [TestCase(1000, 1f,
                1f, 0f,
                1f, 0f,
                -1f, 1f,
                -1f, 0f,
                int.MaxValue, 100,
                100,
    TestName = "Demand is goes to min when a factor decreases it below min.")]
    [TestCase(1000, 1f,
                1f, 0f,
                1f, 0f,
                -1f, 1f,
                -1f, 1f,
                int.MaxValue,100 ,
                100,
    TestName = "Demand is goes to min when two factors decrease it below min.")]
    public void DemandDataMaxAndMinTests(
        int initialPopulation,
        float percentOfPopulationWantingThisGood,
        float populationElasticity,
        float percentChangeInPopulation,
        float incomeElasticity,
        float percentChangeInIncome,
        float priceElasticity,
        float percentChangeInPrice,
        float monsterAttractionElasticity,
        float percentChangeInMonsterAttraction,
        int maxDemand,
        int minDemand,
        int expectedDemand
    )
    {
        //Arrange
        TestPopulation.Population = initialPopulation;
        var populationElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Population,
            Elasticity = populationElasticity
        };
        var incomeElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Income,
            Elasticity = incomeElasticity
        };
        var priceElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Price,
            Elasticity = priceElasticity
        };
        var monsterAttractionElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.MonsterAttraction,
            Elasticity = monsterAttractionElasticity
        };

        var elasticDemandComponents = new List<ElasticDemandComponent>
        {
            populationElasticityComponent,
            incomeElasticityComponent,
            priceElasticityComponent,
            monsterAttractionElasticityComponent
        };
        var result = TestPopulation.InitializeDemandBasedOnPopulation(
                                                good: Lemonade,
                                                percentOfPopulation: percentOfPopulationWantingThisGood,
                                                minDemand: minDemand,
                                                maxDemand: maxDemand,
                                                elasticDemandComponents: elasticDemandComponents);
        Assert.AreEqual(result, LemonadeStandResultObject.Success(), result.Message);
        var stateChanges = new Dictionary<ElasticDemandComponentEnum, float>
        {
            { ElasticDemandComponentEnum.Population, percentChangeInPopulation },
            { ElasticDemandComponentEnum.Income, percentChangeInIncome },
            { ElasticDemandComponentEnum.Price, percentChangeInPrice },
            { ElasticDemandComponentEnum.MonsterAttraction, percentChangeInMonsterAttraction }
        };

        //Act
        var actualDemand = TestPopulation.GetDemandFor(Lemonade)
                        .GetAdjustedDemand(stateChanges);

        //Assert
        Assert.AreEqual(expectedDemand, actualDemand,
            $"Expected demand: {expectedDemand}, but got: {actualDemand}. " +
            $"Initial Population: {initialPopulation}, Percent of Population Wanting Good: {percentOfPopulationWantingThisGood}, " +
            $"Population Elasticity: {populationElasticity}, Income Elasticity: {incomeElasticity}, Price Elasticity: {priceElasticity}," +
            $"Percent Change in Population: {percentChangeInPopulation}, Percent Change in Income: {percentChangeInIncome}, " +
            $"Percent Change in Price: {percentChangeInPrice}," +
            $"Max Demand: {maxDemand}, Min Demand: {minDemand}.");
    }
    #endregion

    #region Black Swan tests
    [TestCase(-1f, 1000, 1f, -.5f, -.5f, 2f, 0
    , TestName = "Black Swan to Zero: Demand collapses to zero at extreme negative income change.")] 
    public void BlackSwanDemandAdjustmentTests_FromDemandData(
        float incomeElasticity,
        int initialPopulation,
        float percentOfPopulationWantingThisGood,
        float percentChangeInIncome,
        float blackSwanToZeroLevel,
        float blackSwanToVerticalLevel,
        int expectedDemand)
    {
        //Arrange
        TestPopulation.Population = initialPopulation;

        var incomeElasticityComponent = new ElasticDemandComponent
        {
            Type = ElasticDemandComponentEnum.Income,
            Elasticity = incomeElasticity,
            BlackSwanToZeroLevel = blackSwanToZeroLevel,
            BlackSwanToVerticalLevel = blackSwanToVerticalLevel
        };
        var elasticDemandComponents = new List<ElasticDemandComponent>
        {
            incomeElasticityComponent
        };

        var result = TestPopulation.InitializeDemandBasedOnPopulation(
                                                good: Lemonade,
                                                percentOfPopulation: percentOfPopulationWantingThisGood,
                                                elasticDemandComponents: elasticDemandComponents
                                                );
        Assert.AreEqual(result, LemonadeStandResultObject.Success(), result.Message);
        var stateChanges = new Dictionary<ElasticDemandComponentEnum, float>
        {
            { ElasticDemandComponentEnum.Income, percentChangeInIncome }
        };

        //Act
        var actualDemand = TestPopulation.GetDemandFor(Lemonade)
                        .GetAdjustedDemand(stateChanges);
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    #endregion


    #region ElasticDemandComponents == T_0
    
    #endregion
}