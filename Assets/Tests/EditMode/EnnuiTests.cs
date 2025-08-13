#if !UNITY_BURST
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class EnnuiTests
{
   Good Lemonade;
   Goal ReduceEnnuiGoal;
   int Period = 0;

   Market TestMarket;
   iDemandStrategy TestDemandStrategy;

   [SetUp]
   public void Setup()
   {
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

      ReduceEnnuiGoal = new Goal()
                .Named("Reduce Ennui")
                .DescribedAs("Reduce the ennui of the population to 0")
                .WithGoalEvaluator(c => c is PopulationAgent populationCompany && populationCompany.Ennui == 0)
                .WithGoalInitializer((c, g) => g.SetOriginalValue("Ennui", ((PopulationAgent)c).Ennui))
                .Affecting(MetricEnum.Ennui)
                .WithGoalValue(.5f);

      TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
      var mockDemographicManager = new Mock<iDemographicManager>().Object;
      TestMarket = Market.Factory.CreateStarterMarket("Test Market", AgentLevelEnum.Market, TestDemandStrategy)
               .WithDemographicManager(mockDemographicManager);

   }

   [Test]
   public void PopulationsThatMaxOutOnEnnuiCollapse()
   {
      //Arrange
      var population = EconAgentBuilder.For<PopulationAgent>()
         .Named("Really Lazy Population")
         .WithEnnui(1)
         .Build();
      TestMarket.RegisterMarketParticipant(population);
      var actualParticipantCountPreMarketForces = TestMarket.GetMarketParticipants().Count();
      const int expectedParticipantcountPreMarketForces = 1;
      const int expectedParticipantcountPostMarketForces = 0;

      //Act
      TestMarket.UnleashMarketForces(TestMarket.CurrentPeriod);
      var actualParticipantcountPostMarketForces = TestMarket.GetMarketParticipants().Count();

      //Assert
      Assert.AreEqual(expectedParticipantcountPreMarketForces, actualParticipantCountPreMarketForces);
      Assert.AreEqual(expectedParticipantcountPostMarketForces, actualParticipantcountPostMarketForces);
      //Teardown
      TestMarket.RemoveMarketParticipant(population);
   }
   #region ReduceEnnuiStrategy
   [Test]
   public void RE_CalculateBidPerCapitaGeneratesBid()
   {
      // Arrange
      const int initialPopulation = 100;
      const float initialEnnui = .99f;
      var demandForLemonade = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };
      var listOfDemands = new Dictionary<Good, DemandData>()
      {
         {Lemonade, demandForLemonade}
      };

      var strategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                     .WithAggressionLevel(.5m)
                     .Build();

      var population = EconAgentBuilder.For<PopulationAgent>()
         .Named("Test Population")
         .WithInitialCash(1000)
         .WithEnnui(initialEnnui)
         .WithPopulation(initialPopulation)
         .WithBehaviourStrategy(strategy)
         .Demanding(listOfDemands)
         .Build();
      var ennuiReducingEffect = Lemonade.ReducesMetric(MetricEnum.Ennui).By();

      // Act
      strategy.GenerateGoals(population);
      strategy.RemoveGoals(ReduceEnnuiGoal.Name, population);
      population.AddGoal(ReduceEnnuiGoal);
      var ennuiToZeroGoal = population.Goals[0];
      var actualBid = strategy.CalculateBidPerCapita(Lemonade, population, ennuiToZeroGoal).Bid;

      // Assert
      Assert.That(actualBid, Is.GreaterThan(0m));
      Debug.Log($"Bid per capita for {Lemonade.GoodName} is {actualBid}");
      Debug.Log($"Ennui: {initialEnnui} → {ennuiToZeroGoal.MetricTarget} | Impact: {ennuiReducingEffect:F5}");
   }

   [Test]
   public void SuperiorGoodGetsHigherAllocation()
   {
      //Arrange
      const int initialPopulation = 100;
      const float initialEnnui = .99f;
      Good FruitPunch = new GoodBuilder()
         .Named("Fruit Punch")
         .WithRarity(RarityEnum.Uncommon)
         .Costing(5m)
         .WhichIsProducedGood()
         .Build();

      var FruitPunchEffect = new GoodEffect()
      .Named("Fruit Punch Effect")
      .Affecting(MetricEnum.Ennui)
      .WithEffectMagnitude(-.10f)
      .WithEffect(new MetricModifier<PopulationAgent>(
                c => c.Ennui,
                (c, newValue) => c.Ennui = newValue));
      FruitPunch.AddEffect(FruitPunchEffect);

      var demandForLemonade = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };

      var demandForFruitPunch = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };

      var listOfDemands = new Dictionary<Good, DemandData>()
            {
              {Lemonade,demandForLemonade},
              {FruitPunch,demandForFruitPunch}
            };
      var strategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                     .WithAggressionLevel(.5m)
                     .Build();

      var population = EconAgentBuilder.For<PopulationAgent>()
         .Named("Test Population")
         .WithInitialCash(1000)
         .WithEnnui(initialEnnui)
         .WithPopulation(initialPopulation)
         .WithBehaviourStrategy(strategy)
         .Demanding(listOfDemands)
         .Build();

      strategy.GenerateGoals(population);

      //Act
      var ActualDemand = strategy.GenerateBidAskSpreads(population);
      var LemonAllocation = ActualDemand.Where(d => d.Key == Lemonade).FirstOrDefault().Value.Allocation;
      var FruitPunchAllocation = ActualDemand.Where(d => d.Key == FruitPunch).FirstOrDefault().Value.Allocation;

      //Assert
      Assert.IsTrue(LemonAllocation > FruitPunchAllocation);
   }
   [Test]
   public void GenerateBidAskSpreadsAllocationSumsToOneIfThereIsOnlyOneGood()
   {
      //Arrange
      const int initialPopulation = 100;
      const float initialEnnui = .99f;

      var demandForLemonade = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };

      var listOfDemands = new Dictionary<Good, DemandData>()
            {
              {Lemonade,demandForLemonade}
            };

      var strategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                     .WithAggressionLevel(.5m)
                     .Build();
      var population = EconAgentBuilder.For<PopulationAgent>()
         .Named("Test Population")
         .WithInitialCash(1000)
         .WithEnnui(initialEnnui)
         .WithPopulation(initialPopulation)
         .WithBehaviourStrategy(strategy)
         .Demanding(listOfDemands)
         .Build();
      strategy.GenerateGoals(population);
      const decimal expectedAllocation = 1.0m;
      //Act
      var ActualDemand = strategy.GenerateBidAskSpreads(population);
      var ActualAllocation = ActualDemand.Sum(d => d.Value.Allocation);
      //Assert
      Assert.That(ActualAllocation, Is.EqualTo(expectedAllocation).Within(0.01m));
   }

   [Test]
   public void GenerateBidAskSpreadsAllocationSumsToOneIfThereAreMultipleGoods()
   {
      //Arrange
      const int initialPopulation = 100;
      const float initialEnnui = .99f;

      Good FruitPunch = new GoodBuilder()
         .Named("Fruit Punch")
         .WithRarity(RarityEnum.Uncommon)
         .Costing(5m)
         .WhichIsProducedGood()
         .Build();

      var demandForLemonade = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };

      var demandForFruitPunch = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };
      var listOfDemands = new Dictionary<Good, DemandData>()
            {
              {Lemonade,demandForLemonade},
              {FruitPunch,demandForFruitPunch}
            };
      var strategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                     .WithAggressionLevel(.5m)
                     .Build();

      var population = EconAgentBuilder.For<PopulationAgent>()
         .Named("Test Population")
         .WithInitialCash(1000)
         .WithEnnui(initialEnnui)
         .WithPopulation(initialPopulation)
         .WithBehaviourStrategy(strategy)
         .Demanding(listOfDemands)
         .Build();
      strategy.GenerateGoals(population);
      const decimal expectedAllocation = 1.0m;

      //Act
      var ActualDemand = strategy.GenerateBidAskSpreads(population);
      var ActualAllocation = ActualDemand.Sum(d => d.Value.Allocation);

      //Assert
      Assert.That(ActualAllocation, Is.EqualTo(expectedAllocation).Within(0.01m));
   }

   [Test]
   public void CreateBuysReturnsBuysThatDoNotExceedCash()
   {
      //Arrange
      const int initialPopulation = 100;
      const float initialEnnui = .99f;

      var demandForLemonade = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };
      var listOfDemands = new Dictionary<Good, DemandData>()
            {
              {Lemonade,demandForLemonade}
            };
      var strategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                     .WithAggressionLevel(.5m)
                     .Build();

      var population = EconAgentBuilder.For<PopulationAgent>()
         .Named("Test Population")
         .WithInitialCash(1000)
         .WithEnnui(initialEnnui)
         .WithPopulation(initialPopulation)
         .WithBehaviourStrategy(strategy)
         .Demanding(listOfDemands)
         .AssumingNewGoodsCost(1)
         .Build();
      strategy.GenerateGoals(population);

      //Act
      var ActualDemand = strategy.GenerateBidAskSpreads(population);
      var buys = strategy.CreateBuys(population);
      var totalCostOfBuys = buys.Sum(b => b.TradeToSubmit.Price) * buys.Sum(c => c.TradeToSubmit.Quantity);

      //Assert
      Assert.That(totalCostOfBuys, Is.LessThanOrEqualTo(population.GetCash()),
         $"Total cost of buys {totalCostOfBuys} exceeds available cash {population.GetCash()}");
      Debug.Log($"Total cost of buys: {totalCostOfBuys}, Available cash: {population.GetCash()}");
   }
   [Test]
   public void PerformStrategySubmitsOrdersToMarket()
   {
      //Arrange
      const int initialPopulation = 100;
      const float initialEnnui = .99f;

      var demandForLemonade = new DemandData()
      {
         MinDemand = 0,
         MaxDemand = 100
      };
      var listOfDemands = new Dictionary<Good, DemandData>()
            {
              {Lemonade,demandForLemonade}
            };
      var strategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                     .WithAggressionLevel(.5m)
                     .Build();

      var population = EconAgentBuilder.For<PopulationAgent>()
         .Named("Test Population")
         .WithInitialCash(1000)
         .WithEnnui(initialEnnui)
         .WithPopulation(initialPopulation)
         .WithBehaviourStrategy(strategy)
         .Demanding(listOfDemands)
         .Build();
      strategy.GenerateGoals(population);

      //Act
      TestMarket.RegisterMarketParticipant(population);
      strategy.PerformStrategy(population);
      var actualOrders = TestMarket.GetOrdersSentToMarket();

      //Assert
      Assert.That(actualOrders, Is.Not.Empty, "No orders were submitted to the market.");
      Debug.Log($"First order submitted: {actualOrders.FirstOrDefault()} total order value: {actualOrders.FirstOrDefault()?.Price * actualOrders.FirstOrDefault()?.Quantity}");
   }



   #endregion
} 
#endif