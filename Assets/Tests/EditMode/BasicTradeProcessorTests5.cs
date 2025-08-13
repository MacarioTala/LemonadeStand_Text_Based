using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static TestHelpers;

//This partial class exercises the path from ExecuteBestTradesForGood through to
//the transaction manager and recording the orders and executions in the market
public partial class BasicTradeProcessorTests
{
    readonly TestComparer<Execution> ExecutionComparer=new(new string[] { "CounterPartyTrades" });

    [TestCase(TestName="ExecuteBestTradesForGood: One Buyer, One Seller, both fill with executions recorded")]
    public void EBTG_OneBuyerOneSeller_BothExecutionsRecorded()
    {
        //Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, 5,9m,Period));
        var Company1BuysLemonadeFromAny = new Order(Company1,null,Lemonade,5,10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = Company1BuysLemonadeFromAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        var Company2SellsLemonadeToAny = new Order(null,Company2,Lemonade,5,10m);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = Company2SellsLemonadeToAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        Company2.QueueOrder(Company2Context);
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        //Act
        TestTradeProcessor.ExecuteBestTradesForGood(Lemonade,TestMarket,OrdersSentToMarket);
        //Assert
        var transactionsRecorded = TestMarket.GetExecutionsInPeriod(Period);
        var recordedTransaction = transactionsRecorded?.FirstOrDefault();
        Assert.IsNotNull(recordedTransaction);
        Assert.IsTrue(transactionsRecorded.Count == 2);
        Assert.AreEqual(Company1,recordedTransaction.Buyer);
        Assert.AreEqual(Company2,recordedTransaction.Seller);
        Assert.AreEqual(Lemonade,recordedTransaction.RecordedTrade.Good);
        Assert.AreEqual(5,recordedTransaction.Quantity);
        Assert.AreEqual(10m,recordedTransaction.Price);

    }
     
    [TestCase(TestName="One Buyer, Multiple Sellers, both sellers fill. 4 executions recorded")]
    public void EBTG_OneBuyerMultSellerBothSellersFill_4ExecutionsRecorded()
    {
        //Arrange
        var Company3 = EconAgent.Factory.Create("Company 3", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company3);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, 3,9m,Period));
        Company3.GetInventory().AddGood(new InventoryEntry(Lemonade, 2,9m,Period));

        var Company1BuysLemonadeFromAny = new Order(Company1,null,Lemonade,5,10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = Company1BuysLemonadeFromAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        var Company2SellsLemonadeToAny = new Order(null,Company2,Lemonade,3,10m);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = Company2SellsLemonadeToAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        var Company3SellsLemonadeToAny = new Order(null,Company3,Lemonade,2,10m);
        var Company3Context = new ActionContext
        {
            TradeToSubmit = Company3SellsLemonadeToAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        Company2.QueueOrder(Company2Context);
        Company3.QueueOrder(Company3Context);
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();

        var ExpectedExecutions = new List<Execution>
        {
            new(Company2SellsLemonadeToAny,Company1,Company2,3,10m,Period),
            new(Company3SellsLemonadeToAny,Company1,Company3,2,10m,Period),
            new(Company1BuysLemonadeFromAny,Company1,Company2,3,10m,Period),
            new(Company1BuysLemonadeFromAny,Company1,Company3,2,10m,Period),
        };

        //Act
        TestTradeProcessor.ExecuteBestTradesForGood(Lemonade,TestMarket,OrdersSentToMarket);
        //Assert
        var ActualExecutions = TestMarket.GetExecutionsInPeriod(Period);
        Assert.IsTrue(ActualExecutions.Count == 4);
        Assert.IsTrue(ExecutionComparer.ListsAreEquivalent(ExpectedExecutions,ActualExecutions,ExecutionComparer));
    }

    [TestCase(TestName ="ExecuteBestTradesForGood: One Buyer, Multiple Sellers, Low Price seller fills")]
    public void EBTFG_OneBuyerMultSellerOneSellerFills()
    {
        //Arrange
        var Company3 = EconAgent.Factory.Create("Company 3", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company3);
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, 5,9m,Period));
        Company3.GetInventory().AddGood(new InventoryEntry(Lemonade, 2,10m,Period));
        var Company1BuysLemonadeFromAny = new Order(Company1,null,Lemonade,5,10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = Company1BuysLemonadeFromAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        var Company2SellsLemonadeToAny = new Order(null,Company2,Lemonade,5,10m);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = Company2SellsLemonadeToAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        var Company3SellsLemonadeToAny = new Order(null,Company3,Lemonade,2,10m);
        var Company3Context = new ActionContext
        {
            TradeToSubmit = Company3SellsLemonadeToAny,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        Company2.QueueOrder(Company2Context);
        Company3.QueueOrder(Company3Context);
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        var ExpectedExecutions = new List<Execution>
        {
            new(Company2SellsLemonadeToAny,Company1,Company2,5,10m,Period),
            new(Company1BuysLemonadeFromAny,Company1,Company2,5,10m,Period)
        };
        //Act
        TestTradeProcessor.ExecuteBestTradesForGood(Lemonade,TestMarket,OrdersSentToMarket);
        //Assert
        var ActualExecutions = TestMarket.GetExecutionsInPeriod(Period);
        Assert.IsTrue(ActualExecutions.Count == 2); //Note: Company 3's order might be processed
                                                        //downstream, during market trade processing
        Assert.IsTrue(ExecutionComparer.ListsAreEquivalent(ExpectedExecutions,ActualExecutions,ExecutionComparer));
    }

    

}