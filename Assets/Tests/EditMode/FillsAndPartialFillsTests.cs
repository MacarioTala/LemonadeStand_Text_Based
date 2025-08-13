using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class FillsAndPartialFillsTests
{
    TheEconomy testEconomy;
    Market TestMarket;

    EconAgent TestCompany1;
    EconAgent TestCompany2;

    Good Lemon;
    Good Water;
    Good Sugar;
    Good Lemonade;
    int Period;

    [SetUp]
    public void Setup()
    {
        TheEconomy.SetupForTests(new MockLogger());
        testEconomy = TheEconomy.Instance;

        TestMarket = Market.Factory.CreateMarket("TestMarket", AgentLevelEnum.Market)
            .WithDemandStrategy(ScriptableObject.CreateInstance<LinearDemandStrategy>())
            .WithSupplyProvider(new BasicSupplyProvider())
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithTransactionManager(new BasicTransactionManager());

        TestCompany1 = EconAgent.Factory.Create("TestCompany1", AgentLevelEnum.Beginner);
        TestCompany2 = EconAgent.Factory.Create("TestCompany2", AgentLevelEnum.Beginner);

        TestMarket.RegisterMarketParticipant(TestCompany1);
        TestMarket.RegisterMarketParticipant(TestCompany2);
        
        Lemon = Good.CreateInstance("Lemon", new PriceBand(.5m, 2m), RarityEnum.Common);
        Water = Good.CreateInstance("Water", new PriceBand(.5m, 1m), RarityEnum.Common);
        Sugar = Good.CreateInstance("Sugar", new PriceBand(.5m, 1m), RarityEnum.Common);
        Lemonade = Good.CreateInstance("Lemonade", new PriceBand(1m, 3m), RarityEnum.Uncommon);
        Period = 0;
    }

    [Test]
    public void TestThatOrdersAreNotFilledIfThereIsNoCounterParty()
    {
        //Arrange
        TestCompany1.GetInventory().AddGood(new InventoryEntry(Lemon,100,2,Period));
        TestCompany2.GetInventory().AddGood(new InventoryEntry(Water,100,1m,Period));

        var Company1BuysWaterFromCompany2 = new Order(TestCompany1, TestCompany2, Water, 50, 1m);
        var orderContext = CreateActionContext(Company1BuysWaterFromCompany2, TestMarket, Period);
        TestCompany1.QueueOrder(orderContext);

        var expected = LemonadeStandResultObject
                .Failure(ResultTypeEnum.NoMatchingCounterParties, "No counterparty was found for this offer");
        //Act
        TestMarket.ProcessCompanyOrders();
        var actual = Company1BuysWaterFromCompany2.OrderStatus;
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
    }
    [Test]
    public void TestThatOrderFullyFillsIfCounterPartyMatchesQuantity()
    {
        //Arrange
        TestCompany1.GetInventory().AddGood(new InventoryEntry(Lemon,100,2m,Period));
     
        var Company2BuysLemonFromCompany1 = new Order(TestCompany2, TestCompany1, Lemon, 50, 2m);
        var Company1SellsLemonToCompany2 = new Order(TestCompany2, TestCompany1, Lemon, 50, 2m);

        var Company1Context = CreateActionContext(Company1SellsLemonToCompany2, TestMarket, Period);
        var Company2Context = CreateActionContext(Company2BuysLemonFromCompany1, TestMarket, Period);
        
        TestCompany1.QueueOrder(Company1Context);
        TestCompany2.QueueOrder(Company2Context);
        var expectedLemonBuyFillQuantity = 50;
        var expectedLemonSellFillQuantity = 50;
        var expectedCompany1Lemons = 50;
        var expectedCompany2Lemons = 50;
        //Act
        TestMarket.ProcessCompanyOrders();
        var actualLemonBuyFillQuantity = Company2BuysLemonFromCompany1.FilledQuantity;
        var actualLemonSellFillQuantity = Company1SellsLemonToCompany2.FilledQuantity;
        var actualCompany1Lemons = TestCompany1.GetInventory().GetInventoryEntriesByGood(Lemon.GoodName)?.FirstOrDefault()?.quantity??0;
        var actualCompany2Lemons = TestCompany2.GetInventory().GetInventoryEntriesByGood(Lemon.GoodName)?.FirstOrDefault()?.quantity??0;
        //Assert
        Assert.AreEqual(expectedLemonBuyFillQuantity, actualLemonBuyFillQuantity,$"{TestCompany1}'s order was filled with {actualLemonBuyFillQuantity} lemons");
        Assert.AreEqual(expectedLemonSellFillQuantity, actualLemonSellFillQuantity,$"{TestCompany2}'s order was filled with {actualLemonSellFillQuantity} lemons");
        Assert.AreEqual(expectedCompany1Lemons, actualCompany1Lemons,$"{TestCompany1} had {actualCompany1Lemons} lemons at the end of the transaction");
        Assert.AreEqual(expectedCompany2Lemons, actualCompany2Lemons,$"{TestCompany2} had {actualCompany2Lemons} lemons at the end of the transaction");
    }

    [Test]
    public void TestThatMarketPartiallyFillsOrderIfBuyingCompanyDoesntWantEntireQuantity()
    { 
        //Arrange
        TestCompany2.GetInventory().AddGood(new InventoryEntry(Lemon,100,2m,Period));
        var Company1BuysLemonsFromAnyone = new Order(TestCompany1, null, Lemon, 50, 2m);
        var Company2SellsLemonsToAnyone = new Order(null, TestCompany2, Lemon, 100, 2m);
        TestCompany1.QueueOrder(CreateActionContext(Company1BuysLemonsFromAnyone, TestMarket, Period));
        TestCompany2.QueueOrder(CreateActionContext(Company2SellsLemonsToAnyone, TestMarket, Period));
        var expectedLemonBuyFillQuantity = 50;
        var expectedLemonSellFillQuantity = 50;
        //Act
        TestMarket.ProcessCompanyOrders();
        var actualLemonBuyFillQuantity = Company1BuysLemonsFromAnyone.FilledQuantity;
        var actualLemonSellFillQuantity = Company2SellsLemonsToAnyone.FilledQuantity;
        //Assert
        Assert.AreEqual(expectedLemonBuyFillQuantity, actualLemonBuyFillQuantity
                        ,$"Expected {TestCompany1}'s order to fill with {expectedLemonBuyFillQuantity}, "
                          +"but was filled with {actualLemonBuyFillQuantity} lemons");
        Assert.AreEqual(expectedLemonSellFillQuantity, actualLemonSellFillQuantity
                        ,$"Expected {TestCompany2}'s order to fill with {expectedLemonSellFillQuantity}, "
                          +"but was filled with {actualLemonSellFillQuantity} lemons");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testEconomy.gameObject);
        TestMarket = null;
        TestCompany1 = null;
        TestCompany2 = null;
        Lemon = null;
        Water = null;
        Sugar = null;
        Lemonade = null;
    }
}
