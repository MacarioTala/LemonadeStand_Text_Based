using NUnit.Framework;

[TestFixture]
public partial class BasicTradeProcessorTests
{
#region GeneratePrimaryOrder Tests

    [Test]
    public void IfOnlyTwoOrdersExistBuyOrderIsPrimary_MatchedOrders()
    {
        // Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        var company1BuysRLFromCompany2ByCompany1 = new Order(Company1, Company2, RadioactiveLemonade, 10, 10m);
        var company2BuysRLFromCompany1ByCompany2 = new Order(Company1, Company2, RadioactiveLemonade, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromCompany2ByCompany1,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = company2BuysRLFromCompany1ByCompany2,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(Company2Context);
        
        var expected = company1BuysRLFromCompany2ByCompany1;
        // Act
        var actual=TestTradeProcessor.GeneratePrimaryOrder(TestMarket,RadioactiveLemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }

     [Test]
    public void IfOnlySellOrderExistsItIsPrimary()
    {
        // Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        var company1BuysRLFromCompany2ByCompany1 = new Order(Company1, Company2, RadioactiveLemonade, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromCompany2ByCompany1,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
   
        var expected = company1BuysRLFromCompany2ByCompany1;
        // Act
        var actual=TestTradeProcessor.GeneratePrimaryOrder(TestMarket,RadioactiveLemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }

     [Test]
    public void BuyOrderWithGreatestQuantityIsPrimary()
    {
        // Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        
        var Company3 = EconAgent.Factory.Create("Company 3", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company3);

        var company1BuysRLFromCompany2ByCompany1 = new Order(Company1, Company2, RadioactiveLemonade, 8, 10m);
        var company2BuysRLFromCompany1ByCompany2 = new Order(Company1, Company2, RadioactiveLemonade, 15, 10m);
        var company3BuysRLFromCompany2ByCompany3 = new Order(Company3, Company2, RadioactiveLemonade, 7, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromCompany2ByCompany1,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = company2BuysRLFromCompany1ByCompany2,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(Company2Context);
        var Company3Context = new ActionContext
        {
            TradeToSubmit = company3BuysRLFromCompany2ByCompany3,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company3.QueueOrder(Company3Context);
        var expected = company1BuysRLFromCompany2ByCompany1;

        // Act
        var actual=TestTradeProcessor.GeneratePrimaryOrder(TestMarket,RadioactiveLemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void SellOrderWithGreatestQuantityIsPrimaryIfNoBuyOrdersExist()
    {
        // Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        var Company3 = EconAgent.Factory.Create("Company 3", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company3);
        Company3.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 15, 10m, Period));

        var company2SellsRLToCompany1ByCompany2 = new Order(Company1, Company2, RadioactiveLemonade, 8, 10m);
        var company3SellsRLToCompany1ByCompany3 = new Order(Company1, Company3, RadioactiveLemonade, 15, 10m);
        var company2SellsToCompany1Context = new ActionContext
        {
            TradeToSubmit = company2SellsRLToCompany1ByCompany2,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(company2SellsToCompany1Context);
        var company3SellsToCompany1Context = new ActionContext
        {
            TradeToSubmit = company3SellsRLToCompany1ByCompany3,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company3.QueueOrder(company3SellsToCompany1Context);
        var expected = company3SellsRLToCompany1ByCompany3;
        // Act
        var actual=TestTradeProcessor.GeneratePrimaryOrder(TestMarket,RadioactiveLemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void BuyOrdersAreStillPrimaryEvenWithNoSeller()
    {
        // Arrange
        var company1BuysRLFromMarket = new Order(Company1, null, RadioactiveLemonade, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromMarket,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var expected = company1BuysRLFromMarket;
        // Act
        var actual=TestTradeProcessor.GeneratePrimaryOrder(TestMarket,RadioactiveLemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void SellOrdersArePrimaryEvenWithNoBuyerIfTheyreAlone()
    {
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        var company1SellsRLToAnyone = new Order(null, Company1, RadioactiveLemonade, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1SellsRLToAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var expected = company1SellsRLToAnyone;
        // Act
        var actual=TestTradeProcessor.GeneratePrimaryOrder(TestMarket,RadioactiveLemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void FullyFilledOrdersCannotBePrimary()
    {
        // Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));

        var company1BuysRLFromAnyByCompany1 = new Order(Company1, null, RadioactiveLemonade, 10, 10m);
        var company2SellsRLToCompany1ByCompany2 = new Order(Company1, Company2, RadioactiveLemonade, 10, 10m);
        company1BuysRLFromAnyByCompany1.FilledQuantity = 10;
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromAnyByCompany1,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = company2SellsRLToCompany1ByCompany2,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(Company2Context);
        var expected = company2SellsRLToCompany1ByCompany2;
        // Act
        var actual=TestTradeProcessor.GeneratePrimaryOrder(TestMarket,RadioactiveLemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }
#endregion
}