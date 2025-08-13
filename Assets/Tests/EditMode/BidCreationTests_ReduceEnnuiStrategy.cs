using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

[TestFixture]
public class BidCreationTests_ReduceEnnuiStrategy
{
    Good Lemon;
    Good Water;
    Good Sugar;
    Good Lemonade;

    GoodEffect ReduceEnnuiEffect;

    Recipe LemonadeRecipe;

    iStrategy TestReduceEnnuiStrategy;

    PopulationAgent TestPopulation;

    const decimal InitialCash=2000;

    Market TestMarket;

    [SetUp]
    public void Setup()
    {
        var initialEnnui = .90f;
        var initialPopulation = 1000;
        Lemonade = new GoodBuilder()
                    .Named("Lemonade")
                    .WithRarity(RarityEnum.Uncommon)
                    .WhichIsProducedGood()
                    .Build();

        Lemon = new GoodBuilder()
                .Named("Lemon")
                .WithRarity(RarityEnum.Common)
                .Costing(.2m)
                .Build();
        Water = new GoodBuilder()
                .Named("Water")
                .WithRarity(RarityEnum.Common)
                .Costing(.2m)
                .Build();
        Sugar = new GoodBuilder()
                .Named("Sugar")
                .WithRarity(RarityEnum.Common)
                .Costing(.2m)
                .Build();

        LemonadeRecipe = new Recipe("Ordinary Lemonade"
                                    , Lemonade
                                    , new List<Ingredient>{
                                        new (Lemon,1),
                                        new (Water,3),
                                        new (Sugar,1)
                                        }
                                    );

        ReduceEnnuiEffect = new GoodEffect()
                            .Named("Reduce Ennui effect")
                            .Affecting(MetricEnum.Ennui)
                            .WithEffectMagnitude(-.05f)
                            .WithEffect(new MetricModifier<PopulationAgent>
                                            (
                                            x => x.Ennui,
                                            (x, newValue) => x.Ennui = newValue
                                            )
                                        );

        Lemonade.AddEffect(ReduceEnnuiEffect);

        TestReduceEnnuiStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                                .WithAggressionLevel(.20m)
                                .Build();

        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
                        .Named("Test Population")
                        .WithInitialCash(InitialCash)
                        .WithEnnui(initialEnnui)
                        .WithPopulation(initialPopulation)
                        .WithBehaviourStrategy(TestReduceEnnuiStrategy)
                        .AssumingNewGoodsCost(1m)
                        .Build();
        TestReduceEnnuiStrategy.GenerateGoals(TestPopulation);

        TestMarket = Market.Factory.CreateStarterMarket("Test Market", AgentLevelEnum.Market, null);

        TestMarket.RegisterMarketParticipant(TestPopulation);
    }
    #region iStrategy
    [TestCase(TestName = "When no market prices are available, the bid is equal to the Naive COG")]
    public void NoMarketPricesDefaultBid()
    {
        //Arrange
        var naiveCog = TestPopulation.GetMarketIgnorantAssumedCOG();
        var expected = naiveCog;

        //Act
        var actual = iStrategy.GetCostAnchoredBid(Lemonade, TestPopulation);

        //Assert
        Assert.AreEqual(expected, actual);
    }
    [TestCase(TestName = "From iStrategy: When no market prices are available, the bid is equal to the Naive COG.")]
    public void NoMarketPricesDefaultBidMultiplierPassed()
    {
        //Arrange
        var naiveCog = TestPopulation.GetMarketIgnorantAssumedCOG();
        var expected = naiveCog;

        //Act
        var actual = iStrategy.GetCostAnchoredBid(Lemonade, TestPopulation);

        //Assert
        Assert.AreEqual(expected, actual);
    }
    #endregion
    #region GenerateBidAskSpreads
    [Test]
    public void NoProductsMatchingGoalReturnsEmptySet()
    {
        //Arrange
        var goal = TestPopulation.Goals.Find(x => x.Name.Equals("Reduce Ennui"));

        //Act
        var actualBids = (TestReduceEnnuiStrategy as ReduceEnnuiStrategy).GenerateBidAskSpreads(TestPopulation)
        .Where(x=>x.Key.Equals(Lemonade));

        //Assert
        Assert.IsNotNull(goal,"Goal not set");
        Assert.IsFalse(actualBids.Any());
    }
    #endregion
    
    #region CompanyGetPerceivedCostOfGood
    [Test]
    public void GetPerceivedCostOfGoodReturnsMarketIgnorantAssumedCOGNoRecipes()
    {
        //Arrange
        var naiveCog = TestPopulation.GetMarketIgnorantAssumedCOG();
        var expected = naiveCog;

        //Act
        var actual = TestPopulation.GetPerceivedCostOfGood(Lemonade, null);

        //Assert 
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GetPerceivedCostOfGoodReturnsCostOfRecipePresent_NoMarketPrices()
    {
        //Arrange
        TestPopulation.Add_recipe(LemonadeRecipe);
        var expected = LemonadeRecipe.GetCostPerUnit(null);

        //Act
        var actual = TestPopulation.GetPerceivedCostOfGood(Lemonade, null);

        //Assert
        Assert.AreEqual(expected, actual);

        //TearDown
        TestPopulation.RemoveRecipe(LemonadeRecipe);
    }

    [Test]
    public void GetPerceivedCostOfGoodReturnsCostOfRecipePresent_MarketPricesPassed()
    {
        //Arrange
        TestPopulation.Add_recipe(LemonadeRecipe);
        var prices = new Dictionary<Good,decimal>
                        {
                            { Lemon,.3m },
                            { Water,.1m },
                            { Sugar,.2m }
                        };
        var expected = LemonadeRecipe.GetPerceivedCostPerUnit(prices);

        //Act
        var actual = TestPopulation.GetPerceivedCostOfGood(Lemonade, prices);

        //Assert
        Assert.AreEqual(expected, actual);

        //TearDown
        TestPopulation.RemoveRecipe(LemonadeRecipe);
    }
    [Test]
    public void GetPerceivedCostOfGoodReturnsRecipeCostFromIStrategy()
    {
        //Arrange
        TestPopulation.Add_recipe(LemonadeRecipe);
        var company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);

        var prices = new Dictionary<Good, decimal>
                        {
                            { Lemon,.3m },
                            { Water,.1m },
                            { Sugar,.2m }
                        };
        TestMarket.MarketData.Add(new MarketData { Company = company1, Good = Lemon, Ask = .3m});
        TestMarket.MarketData.Add(new MarketData { Company = company1, Good = Water, Ask = .1m });
        TestMarket.MarketData.Add(new MarketData { Company = company1, Good = Sugar, Ask = .2m});

        var expected = LemonadeRecipe.GetPerceivedCostPerUnit(prices);

        //Act
        var actual = iStrategy.GetCostAnchoredBid(Lemonade, TestPopulation);

        //Assert
        Assert.AreEqual(expected, actual);

        //Teardown
        TestMarket.MarketData.Clear();
    }
    #endregion
    #region CalculateBidPerCapita
    /// <summary>
    /// Note: This is an internal call that assumes that demand for the good is already present.
    /// </summary>

    [Test]
    public void CalculateBidPerCapitaReturnsNaiveBidWhenNoPricesExist()
    {
        //Arrange
        var naiveCog = TestPopulation.GetMarketIgnorantAssumedCOG();
        var expected = naiveCog;
        var reduceEnnuiGoal = TestPopulation.Goals
                            .Where(x => x.Name.Equals("Reduce Ennui"))
                            .FirstOrDefault();

        //Act
        var actual = (TestReduceEnnuiStrategy as ReduceEnnuiStrategy)
                        .CalculateBidPerCapita(Lemonade, TestPopulation, reduceEnnuiGoal)
                        .Bid;

        //Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void CalculateBidPerCapitaReturnsPerceivedCostOfGoodsWhenPricesExist()
    {
        //Arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);

        TestMarket.MarketData.Add(new MarketData { Company = company1, Good = Lemon, Ask = .3m });
        TestMarket.MarketData.Add(new MarketData { Company = company1, Good = Water, Ask = .1m });
        TestMarket.MarketData.Add(new MarketData { Company = company1, Good = Sugar, Ask = .2m });

        var pricedCog = LemonadeRecipe.GetCostPerUnit(null);
        var expected = pricedCog;
        var reduceEnnuiGoal = TestPopulation.Goals
                            .Where(x => x.Name.Equals("Reduce Ennui"))
                            .FirstOrDefault();
       

        //Act
        var actual = (TestReduceEnnuiStrategy as ReduceEnnuiStrategy)
                        .CalculateBidPerCapita(Lemonade, TestPopulation, reduceEnnuiGoal)
                        .Bid;

        //Assert
        Assert.AreEqual(expected, actual);

        //TearDown
        TestMarket.MarketData.Clear();
    }
    #endregion
    #region Initial Bid Creation
    [TestCase(TestName = "If demand for a good exists, a bid is created, even when no recipes exist")]
    public void DemandExistsNoRecipesBuysStillCreated()
    {
        //Arrange
        var populationMarket = TestPopulation.GetMarket();
        var companiesInMarket = populationMarket.GetMarketParticipants()
                                .Where(x => !x.Equals(TestPopulation));
        TestPopulation.Recipes.Clear();
        TestPopulation.InitializeDemandBasedOnPopulation(Lemonade, 1f);
        var initialOrderCount = TestMarket.GetOrdersSentToMarket().Count();

        var affectsEnnui = Lemonade.HasEffectOn(MetricEnum.Ennui);

        //Act
        TestPopulation.PerformStrategy(populationMarket.CurrentPeriod);
        var ordersPostStrategy = TestMarket.GetOrdersSentToMarketByCompany(TestPopulation);

        //Assert
        Assert.IsTrue(affectsEnnui,"Lemonade has no effect on ennui");
        Assert.IsTrue(populationMarket.Equals(TestMarket), "Population not in market");
        Assert.IsTrue(companiesInMarket?.Count() == 0, "There are other companies in the market");
        Assert.IsTrue(initialOrderCount == 0, "Should be no initial orders");
        Assert.IsTrue(ordersPostStrategy.Count() > 0, "Orders should exist after strategy is performed");
    }
    [TestCase(1, .5, .5, 4000,1, TestName = "Naive bid of .5, total aggression, company orders 4000 units")]
    [TestCase(.2, .5, .5, 800,1, TestName = "Naive bid of .5, 20% aggression, company orders 800 units")]
    [TestCase(0,.5, null, null,0, TestName = "Naive bid of .5, no aggression, company orders nothing")]
    public void InitialBidTestsDifferentBids(decimal aggressionLevel,
                                             decimal naiveBid,
                                             decimal? expectedBid,
                                             int? expectedQty,
                                             int expectedNumOrders)
    {
        //Arrange
        TestPopulation.SetMarketIgnorantAssumedCOG(naiveBid);
        TestPopulation.SetAggressionLevel(aggressionLevel);
        var populationMarket = TestPopulation.GetMarket();
        var companiesInMarket = populationMarket.GetMarketParticipants()
                                .Where(x => !x.Equals(TestPopulation));
        TestPopulation.Recipes.Clear();
        TestPopulation.InitializeDemandBasedOnPopulation(Lemonade, 1f);

        //Act
        TestPopulation.PerformStrategy(populationMarket.CurrentPeriod);
        var ordersPostStrategy = TestMarket.GetOrdersSentToMarketByCompany(TestPopulation);
        var actualBid = ordersPostStrategy.FirstOrDefault()?.Price;
        var actualQty = ordersPostStrategy.FirstOrDefault()?.Quantity;
        var actualNumOrders = ordersPostStrategy.Count();

        //Assert
        Assert.AreEqual(expectedNumOrders, actualNumOrders, $"Expected {expectedNumOrders} orders but got {actualNumOrders}");
        Assert.AreEqual(expectedBid, actualBid);
        Assert.AreEqual(expectedQty, actualQty);
    }

    #endregion
    [TearDown]
    public void TearDown()
    {
        TestMarket = null;
        TheEconomy.Instance.ClearEconomy();
    }
}