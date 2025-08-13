using NUnit.Framework;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using static TestHelpers;
[TestFixture]
public class CompanyTests
{
    TheEconomy TestEconomy;
    private TestHelpers testHelpers;
    Market TestMarket;
    EconAgent Company1;
    Good Lemonade;
    const int Period = 0;

    [SetUp]
    public void Setup()
    {
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        testHelpers = new TestHelpers();
        Company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
        Lemonade = Good.CreateInstance("Lemonade", new PriceBand(1, 3), RarityEnum.Common);
        
        TestMarket = Market.Factory.CreateStarterMarket("Test Market", AgentLevelEnum.Market, new LinearDemandStrategy());
        TestMarket.RegisterMarketParticipant(Company1);
    }

    [Test]
    public void Set_initial_cash_sets_cash_to_10000_for_beginner()
    {
        //arrange
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        const float expected_cash = 10000;
        //Act
        //Assert
        Assert.AreEqual(expected_cash, company.GetCash());
    }
    [Test]
    public void BuyGoodRemovesCashFromBuyer()
    {
        //arrange
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        var good = Good.CreateInstance("Lemon", new PriceBand(1, 3), RarityEnum.Common);
        const decimal trade_price = 3.0m;
        var expected_cash = 10000 - 3;
        //Act
        company.BuyGood(good, 1, trade_price);
        var actual_cash = company.GetCash();
        //Assert
        Assert.AreEqual(expected_cash, actual_cash);

    }

    [Test]
    public void Buy_good_when_buyer_has_enough_cash_adds_good_to_inventory()
    {
        //arrange
        const int PeriodIsIrrelevant = 0;
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        var good = Good.CreateInstance("Lemon", new PriceBand(1, 3), RarityEnum.Common);
        const decimal trade_price = 3.0m;
        var expected_inventory_entry = new InventoryEntry(good, 1, trade_price, PeriodIsIrrelevant);
        
        //Act
        company.BuyGood(good, 1, trade_price);
        var actual_inventory_entry = company.GetInventory().GetInventoryEntries().Where(x => x.good.GoodName == "Lemon" 
                                                                    && x.quantity == 1 
                                                                    && x.Cost == trade_price
                                                                    ).FirstOrDefault();
        //Assert
        var expected_inventory = testHelpers.ListToString(new List<InventoryEntry> { expected_inventory_entry });
        var actual_inventory = testHelpers.ListToString(company.GetInventory().GetInventoryEntries());
        Assert.AreEqual(expected_inventory, actual_inventory);
    }

    [Test]
    public void Buy_good_when_buyer_doesnt_have_enough_cash_throws_exception()
    {
        //arrange
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        var good = Good.CreateInstance("Lemon", new PriceBand(1, 3), RarityEnum.Common);
        const decimal trade_price = 3.0m;
        const int trade_quantity = 10000;
        //Act
        //Assert
        Assert.Throws<InsufficientFundsException>(() => company.BuyGood(good, trade_quantity, trade_price));
    }
    [Test]
    public void Sell_good_when_seller_has_good_in_inventory_adds_cash_to_seller()
    {
        //arrange
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        var good = Good.CreateInstance("Lemon", new PriceBand(1, 3), RarityEnum.Common);
        company.BuyGood(good, 1,good.GetPrice());
        var initial_cash = company.GetCash();
        var good_price = 3.0m;
        var expected_cash = initial_cash + good_price;
        
        //Act
        company.SellGood(good, 1, good_price);
        var actual_cash = company.GetCash();
        //Assert
        Assert.AreEqual(expected_cash, actual_cash);
    }

    [Test]
    public void Sell_good_throws_exception_when_not_enough_quantity()
    {
        //arrange
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        var good = Good.CreateInstance("Lemon", new PriceBand(1, 3), RarityEnum.Common);
        company.BuyGood(good, 1,good.GetPrice());
        var good_price = 3.0m;
        //Act
        //Assert
        Assert.Throws<InventoryException>(() => company.SellGood(good, 2, good_price));
    }

    [Test]
    public void QueueTradeReturnsFailureWhenActionContextDoesNotContainTrade()
    {
        //Assert
        var company1 = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner);
        var lemon = Good.CreateInstance("Lemon", new PriceBand(1, 3), RarityEnum.Common);
        var context = new ActionContext{BidToSubmit = 2.0m, AskToSubmit = 3.0m, GoodToSubmit = lemon};

        var expected = new LemonadeStandResultObject{Result = ResultTypeEnum.ContextHasNoTrade, Message = "ActionContext does not contain a trade"};
        //Act
        var actual=company1.QueueOrder(context);
        
        //Assert
        Assert.AreEqual(expected.Result, actual.Result);
        
    }

    [Test]
    public void QO_RejectsOrdersWithNegativePrice()
    {
        //Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 1000, 3m,0));
        var company1Order = new Order(null, Company1, Lemonade, 1000, -3.5m);
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.OrderHasInvalidPrice,"").Result;
        //Act
        var actual = Company1.QueueOrder(CreateActionContext(company1Order, TestMarket,Period)).Result;
        //Assert
        Assert.AreEqual(expected, actual);
        
    }
    [Test]
    public void QO_RejectsOrdersWithZeroQuantity()
    {
        //Arrange
        var Company1BuysLemonade = new Order(Company1,null, Lemonade, 0, 3.5m);
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.OrderHasInvalidQuantity,"").Result;
        //Act
        var actual = Company1.QueueOrder(CreateActionContext(Company1BuysLemonade, TestMarket,Period)).Result;
        //Assert
        Assert.AreEqual(expected, actual);
    }
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy.gameObject);
        Object.DestroyImmediate(Lemonade);
        TestMarket = null;
        Company1 = null;
    }
}