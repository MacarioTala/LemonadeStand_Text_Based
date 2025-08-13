using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class SupplyTests
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
    readonly iSupplyProvider TestSupplyProvider= new BasicSupplyProvider();

    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    [SetUp]
    public void Setup()
    {
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
        .WithDemandStrategy(TestDemandStrategy)
            .WithSupplyProvider(TestSupplyProvider)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithPriceManager(new BasicPriceManager());
        TestSupplyProvider.Initialize(TestMarket);

        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company 2", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);

        Period=0;

        Lemon = Good.CreateInstance("Lemon", PriceBand1, RarityEnum.Common);
        Lemonade = Good.CreateInstance("Lemonade", PriceBand2, RarityEnum.Uncommon);
    }
#region Isolated BasicSupplyProvider Tests
    [Test]
    public void Company1Submits1000UnitSellOrder_ExpectSupply1000Units()
    {
        //Arrange
        var Company1SellsLemonadeToAnyone = new Order(null,Company1, Lemonade,1000,20);
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 1000,10,0));
        const int period = 2;
        var expected = (Lemonade, 1000, 20m);

        //Act
        Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone, TestMarket,period));
        var actual = TestSupplyProvider.GetSupplyInPeriod(2)?.FirstOrDefault();
        //Assert
        Assert.That(expected, Is.EqualTo(actual));
    }
    [Test]
    public void Company1Submits1000UnitBuyOrder_ExpectSupply0Units()
    {
        //Arrange
        var Company1BuysLemonadeFromAnyone = new Order(Company1,null, Lemonade,1000,20);
        Company1.SetCash(1000);
        const int period = 2;
        Order expected = null;

        //Act
        Company1.QueueOrder(CreateActionContext(Company1BuysLemonadeFromAnyone, TestMarket,period));
        var actual = TestSupplyProvider.GetSupplyInPeriod(2)?.FirstOrDefault();
        //Assert
        Assert.AreEqual(expected, actual);
    }
#endregion
#region From Market
    [Test]
    public void Company1Submits1000UnitSellOrder_ExpectSupply1000Units_FromMarket()
    {
        //Arrange
        var Company1SellsLemonadeToAnyone = new Order(null,Company1, Lemonade,1000,20);
        Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 1000,10,0));
        const int period = 2;
        var expected = (Lemonade, 1000, 20m);
        //Act   
        Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone, TestMarket,period));
        var actual = TestMarket.GetSupplyInPeriod(2)?.FirstOrDefault();
        //Assert
        Assert.That(expected, Is.EqualTo(actual));
    }
#endregion

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(TestEconomy);
        Company1 = null;
        Company2 = null;
        TestMarket = null;
        Lemon = null;
        Lemonade = null;
    }
}