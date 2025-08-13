using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;

[TestFixture]
//This partial class tests the ProcessCompanyOrders method in BasicTradeProcessor
public partial class BasicTradeProcessorTests
{
    TheEconomy TestEconomy;
    int Period = 0;
    Market TestMarket;
    BasicTradeProcessor TestTradeProcessor;
    iDemandStrategy TestDemandStrategy;

    Good Lemonade;
    Good RadioactiveLemonade;

    Good Lemon;
    Good Radium;

    EconAgent Company1;
    EconAgent Company2;

    [SetUp]
    public void SetUp()
    {
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company 2", AgentLevelEnum.Beginner);

        TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        TestTradeProcessor = new BasicTradeProcessor();

        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
            .WithDemandStrategy(TestDemandStrategy)
            .WithTradeProcessor(TestTradeProcessor)
            .WithTransactionManager(new BasicTransactionManager())
            .WithCash(1000000);

        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);
        
        Lemon = Good.CreateInstance("Lemons", new PriceBand(1, 3), RarityEnum.Common);
        Lemonade = Good.CreateInstance("Lemonade", new PriceBand(1, 3), RarityEnum.Uncommon);
        RadioactiveLemonade = Good.CreateInstance("Radioactive Lemonade", new PriceBand(10, 20), RarityEnum.Very_Rare);
        Radium = Good.CreateInstance("Radium", new PriceBand(10, 20), RarityEnum.Very_Rare);
    }
    
#region ProcessCompanyOrders Tests
    [Test]
    public void ProcessCompanyOrdersIgnoresOrdersWhereSellerIsMarket()
    {
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1m, Period));
        var testOrder = new Order(TestMarket, Company1, Lemonade, 100, 10m);
        var testContext = new ActionContext{
                    TradeToSubmit = testOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period};
        var expected=0;
        // Act
        Company1.QueueOrder(testContext);
        TestMarket.ProcessCompanyOrders();
        var actual = testOrder.FilledQuantity;
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void PCO_PerfectMatch_OneGood()
    {
        // Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1m, Period));

        var testOrder = new Order(Company1,null, Lemonade, 10, 10m);
        var testOrder2 = new Order(null,Company2, Lemonade, 10, 10m);
        var testContext = new ActionContext{
                    TradeToSubmit = testOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var testContext2 = new ActionContext{
                    TradeToSubmit = testOrder2,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var expectedLemonadeFill=10;
        Company1.QueueOrder(testContext);
        Company2.QueueOrder(testContext2);
        // Act
        TestTradeProcessor.ProcessCompanyOrders(TestMarket);
        var actualLemonadeFill = testOrder.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedLemonadeFill, actualLemonadeFill);
    }

    [Test]
    public void PCO_TwoBuyers_NoFill_NoException()
    {
        // Arrange
        var testOrder = new Order(Company1,null, Lemonade, 10, 10m);
        var testOrder2 = new Order(Company2, null, Lemonade, 10, 10m);
        var testContext = new ActionContext{
                    TradeToSubmit = testOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var testContext2 = new ActionContext{
                    TradeToSubmit = testOrder2,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var expectedCompany1Fill=0;
        var expectedCompany2Fill=0;
        Company1.QueueOrder(testContext);
        Company2.QueueOrder(testContext2);
        // Act
        TestTradeProcessor.ProcessCompanyOrders(TestMarket);
        var actualCompany1Fill = testOrder.FilledQuantity;
        var actualCompany2Fill = testOrder2.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedCompany1Fill, actualCompany1Fill);
        Assert.AreEqual(expectedCompany2Fill, actualCompany2Fill);
    }

    [Test]
    public void PCO_Mismatch_NoFill_NoException()
    {
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1m, Period));
        Company1.GetInventory().AddGood(new InventoryEntry(Radium, 10, 1m, Period));
        Company2.GetInventory().AddGood(new InventoryEntry(Lemon, 10, 1m, Period));
        var testOrder = new Order(Company1,null, Lemonade, 10, 10m);
        var testOrder2 = new Order(Company2, null, Lemonade, 10, 10m);
        var testOrder3 = new Order(null,Company1, Radium, 10, 10m);
        var testOrder4 = new Order(null,Company2 , Lemon, 10, 10m);

        var expectedResults = new (int ExpectedFill, Order Order)[]
        {
            (0, testOrder),
            (0, testOrder2),
            (0, testOrder3),
            (0, testOrder4)
        };
        var expectedOrderStatus= LemonadeStandResultObject.Failure(ResultTypeEnum.NoMatchingCounterParties,"").Result;

        Company1.QueueOrder(TestHelpers.CreateActionContext(testOrder, TestMarket, Period));
        Company2.QueueOrder(TestHelpers.CreateActionContext(testOrder2, TestMarket, Period));
        Company1.QueueOrder(TestHelpers.CreateActionContext(testOrder3, TestMarket, Period));
        Company2.QueueOrder(TestHelpers.CreateActionContext(testOrder4, TestMarket, Period));
        // Act
        var PCOResult =TestTradeProcessor.ProcessCompanyOrders(TestMarket);
        
        // Assert
        foreach (var (ExpectedFill, Order) in expectedResults)
        {
            Assert.AreEqual(ExpectedFill, 
                            Order.FilledQuantity, 
                            $"Expected {ExpectedFill} for {Order} but got {Order.FilledQuantity}");
            Assert.AreEqual(expectedOrderStatus, 
                            Order.OrderStatus.Result, 
                            $"Expected {expectedOrderStatus} for {Order} but got {Order.OrderStatus}");
        }
        Assert.IsEmpty(PCOResult,$"Expected no trades to be executed,but got {PCOResult.Count}");
    }

    [Test]
    public void PCO_PerfectMatch_TwoGoods()
    {
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1m, Period));
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 1m, Period));

        var company1RadioactiveLemonadeBuyOrder = new Order(Company1,null, RadioactiveLemonade, 10, 10m);
        var company2LemonadeBuyOrder = new Order(Company2,null, Lemonade, 100, 10m);
        var company1LemonadeSellOrder = new Order(null,Company1, Lemonade, 100, 10m);
        var company2RadioactiveLemonadeOrderSell = new Order(null,Company2, RadioactiveLemonade, 10, 10m);

        var company1RadioactiveBuyLemonadeContext = new ActionContext{
                    TradeToSubmit = company1RadioactiveLemonadeBuyOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company2LemonadeBuyContext = new ActionContext{
                    TradeToSubmit = company2LemonadeBuyOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company1LemonadeSellContext = new ActionContext{
                    TradeToSubmit = company1LemonadeSellOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company2RadioactiveSellLemonadeContext = new ActionContext{
                    TradeToSubmit = company2RadioactiveLemonadeOrderSell,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        Company1.QueueOrder(company1RadioactiveBuyLemonadeContext);
        Company2.QueueOrder(company2LemonadeBuyContext);
        Company1.QueueOrder(company1LemonadeSellContext);
        Company2.QueueOrder(company2RadioactiveSellLemonadeContext);
        var expectedCompany1RadioactiveLemonadeBuyFill=10;
        var expectedCompany2LemonadeBuyFill=100;
        var expectedCompany1LemonadeSellFill=100;
        var expectedCompany2RadioactiveLemonadeSellFill=10;
        // Act
        TestTradeProcessor.ProcessCompanyOrders(TestMarket);
        var actualCompany1RadioactiveBuyLemonadeFill = company1RadioactiveLemonadeBuyOrder.FilledQuantity;
        var actualCompany2LemonadeBuyFill = company2LemonadeBuyOrder.FilledQuantity;
        var actualCompany1LemonadeSellFill = company1LemonadeSellOrder.FilledQuantity;
        var actualCompany2RadioactiveSellLemonadeFill = company2RadioactiveLemonadeOrderSell.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedCompany1RadioactiveLemonadeBuyFill, 
                        actualCompany1RadioactiveBuyLemonadeFill,
                        $"Radioactive Lemonade Buy not filled correctly");
        Assert.AreEqual(expectedCompany2LemonadeBuyFill, 
                        actualCompany2LemonadeBuyFill,
                        $"Lemonade Buy not filled correctly");
        Assert.AreEqual(expectedCompany1LemonadeSellFill, 
                        actualCompany1LemonadeSellFill,
                        $"Lemonade Sell not filled correctly");
        Assert.AreEqual(expectedCompany2RadioactiveLemonadeSellFill, 
                        actualCompany2RadioactiveSellLemonadeFill,
                        $"Radioactive Lemonade Sell not filled correctly");
    }

    [Test]
    public void PCO_ThreeBuyersTwoSellersTwoGoods_allFill()
    {
        // Arrange
        var Company3 = EconAgent.Factory.Create("Company 3", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company3);

        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1m, Period));
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 1m, Period));
        Company3.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1m, Period));

        var company1RadioactiveLemonadeBuyOrder = new Order(Company1,null, RadioactiveLemonade, 5, 10m);
        var company2LemonadeBuyOrder = new Order(Company2,null, Lemonade, 50, 10m);
        var company3LemonadeBuyOrder = new Order(Company3,null, Lemonade, 50, 10m);
        var company1LemonadeSellOrder = new Order(null,Company1, Lemonade, 100, 10m);
        var company2RadioactiveLemonadeOrderSell = new Order(null,Company2, RadioactiveLemonade, 10, 10m);

        var company1RadioactiveBuyLemonadeContext = new ActionContext{
                    TradeToSubmit = company1RadioactiveLemonadeBuyOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company2LemonadeBuyContext = new ActionContext{
                    TradeToSubmit = company2LemonadeBuyOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company3LemonadeBuyContext = new ActionContext{
                    TradeToSubmit = company3LemonadeBuyOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company1LemonadeSellContext = new ActionContext{
                    TradeToSubmit = company1LemonadeSellOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company2RadioactiveSellLemonadeContext = new ActionContext{
                    TradeToSubmit = company2RadioactiveLemonadeOrderSell,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
    
        Company1.QueueOrder(company1RadioactiveBuyLemonadeContext);
        Company2.QueueOrder(company2LemonadeBuyContext);
        Company3.QueueOrder(company3LemonadeBuyContext);
        Company1.QueueOrder(company1LemonadeSellContext);
        Company2.QueueOrder(company2RadioactiveSellLemonadeContext);
        var expectedCompany1RadioactiveLemonadeBuyFill=5;
        var expectedCompany2LemonadeBuyFill=50;
        var expectedCompany3LemonadeBuyFill=50;
        var expectedCompany1LemonadeSellFill=100;
        var expectedCompany2RadioactiveLemonadeSellFill=5;
        // Act
        TestTradeProcessor.ProcessCompanyOrders(TestMarket);
        var actualCompany1RadioactiveBuyLemonadeFill = company1RadioactiveLemonadeBuyOrder.FilledQuantity;
        var actualCompany2LemonadeBuyFill = company2LemonadeBuyOrder.FilledQuantity;
        var actualCompany3LemonadeBuyFill = company3LemonadeBuyOrder.FilledQuantity;
        var actualCompany1LemonadeSellFill = company1LemonadeSellOrder.FilledQuantity;
        var actualCompany2RadioactiveSellLemonadeFill = company2RadioactiveLemonadeOrderSell.FilledQuantity;
        // Assert
        Assert.AreEqual( expectedCompany1RadioactiveLemonadeBuyFill
                        ,actualCompany1RadioactiveBuyLemonadeFill
                        ,$"Expected Company 1 to fill buy of {expectedCompany1RadioactiveLemonadeBuyFill}"+
                        $" but got {actualCompany1RadioactiveBuyLemonadeFill}");
        Assert.AreEqual( expectedCompany2LemonadeBuyFill
                        ,actualCompany2LemonadeBuyFill
                        ,$"Expected Company 2 to fill buy of {expectedCompany2LemonadeBuyFill}"+
                        $" but got {actualCompany2LemonadeBuyFill}");
        Assert.AreEqual( expectedCompany3LemonadeBuyFill
                        ,actualCompany3LemonadeBuyFill
                        ,$"Expected Company 3 to fill buy of {expectedCompany3LemonadeBuyFill}"+
                        $" but got {actualCompany3LemonadeBuyFill}");
        Assert.AreEqual( expectedCompany1LemonadeSellFill
                        ,actualCompany1LemonadeSellFill
                        ,$"Expected Company 1 to fill sell of {expectedCompany1LemonadeSellFill}"+
                        $" but got {actualCompany1LemonadeSellFill}");
        Assert.AreEqual( expectedCompany2RadioactiveLemonadeSellFill
                        ,actualCompany2RadioactiveSellLemonadeFill
                        ,$"Expected Company 2 to fill sell of {expectedCompany2RadioactiveLemonadeSellFill}"+
                        $" but got {actualCompany2RadioactiveSellLemonadeFill}");
    }
    [Test]
    public void PCO_2Companies2Buy2SellAllPartial()
    {
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1m, Period));
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 1m, Period));

        var company1RadioactiveLemonadeBuyOrder = new Order(Company1,null, RadioactiveLemonade, 5, 10m);
        var company2LemonadeBuyOrder = new Order(Company2,null, Lemonade, 50, 10m);
        var company1LemonadeSellOrder = new Order(null,Company1, Lemonade, 100, 10m);
        var company2RadioactiveLemonadeOrderSell = new Order(null,Company2, RadioactiveLemonade, 10, 10m);

        var company1RadioactiveBuyLemonadeContext = new ActionContext{
                    TradeToSubmit = company1RadioactiveLemonadeBuyOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company2LemonadeBuyContext = new ActionContext{
                    TradeToSubmit = company2LemonadeBuyOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company1LemonadeSellContext = new ActionContext{
                    TradeToSubmit = company1LemonadeSellOrder,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        var company2RadioactiveSellLemonadeContext = new ActionContext{
                    TradeToSubmit = company2RadioactiveLemonadeOrderSell,
                    MarketToSubmitTo = TestMarket,
                    Period = Period
                                            };
        Company1.QueueOrder(company1RadioactiveBuyLemonadeContext);
        Company1.QueueOrder(company1LemonadeSellContext);
        Company2.QueueOrder(company2LemonadeBuyContext);
        Company2.QueueOrder(company2RadioactiveSellLemonadeContext);
        var expectedCompany1RadioactiveLemonadeBuyFill=5;
        var expectedCompany2LemonadeBuyFill=50;
        var expectedCompany1LemonadeSellFill=50;
        var expectedCompany2RadioactiveLemonadeSellFill=5;
        // Act
        TestTradeProcessor.ProcessCompanyOrders(TestMarket);
        var actualCompany1RadioactiveBuyLemonadeFill = company1RadioactiveLemonadeBuyOrder.FilledQuantity;
        var actualCompany1LemonadeSellFill = company1LemonadeSellOrder.FilledQuantity;
        var actualCompany2LemonadeBuyFill = company2LemonadeBuyOrder.FilledQuantity;
        var actualCompany2RadioactiveSellLemonadeFill = company2RadioactiveLemonadeOrderSell.FilledQuantity;
        // Assert
        Assert.AreEqual( expectedCompany1RadioactiveLemonadeBuyFill
                        ,actualCompany1RadioactiveBuyLemonadeFill
                        ,$"Expected Company 1 to fill buy of {expectedCompany1RadioactiveLemonadeBuyFill}"+
                        $" but got {actualCompany1RadioactiveBuyLemonadeFill}");
        Assert.AreEqual( expectedCompany2LemonadeBuyFill
                        ,actualCompany2LemonadeBuyFill
                        ,$"Expected Company 2 to fill buy of {expectedCompany2LemonadeBuyFill}"+
                        $" but got {actualCompany2LemonadeBuyFill}");
        Assert.AreEqual( expectedCompany1LemonadeSellFill
                        ,actualCompany1LemonadeSellFill
                        ,$"Expected Company 1 to fill sell of {expectedCompany1LemonadeSellFill}"+
                        $" but got {actualCompany1LemonadeSellFill}");
        Assert.AreEqual( expectedCompany2RadioactiveLemonadeSellFill
                        ,actualCompany2RadioactiveSellLemonadeFill
                        ,$"Expected Company 2 to fill sell of {expectedCompany2RadioactiveLemonadeSellFill}"+
                        $" but got {actualCompany2RadioactiveSellLemonadeFill}");
    }
#endregion


    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy.gameObject);
        TestMarket = null;
        TestTradeProcessor = null;
        Company1 = null;
        Company2 = null;
        Lemonade = null;
        RadioactiveLemonade = null;
    }
}