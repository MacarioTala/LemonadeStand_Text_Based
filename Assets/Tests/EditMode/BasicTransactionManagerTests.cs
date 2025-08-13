using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class BasicTransactionManagerTests
{
    TheEconomy TestEconomy;
    Good Lemon;
    Good Lemonade;
    Market TestMarket;
    int Period;

    EconAgent Company1;
    EconAgent Company2;
    readonly PriceBand PriceBand1 = new(.5m, 1.0m);
    readonly PriceBand PriceBand2 = new(5.0m, 10m);

    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    readonly TestComparer<Execution> ExecutionComparer = new(new string[] { "CounterPartyTrades" });
    [SetUp]
    public void Setup()
    {
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
            .WithDemandStrategy(TestDemandStrategy)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithTransactionManager(new BasicTransactionManager())
            .WithDemographicManager(new BasicDemographicManager())
            .WithCash(10000);
        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company 2", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);

        Period=0;

        Lemon = Good.CreateInstance("Lemon", PriceBand1, RarityEnum.Common);
        Lemonade = Good.CreateInstance("Lemonade", PriceBand2, RarityEnum.Uncommon);
    }
#region ValidateTransactionTests
    [Test]
    public void ValidateTransactionShouldReturnSelfTradeWhenCompanySubmitsTwoIdenticalTrades()
    {
        //Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        var order = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company1
        };
        var costOfThisLeg = 1m;

        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.SelfTrade, "Company cannot trade with itself");
        var transactionManager = new BasicTransactionManager();
        //Act
        var actual = transactionManager.ValidateTransaction(order, counterPartyOrder, costOfThisLeg);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }

    [Test]
    public void ValidateTransactionShouldReturnSelfTradeWhenCompanyTriesToTradeWithItself()
    {
        //Arrange
        var order = new Order(Company1,Company1,Lemon,1,1m)
        {
            SubmittingCompany = Company1
        };
        
        var costOfThisLeg = 1m;

        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.SelfTrade, "Company cannot trade with itself");
        var transactionManager = new BasicTransactionManager();
        //Act
        var actual = transactionManager.ValidateTransaction(order, order, costOfThisLeg);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }

    [Test]
    public void ValidateTransactionReturnsSuccessForValidOrderPair()
    {
        //Arrange
        Company1.SetCash(1000m);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        var order = new Order(Company1, Company2, Lemon, 1, 1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var costOfThisLeg = 1m;
        var expected = LemonadeStandResultObject.Success();
        var transactionManager = new BasicTransactionManager();
        //Act
        var actual = transactionManager.ValidateTransaction(order, counterPartyOrder, costOfThisLeg);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }
    
    [Test]
    public void ValidateTransactionReturnsInsufficientFundsWhenBuyerDoesNotHaveEnoughCash()
    {
        //Arrange
        Company1.SetCash(0m);
        var order = new Order(Company1, Company2, Lemon, 1, 1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var costOfThisLeg = 1m;
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.InsufficientCash, "");
        var transactionManager = new BasicTransactionManager();
        //Act
        var actual = transactionManager.ValidateTransaction(order, counterPartyOrder, costOfThisLeg);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }

    [Test]
    public void ValidateTransactionReturnsInsufficientGoodsWhenSellerDoesNotHaveEnoughGoods()
    {
        //Arrange
        Company1.SetCash(1000m);
        var order = new Order(Company1, Company2, Lemon, 1, 1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var costOfThisLeg = 1m;
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.InsufficientGoods, "Seller does not have enough goods to complete the transaction");
        var transactionManager = new BasicTransactionManager();
        //Act
        var actual = transactionManager.ValidateTransaction(order, counterPartyOrder, costOfThisLeg);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }
#endregion
#region ProcessTransactionTests
    [Test]
    public void ForFullyFilledTradesProcessTransactionShouldReturnSuccess()
    {
        //Arrange
        Company1.SetCash(1000m);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        var order = new Order(Company1, Company2, Lemon, 1, 1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var expected = LemonadeStandResultObject.Success();
        var transactionManager = new BasicTransactionManager();
        //Act
        var actual = transactionManager.ProcessTransactionPair(order, counterPartyOrder, Period);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }
    [Test]
    public void FullyFilledTradesTransferCashAndGoodsCorrectly()
    {
        //Arrange
        Company1.SetCash(1000m);
        Company2.SetCash(0);
        Company1.GetInventory().Clear();
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        var order = new Order(Company1, Company2, Lemon, 1, 1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var transactionManager = new BasicTransactionManager();
        var expectedBuyerCash = 999m;
        var expectedSellerCash = 1m;
        var expectedBuyerLemonQuantity = 1;
        var expectedSellerLemonQuantity = 0;
        //Act
        transactionManager.ProcessTransactionPair(order, counterPartyOrder, Period);
        var actualBuyerCash = Company1.GetCash();
        var actualSellerCash = Company2.GetCash();
        var actualBuyerLemonQuantity = Company1.GetInventory()
                                    .GetInventoryEntriesByGood(Lemon.GoodName)
                                    ?.FirstOrDefault()?.quantity ?? 0;
        var actualSellerLemonQuantity = Company2.GetInventory()
                                    .GetInventoryEntriesByGood(Lemon.GoodName)
                                    ?.FirstOrDefault()?.quantity ?? 0;
        //Assert
        Assert.AreEqual(expectedBuyerCash, actualBuyerCash, "Buyer cash not as expected");
        Assert.AreEqual(expectedSellerCash, actualSellerCash, "Seller cash not as expected");
        Assert.AreEqual(expectedBuyerLemonQuantity, actualBuyerLemonQuantity, "Buyer lemon quantity not as expected");
        Assert.AreEqual(expectedSellerLemonQuantity, actualSellerLemonQuantity, "Seller lemon quantity not as expected");
    }
    [Test]
    public void PartiallyFilledTradesTransferCashAndGoodsCorrectly()
    {
        //Arrange
        Company1.SetCash(1000m);
        Company2.SetCash(0);
        Company1.GetInventory().Clear();
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        var order = new Order(Company1, Company2, Lemon, 2, 1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var transactionManager = new BasicTransactionManager();
        var expectedBuyerCash = 999m;
        var expectedSellerCash = 1m;
        var expectedBuyerLemonQuantity = 1;
        var expectedSellerLemonQuantity = 0;
        var expectedBuyerOrderFullyFilledStatus = false;
        var expectedBuyerOrderPartiallyFilledStatus = true;
        //Act
        transactionManager.ProcessTransactionPair(order, counterPartyOrder, Period);
        var actualBuyerCash = Company1.GetCash();
        var actualSellerCash = Company2.GetCash();
        var actualBuyerLemonQuantity = Company1.GetInventory()
                                    .GetInventoryEntriesByGood(Lemon.GoodName)
                                    ?.FirstOrDefault()?.quantity ?? 0;
        var actualSellerLemonQuantity = Company2.GetInventory()
                                    .GetInventoryEntriesByGood(Lemon.GoodName)
                                    ?.FirstOrDefault()?.quantity ?? 0;
        var actualBuyerOrderFullyFilledStatus = order.IsFullyFilled;
        var actualBuyerOrderPartiallyFilledStatus = order.IsPartiallyFilled;
        //Assert
        Assert.AreEqual(expectedBuyerCash, actualBuyerCash, "Buyer cash not as expected");
        Assert.AreEqual(expectedSellerCash, actualSellerCash, "Seller cash not as expected");
        Assert.AreEqual(expectedBuyerLemonQuantity, actualBuyerLemonQuantity, "Buyer lemon quantity not as expected");
        Assert.AreEqual(expectedSellerLemonQuantity, actualSellerLemonQuantity, "Seller lemon quantity not as expected");
        Assert.AreEqual(expectedBuyerOrderFullyFilledStatus, actualBuyerOrderFullyFilledStatus, "Buyer order fully filled status not as expected");
        Assert.AreEqual(expectedBuyerOrderPartiallyFilledStatus, actualBuyerOrderPartiallyFilledStatus, "Buyer order partially filled status not as expected");
    }
    [TestCase(TestName = "ProcessTransaction:One Buyer, two sellers. Expected: 3 executions")]
    public void ProcessTransactionRecordsTwoCounterPartiesWhenTwoCounterPartiesArePresent()
    {
        //Arrange
        var basicTransactionManager = new BasicTransactionManager();
        var Company3 = EconAgent.Factory.Create("Company 3",AgentLevelEnum.Beginner);
        
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        Company3.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        var Company1Buys2LemonFromMultiple = new Order(Company1, Company2, Lemon, 2, 1m)
        {
            SubmittingCompany = Company1
        };
        var counterPartyOrders = new List<Order>();
        var company2Sells1LemonToCompany1 = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var company3Sells1LemonToCompany1 = new Order(Company1,Company3,Lemon,1,1m)
        {
            SubmittingCompany = Company3
        };
        counterPartyOrders.Add(company2Sells1LemonToCompany1);
        counterPartyOrders.Add(company3Sells1LemonToCompany1);

        var orderContext = new ActionContext{PrimaryOrder = Company1Buys2LemonFromMultiple
            ,CounterPartyOrders=counterPartyOrders,
            MarketToSubmitTo = TestMarket,
            Period = Period};

        var expectedExecutions = new List<Execution>()
        {
            new(Company1Buys2LemonFromMultiple, Company1, Company2, 1, 1m, Period),
            new(Company1Buys2LemonFromMultiple, Company1, Company3, 1, 1m, Period),
            new(company2Sells1LemonToCompany1, Company1, Company2, 1, 1m, Period),
            new(company3Sells1LemonToCompany1, Company1, Company3, 1, 1m, Period)
        };
        //Act
        basicTransactionManager.ProcessPairedOrders(orderContext);
        var actualExecutions = TestMarket.GetExecutionsInPeriod(Period);
        var actualCounterPartyOrders = TestMarket.GetExecutionsInPeriod(Period);
        //Assert
        Assert.AreEqual(4, actualCounterPartyOrders.Count);
        Assert.IsTrue(ExecutionComparer.ListsAreEquivalent(expectedExecutions, actualCounterPartyOrders,ExecutionComparer));
    }
#endregion
#region ProcessMarketTransactionTests
    [Test]
    public void ProcessMarketTransactionShouldReturnSuccessForValidOrderPair()
    {
        //Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(Lemon, 1, 1m, 1));
        var Company1SellsLemonsToAnyone = new Order(Company1, null, Lemon, 1, 1m)
        {
            SubmittingCompany = Company1
        };
        var marketGeneratedCounterPartyOrder = new Order(Company1,Company2,Lemon,1,1m)
        {
            SubmittingCompany = Company2
        };
        var context = new ActionContext
        {
            PrimaryOrder = Company1SellsLemonsToAnyone,
            CounterPartyOrders = new List<Order>{marketGeneratedCounterPartyOrder},
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        var expected = LemonadeStandResultObject.Success();
        var transactionManager = new BasicTransactionManager();
        //Act
        var actual = transactionManager.ProcessMarketTransaction(context);
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }
#endregion

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy.gameObject);
        TestMarket = null;
        Lemon = null;
        Company1 = null;
        Company2 = null;
    }
}