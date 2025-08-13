using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class RecordingExecutions
{
    TheEconomy TestEconomy;
    Good Lemon;
    Good Lemonade;
    Market TestMarket;
    int Period;

    EconAgent Company1;
    EconAgent Company2;

    iStrategy TestBehaviourStrategy;
    PopulationAgent TestPopulation;
    readonly PriceBand PriceBand1 = new(.5m, 1.0m);
    readonly PriceBand PriceBand2 = new(5.0m, 10m);

    readonly TestComparer<Execution> ExecutionComparer=new(new string[] { "CounterPartyTrades","RecordedTrade" });

    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    [SetUp]
    public void Setup()
    {
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        TestBehaviourStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                .WithAggressionLevel(.5m)
                .Build();
        
        Lemonade = new GoodBuilder()
                .Named("Lemonade")
                .WithRarity(RarityEnum.Uncommon)
                .Costing(.5m)
                .WhichIsProducedGood()
                .Build();
        var reduceEnnuiEffect = new GoodEffect()
                .Named("Reduce Ennui")
                .DescribedAs("Reduces ennui")
                .Affecting(MetricEnum.Ennui)
                .WithEffectMagnitude(-.4f)
                .WithEffect(new MetricModifier<PopulationAgent>(
                           c => c.Ennui,
                           (c, newValue) => c.Ennui = newValue)
                           );
        Lemonade.AddEffect(reduceEnnuiEffect);
        
        var LemonadeDemand = new DemandData
        {
            MinDemand = 0,
            MaxDemand = 10000
        };

        var ListOfDemands = new Dictionary<Good, DemandData>
        {
            {Lemonade, LemonadeDemand},
        };

        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .WithPopulation(100)
            .WithInitialCash(10000)
            .WithBehaviourStrategy(TestBehaviourStrategy)
            .WithFixedCostStrategy(new BasicFixedCostStrategy())
            .Demanding(ListOfDemands)
            .WithEnnui(.9f)
            .Build();
        TestBehaviourStrategy.GenerateGoals(TestPopulation);

        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
            .WithDemandStrategy(TestDemandStrategy)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithPriceManager(new BasicPriceManager())
            .WithDemographicManager(new MockDemographicManager())
            .WithTransactionManager(new BasicTransactionManager());
        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company 2", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);
        TestMarket.RegisterMarketParticipant(TestPopulation);

        Period=0;

        Lemon = Good.CreateInstance("Lemon", PriceBand1, RarityEnum.Common);
        Lemonade = Good.CreateInstance("Lemonade", PriceBand2, RarityEnum.Uncommon);
    }
    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(TestEconomy);
        Company1 = null;
        Company2 = null;
        TestMarket = null;
        Lemon = null;
        Lemonade = null;
    }

#region Orders get executions added 
    [TestCase(TestName = "Company 1 buys 5 lemonade from Company 2, expect 1 execution for each order")]
    public void ProcessCompanyOrdersRecordsExecutionsInOrders_1P1CP()
    {
        //Arrange
        var Company1SellsLemonadeToAnyone = new Order(Company1, null, Lemonade, 5, 10m);
        var Company2BuysLemonadeFromAnyone = new Order(null, Company2, Lemonade, 5, 10m);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, 5, 10m, 2));
        Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone,TestMarket,Period));
        Company2.QueueOrder(CreateActionContext(Company2BuysLemonadeFromAnyone,TestMarket,Period));
        
        const int expectedOrder1ExecutionCount = 1;
        const int expectedOrder2ExecutionCount = 1;
        var expectedOrder1 = new Execution(Company1SellsLemonadeToAnyone,Company1,Company2,5,10m,Period);
        var expectedOrder2 = new Execution(Company2BuysLemonadeFromAnyone,Company1,Company2,5,10m,Period);

        //Act
        TestMarket.ProcessCompanyOrders();
        var actualOrder1Executions = Company1SellsLemonadeToAnyone.GetExecutions();
        var actualOrder2Executions = Company2BuysLemonadeFromAnyone.GetExecutions();
        
        //Assert
        Assert.AreEqual(expectedOrder1ExecutionCount,actualOrder1Executions?.Count());
        Assert.AreEqual(expectedOrder2ExecutionCount,actualOrder2Executions?.Count());
        Assert.IsTrue(ExecutionComparer.Equals(expectedOrder1,actualOrder1Executions?.FirstOrDefault()));
        Assert.IsTrue(ExecutionComparer.Equals(expectedOrder2,actualOrder2Executions?.FirstOrDefault()));
    }
    [TestCase(TestName = "Company 1 buys 5 lemonade each from Company 2 and Company 3, expect 2 executions for Company 1 and 1 for each seller")]
     public void ProcessCompanyOrdersRecordsExecutionsInOrders_1P2CP()
     {
        //Arrange
        var Company3 = EconAgent.Factory.Create("Company 3",AgentLevelEnum.Beginner);
        var Company1BuysLemonadeFromAnyone = new Order(Company1, null, Lemonade, 10, 10m);
        var Company2SellsLemonadeToAnyone = new Order(null, Company2, Lemonade, 5, 10m);
        var Company3SellsLemonadeToAnyone = new Order(null, Company3, Lemonade, 5, 10m);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, 5, 10m, 2));
        Company3.GetInventory().AddGood(new InventoryEntry(Lemonade, 5, 10m, 2));
        Company1.QueueOrder(CreateActionContext(Company1BuysLemonadeFromAnyone,TestMarket,Period));
        Company2.QueueOrder(CreateActionContext(Company2SellsLemonadeToAnyone,TestMarket,Period));
        Company3.QueueOrder(CreateActionContext(Company3SellsLemonadeToAnyone,TestMarket,Period));

        const int expectedOrder1ExecutionCount = 2;
        const int expectedOrder2ExecutionCount = 1;
        const int expectedOrder3ExecutionCount = 1;
        var expectedCompany1Execution1 = new Execution(Company1BuysLemonadeFromAnyone,Company1,Company2,5,10m,Period);
        var expectedCompany1Execution2 = new Execution(Company1BuysLemonadeFromAnyone,Company1,Company3,5,10m,Period);
        var expectedCompany2Execution1 = new Execution(Company2SellsLemonadeToAnyone,Company1,Company2,5,10m,Period);
        var expectedCompany3Execution1 = new Execution(Company3SellsLemonadeToAnyone,Company1,Company3,5,10m,Period);
        
        //Act
        TestMarket.ProcessCompanyOrders();
        var actualCompany1Executions = Company1BuysLemonadeFromAnyone.GetExecutions();
        var actualCompany2Executions = Company2SellsLemonadeToAnyone.GetExecutions();
        var actualCompany3Executions = Company3SellsLemonadeToAnyone.GetExecutions();
        //Assert
        Assert.AreEqual(expectedOrder1ExecutionCount,actualCompany1Executions?.Count());
        Assert.AreEqual(expectedOrder2ExecutionCount,actualCompany2Executions?.Count());
        Assert.AreEqual(expectedOrder3ExecutionCount,actualCompany3Executions?.Count());

        Assert.IsTrue(ExecutionComparer.Equals(expectedCompany1Execution1,actualCompany1Executions[0]));
        Assert.IsTrue(ExecutionComparer.Equals(expectedCompany1Execution2,actualCompany1Executions[1]));
    
        Assert.IsTrue(ExecutionComparer.Equals(expectedCompany2Execution1,actualCompany2Executions?.FirstOrDefault()));
        Assert.IsTrue(ExecutionComparer.Equals(expectedCompany3Execution1,actualCompany3Executions?.FirstOrDefault()));
     }
     [TestCase(TestName = "Company 1 sells 5 lemonade to anyone, expect 1 execution for Company1")]
     public void FulfillDemandRecordsExecutionsInOrders_1P1CP()
     {
        //Arrange
        var company1Ask = 1m;
        var companyQuantity = 5;
        var company1CostOfLemonade = .5m;
        var acquiredInPeriod = 0;
        var Company1SellsLemonadeToAnyone = new Order(null,Company1 , Lemonade,companyQuantity, company1Ask);
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, companyQuantity, company1CostOfLemonade, acquiredInPeriod));
        Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone,TestMarket,Period));

        const int expectedCompany1OrderExecutionCount = 1;
        var expectedCompany1Execution = new Execution(Company1SellsLemonadeToAnyone,TestPopulation,Company1,companyQuantity,company1Ask,Period);

        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actualCompany1Executions = Company1SellsLemonadeToAnyone.GetExecutions();
        //Note: no market executions can be querried at this time -- the order is created during fulfill demand

        //Assert
        Assert.AreEqual(expectedCompany1OrderExecutionCount,actualCompany1Executions?.Count());
        Assert.IsTrue(ExecutionComparer.Equals(expectedCompany1Execution,actualCompany1Executions?.FirstOrDefault()));
     }
#endregion

#region Execution Tests From ProcessCompanyOrders
   

    [TestCase(TestName = "Company trades only: Company 1 sells Lemonade to Company2, expect 2 executions")]
    public void MarketExecutionsContainsPartyAndCounterParty_OnePair()
    {
        //Arrange
        var Company1SellsLemonadeToAnyone = new Order(Company1, null, Lemonade, 5, 10m);
        var Company2BuysLemonadeFromAnyone = new Order(null, Company2, Lemonade, 5, 10m);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, 5, 10m, 2));

        const int expectedExecutionCount = 2;
        var expectedExecutions = new List<Execution>
        {
            new(Company1SellsLemonadeToAnyone,Company1,Company2,5,10,Period),
            new(Company2BuysLemonadeFromAnyone,Company1,Company2,5,10,Period)
        };
        Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone,TestMarket,Period));
        Company2.QueueOrder(CreateActionContext(Company2BuysLemonadeFromAnyone,TestMarket,Period));
        //Act
        TestMarket.ProcessCompanyOrders();
        var actualExecutions = TestMarket.GetExecutionsInPeriod(Period);
        //Assert
        Assert.AreEqual(expectedExecutionCount,actualExecutions.Count);
        var comparer = new TestComparer<Execution>(new string[] { "CounterPartyTrades" });
        // for(var i = 0; i < expectedExecutions.Count; i++)
        // {
        //     Assert.IsTrue(comparer.Equals(expectedExecutions[i], actualExecutions[i]));
        // }
        Assert.IsTrue(comparer.ListsAreEquivalent(expectedExecutions, actualExecutions,comparer));
    }
    [TestCase(TestName = "Company trades only: One Buyer, Two Sellers, fully filled. 4 executions expected")]
    public void OneBuyerTwoSellersFullyFilled()
    {
        //Arrange
        var Company3 = EconAgent.Factory.Create("Company 3",AgentLevelEnum.Beginner);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        Company3.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));

        var Company1BuysFromAnyone = new Order(Company1, null, Lemon, 2, 1m);
        var Company2SellsToAnyone = new Order(null,Company2,Lemon,1,1m);
        var Company3SellsToAnyone = new Order(null,Company3,Lemon,1,1m);

        const int expectedExecutionCount = 4;
        var expectedExecutions = new List<Execution>
        {
            new(Company1BuysFromAnyone,Company1,Company2,1,1,Period),
            new(Company1BuysFromAnyone,Company1,Company3,1,1,Period),
            new(Company2SellsToAnyone,Company1,Company2,1,1,Period),
            new(Company3SellsToAnyone,Company1,Company3,1,1,Period)
        };
        Company1.QueueOrder(CreateActionContext(Company1BuysFromAnyone,TestMarket,Period));
        Company2.QueueOrder(CreateActionContext(Company2SellsToAnyone,TestMarket,Period));
        Company3.QueueOrder(CreateActionContext(Company3SellsToAnyone,TestMarket,Period));

        //Act
        TestMarket.ProcessCompanyOrders();
        var actualExecutions = TestMarket.GetExecutionsInPeriod(Period);
        //Assert
        Assert.AreEqual(expectedExecutionCount,actualExecutions.Count);
        var comparer = new TestComparer<Execution>(new string[] { "CounterPartyTrades" });
        Assert.IsTrue(comparer.ListsAreEquivalent(expectedExecutions, actualExecutions,comparer));
    }
    [TestCase(TestName = "Single trade, Population is buyer counterparty. Expected executions: 2")]
    public void OneTradeMarketBuyerCounterparty()
    {
        //Arrange
        var company1Quantity = 5;
        var company1Ask = 3m;
        var Company1SellsToAnyone = new Order( null,Company1, Lemonade, company1Quantity, company1Ask);
        var TestPopulationBuysFromCompany1 = new Order(TestPopulation,Company1, Lemon, company1Quantity, company1Ask){
            SubmittingCompany = TestPopulation,
            FilledQuantity = 5
        };

        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, company1Quantity, company1Ask,Period));
        
        const int expectedExecutionCount = 2;
        var expectedExecutions = new List<Execution>
        {
            new(Company1SellsToAnyone,TestPopulation,Company1,company1Quantity,company1Ask,Period),
            new(TestPopulationBuysFromCompany1,TestPopulation,Company1,company1Quantity,company1Ask,Period)
        };
        Company1.QueueOrder(CreateActionContext(Company1SellsToAnyone,TestMarket,Period));
        
        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actualExecutions = TestMarket.GetExecutionsInPeriod(Period);
        //Assert
        Assert.AreEqual(expectedExecutionCount,actualExecutions.Count);
        Assert.IsTrue(ExecutionComparer.ListsAreEquivalent(expectedExecutions, actualExecutions,ExecutionComparer));
    }
    [TestCase(TestName = "Company1 sells 4000 Lemonade, Company2 buys 10, Company 3 buys 10, population buys 2216. Expected executions: 6")]
    public void BigSell2CompanyCounterParty1MarketCounterparty()
    {
        var company1Quantity = 4000;
        var company2Quantity = 10;
        var company3Quantity = 10;
        var company1Ask = 3m;
        const int expectedPopulationBuys = 2216;//Note: This will change if you change population parameters.
        //Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, company1Quantity, company1Ask, Period));

        var Company3 = EconAgent.Factory.Create("Company 3",AgentLevelEnum.Beginner);
        Company3.SetCash(1000);
        var Company1SellsToAnyone = new Order(null,Company1, Lemonade, company1Quantity, company1Ask);
        var Company2BuysFromAnyone = new Order(Company2, null, Lemonade, company2Quantity, company1Ask);
        var Company3BuysFromAnyone = new Order(Company3, null, Lemonade, company3Quantity, company1Ask);
        const int expectedExecutionCount = 6;
        var PopulationBuysFromCompany1 = new Order(TestPopulation,Company1, Lemonade, 30, company1Ask)
        {
            SubmittingCompany = TestPopulation,
            FilledQuantity = 30
        };
        var expectedExecutions = new List<Execution>
        {
            new(Company1SellsToAnyone,Company2,Company1,10,company1Ask,Period),
            new(Company1SellsToAnyone,Company3,Company1,10,company1Ask,Period),
            new(Company1SellsToAnyone,TestPopulation,Company1,expectedPopulationBuys,company1Ask,Period),
            new(Company2BuysFromAnyone,Company2,Company1,10,company1Ask,Period),
            new(Company3BuysFromAnyone,Company3,Company1,10,company1Ask,Period),
            new(PopulationBuysFromCompany1,TestPopulation,Company1,expectedPopulationBuys,company1Ask,Period)
        };
        Company1.QueueOrder(CreateActionContext(Company1SellsToAnyone,TestMarket,Period));
        Company2.QueueOrder(CreateActionContext(Company2BuysFromAnyone,TestMarket,Period));
        Company3.QueueOrder(CreateActionContext(Company3BuysFromAnyone,TestMarket,Period));
        //Act
        TestMarket.StartTradingPeriod();
        TestMarket.ProcessCompanyOrders();
        var actualExecutions = TestMarket.GetExecutionsInPeriod(Period);
        //Assert
        Assert.AreEqual(expectedExecutionCount,actualExecutions.Count);
        Assert.IsTrue(ExecutionComparer.ListsAreEquivalent(expectedExecutions, actualExecutions,ExecutionComparer));
    }
#endregion
}

