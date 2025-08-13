using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;

public partial class BasicTradeProcessorTests
{
    #region ExecuteBestTradesForGood Tests

    [Test]
    public void EBTFG_NoBuyersNoSellersNoExceptions()
    {
        // Arrange
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        object actual = null;
        // Act
        try 
        {TestTradeProcessor.ExecuteBestTradesForGood(RadioactiveLemonade,TestMarket,OrdersSentToMarket);}
        catch (Exception e)
        {
            actual = e;
        };
        // Assert
        Assert.IsNull(actual);
    }
    [Test]
    public void EBTFG_NoMatchingOrdersNoFillsNoExceptions()
    {
        // Arrange
        var Company1BuysLemonsFromAnyone = new Order(Company1, null, Lemon, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = Company1BuysLemonsFromAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);

        var Company2BuysRLFromAnyone = new Order(Company2, null, RadioactiveLemonade, 10, 10m);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = Company2BuysRLFromAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(Company2Context);
        
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        object actual = null;
        var expectedCompany1Fills = 0;
        var expectedCompany2Fills = 0;
        // Act
        try 
        {TestTradeProcessor.ExecuteBestTradesForGood(RadioactiveLemonade,TestMarket,OrdersSentToMarket);}
        catch (Exception e)
        {
            actual = e;
        };
        var actualCompany1Fills = Company1BuysLemonsFromAnyone.FilledQuantity;
        var actualCompany2Fills = Company2BuysRLFromAnyone.FilledQuantity;
        // Assert
        Assert.IsNull(actual);
        Assert.AreEqual(expectedCompany1Fills, actualCompany1Fills, "Company 1 did not fill the order correctly");
        Assert.AreEqual(expectedCompany2Fills, actualCompany2Fills, "Company 2 did not fill the order correctly");
    }
    [Test]
    public void EBTFG_NullBuyerAndSellerNoFills()
    {
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        var noOneSellsAnythingToAnyone = new Order(null, null, RadioactiveLemonade, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = noOneSellsAnythingToAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        // Act
        TestTradeProcessor.ExecuteBestTradesForGood(RadioactiveLemonade,TestMarket,OrdersSentToMarket);
        var actualCompany1Fills = noOneSellsAnythingToAnyone.FilledQuantity;
        // Assert
        Assert.AreEqual(0, actualCompany1Fills, "Company 1 did not fill the order correctly");
    }
    public static IEnumerable<TestCaseData> TestCases_OneBuyerOneSeller{
        get
        {
            yield return new TestCaseData(10,10m,10,10,10,10,10m,10m)
             .SetName("One Buyer, One Seller, Perfect Match" );
            yield return new TestCaseData(10,10m,10,5,5,5,10m,10m)
             .SetName("One Buyer, One Seller, Demand Exceeds Supply");
            yield return new TestCaseData(10,10m,5,10,5,5,10m,10m)
             .SetName("One Buyer, One Seller, Supply Exceeds Demand");
            yield return new TestCaseData(10,10m,10,0,0,0,10m,10m)
             .SetName("One Buyer, One Seller, No Supply, No fills");
            yield return new TestCaseData(10,10m,0,10,0,0,10m,10m)
             .SetName("One Buyer, One Seller, No Demand, No fills");
        }
    }
    [Test,TestCaseSource(nameof(TestCases_OneBuyerOneSeller))]
    public void EBTFG_OneBuyerOneSeller(int sellerInventoryQuantity,
                                        decimal sellerAcquirePrice,
                                        int buyerOrderQuantity, 
                                        int sellerOrderQuantity,
                                        int expectedBuyerFill,
                                        int expectedSellerFill,
                                        decimal buyerBid,
                                        decimal sellerAsk
                                        )
    {
        // Arrange
        Company2.GetInventory().AddGood(new InventoryEntry(Lemonade, sellerInventoryQuantity, sellerAcquirePrice, Period));
        var company1BuysRLFromCompany2ByCompany1 = new Order(Company1, Company2, Lemonade, buyerOrderQuantity, buyerBid);
        var company2SellsRLToCompany1ByCompany2 = new Order(Company1, Company2, Lemonade,sellerOrderQuantity, sellerAsk);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromCompany2ByCompany1,
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
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        // Act
        TestTradeProcessor.ExecuteBestTradesForGood(Lemonade,TestMarket,OrdersSentToMarket);
        var actualCompany1Fills = company1BuysRLFromCompany2ByCompany1.FilledQuantity;
        var actualCompany2Fills = company2SellsRLToCompany1ByCompany2.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedBuyerFill, actualCompany1Fills, "Company 1 did not fill the order");
        Assert.AreEqual(expectedSellerFill, actualCompany2Fills, "Company 2 did not fill the order");
    }

    [Test]
    public void EBTFG_NoBuyersNoFills()
    {
        // Arrange
        Company1.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, 10, 10m, Period));
        var company1SellsRLToAnyone= new Order(null, Company1, RadioactiveLemonade, 10, 10m);
        var company2SellsRLToAnyone= new Order(null, Company2, RadioactiveLemonade, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1SellsRLToAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = company2SellsRLToAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(Company2Context);
        var expectedCompany1Fills = 0;
        var expectedCompany2Fills = 0;
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        // Act
        TestTradeProcessor.ExecuteBestTradesForGood(RadioactiveLemonade,TestMarket,OrdersSentToMarket);
        var actualCompany1Fills = company1SellsRLToAnyone.FilledQuantity;
        var actualCompany2Fills = company2SellsRLToAnyone.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedCompany1Fills, actualCompany1Fills, "Company 1 did not fill the order correctly");
        Assert.AreEqual(expectedCompany2Fills, actualCompany2Fills, "Company 2 did not fill the order correctly");
    }
    [Test]
    public void EBTFG_NoSellersNoFills()
    {
        // Arrange
        var company1BuysRLFromAnyone= new Order(Company1, null, RadioactiveLemonade, 10, 10m);
        var company2BuysRLFromAnyone= new Order(Company2, null, RadioactiveLemonade, 10, 10m);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = company2BuysRLFromAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(Company2Context);
        var expectedCompany1Fills = 0;
        var expectedCompany2Fills = 0;
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        // Act
        TestTradeProcessor.ExecuteBestTradesForGood(RadioactiveLemonade,TestMarket,OrdersSentToMarket);
        var actualCompany1Fills = company1BuysRLFromAnyone.FilledQuantity;
        var actualCompany2Fills = company2BuysRLFromAnyone.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedCompany1Fills, actualCompany1Fills, "Company 1 did not fill the order correctly");
        Assert.AreEqual(expectedCompany2Fills, actualCompany2Fills, "Company 2 did not fill the order correctly");
    }

    public static IEnumerable<TestCaseData> TestCases_OneBuyerMultipleSellers{
        get
        {
            yield return new TestCaseData(15,10m,10,10m,20,10m,11,10m,9,10m,20,11,9,true,true,true)
             .SetName("One Buyer, Multiple Sellers, Perfect Match -- All filled" );
            yield return new TestCaseData(20,10m,20,10m,50,10m,20,10m,20,10m,40,20,20,false,true,true)
             .SetName("One Buyer, Multiple Sellers, Undersupply -- Buyer partial fill" );
            yield return new TestCaseData(20,10m,20,10m,20,10m,20,10m,20,9m,20,0,20,true,false,true)
             .SetName("One Buyer, Multiple Sellers, Oversupply -- Low price fill");
            yield return new TestCaseData(30, 10m, 15, 10m, 50, 10m, 15, 10m, 10, 2m, 25, 15, 10, false, true, true)
             .SetName("One Buyer, Multiple Sellers, Undersupply -- Buyer Partially Filled, both sellers filled");

        }
    }
    [Test,TestCaseSource(nameof(TestCases_OneBuyerMultipleSellers))]
    public void EBTFG_OneBuyerMultipleSellers(
                                        int Seller1InventoryQuantity,
                                        decimal Seller1AcquirePrice,
                                        int Seller2InventoryQuantity,
                                        decimal Seller2AcquirePrice,
                                        int BuyerOrderQuantity,
                                        decimal BuyerBid,
                                        int Seller1OrderQuantity,
                                        decimal Seller1Ask,
                                        int Seller2OrderQuantity,
                                        decimal Seller2Ask,
                                        int expectedBuyerFill,
                                        int expectedSeller1Fill,
                                        int expectedSeller2Fill,
                                        bool buyerIsFilled,
                                        bool seller1IsFilled,
                                        bool seller2IsFilled
                                    )
    {
        // Arrange
        var Company3 = EconAgent.Factory.Create("Company 3", AgentLevelEnum.Beginner);
        Company2.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, Seller1InventoryQuantity, Seller1AcquirePrice, Period));
        Company3.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, Seller2InventoryQuantity, Seller2AcquirePrice, Period));
        
        TestMarket.RegisterMarketParticipant(Company3);
        var company1BuysRLFromAnyoneByCompany1 = new Order(Company1, null, RadioactiveLemonade, BuyerOrderQuantity, BuyerBid);
        var company2SellsRLToAnyone1ByCompany2 = new Order(null, Company2, RadioactiveLemonade, Seller1OrderQuantity, Seller1Ask);
        var company3SellsRLToAnyone2ByCompany3 = new Order(null, Company3, RadioactiveLemonade, Seller2OrderQuantity, Seller2Ask);
        var Company1Context = new ActionContext
        {
            TradeToSubmit = company1BuysRLFromAnyoneByCompany1,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company1.QueueOrder(Company1Context);
        var Company2Context = new ActionContext
        {
            TradeToSubmit = company2SellsRLToAnyone1ByCompany2,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company2.QueueOrder(Company2Context);
        var Company3Context = new ActionContext
        {
            TradeToSubmit = company3SellsRLToAnyone2ByCompany3,
            MarketToSubmitTo = TestMarket,
            Period = Period
        };
        Company3.QueueOrder(Company3Context);
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        // Act
        TestTradeProcessor.ExecuteBestTradesForGood(RadioactiveLemonade,TestMarket,OrdersSentToMarket);
        var actualCompany1Fills = company1BuysRLFromAnyoneByCompany1.FilledQuantity;
        var actualCompany2Fills = company2SellsRLToAnyone1ByCompany2.FilledQuantity;
        var actualCompany3Fills = company3SellsRLToAnyone2ByCompany3.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedBuyerFill, actualCompany1Fills, "Company 1 did not fill the order");
        Assert.AreEqual(expectedSeller1Fill, actualCompany2Fills, "Company 2 did not fill the order");
        Assert.AreEqual(expectedSeller2Fill, actualCompany3Fills, "Company 3 did not fill the order");
        Assert.AreEqual(buyerIsFilled,company1BuysRLFromAnyoneByCompany1.IsFullyFilled);
        Assert.AreEqual(seller1IsFilled,company2SellsRLToAnyone1ByCompany2.IsFullyFilled);
        Assert.AreEqual(seller2IsFilled,company3SellsRLToAnyone2ByCompany3.IsFullyFilled);
    }

    public static IEnumerable<TestCaseData> TestCases_OneSellerMultipleBuyers{
        get
        {
            yield return new TestCaseData(10,10m,5,10m,5,10m,10,10m,5,5,10)
                .SetName("One Seller, Multiple Buyers, Perfect Match -- All Fill" );
            yield return new TestCaseData(20,10m,20,10m,15,10m,20,10m,20,0,20)
                .SetName("One Seller, Multiple Buyers, Oversupply -- High quantity fill" );
            yield return new TestCaseData(30,10m,20,10m,15,10m,30,10m,20,10,30)
                .SetName("One Seller, Multiple Buyers, Undersupply -- Whales fill first, partial fill for others" );
        }
    }
    
    [Test,TestCaseSource(nameof(TestCases_OneSellerMultipleBuyers))]
    public void EBTFG_OneSellerMultipleBuyers(
                                   int SellerInventoryQuantity,
                                   decimal SellerAcquirePrice,    
                                   int Buyer1OrderQuantity,
                                   decimal Buyer1Bid,
                                   int Buyer2OrderQuantity,
                                   decimal Buyer2Bid,
                                   int SellerOrderQuantity,
                                   decimal SellerAsk,
                                   int expectedBuyer1Fill,
                                   int expectedBuyer2Fill,
                                   int expectedSellerFill

    )
    {
        //Arrange
        var Company3 = EconAgent.Factory.Create("Company 3", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company3);
        Company3.GetInventory().AddGood(new InventoryEntry(RadioactiveLemonade, SellerInventoryQuantity, SellerAcquirePrice, Period));

        var Company1BuysRLFromAnyone = new Order(Company1, null, RadioactiveLemonade, Buyer1OrderQuantity, Buyer1Bid);
        var Company2BuysRLFromAnyone = new Order(Company2, null, RadioactiveLemonade, Buyer2OrderQuantity, Buyer2Bid);
        var Company3SellsRLToAnyone = new Order(null, Company3, RadioactiveLemonade, SellerOrderQuantity, SellerAsk);

        Company1.QueueOrder( new ActionContext
        {
            TradeToSubmit = Company1BuysRLFromAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        });
        Company2.QueueOrder( new ActionContext
        {
            TradeToSubmit = Company2BuysRLFromAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        });
        Company3.QueueOrder( new ActionContext
        {
            TradeToSubmit = Company3SellsRLToAnyone,
            MarketToSubmitTo = TestMarket,
            Period = Period
        });
        
        var OrdersSentToMarket = TestTradeProcessor.GetOrders();
        //Act
        TestTradeProcessor.ExecuteBestTradesForGood(RadioactiveLemonade,TestMarket,OrdersSentToMarket);
        var actualCompany1Fills = Company1BuysRLFromAnyone.FilledQuantity;
        var actualCompany2Fills = Company2BuysRLFromAnyone.FilledQuantity;
        var actualCompany3Fills = Company3SellsRLToAnyone.FilledQuantity;
        //Assert
        Assert.AreEqual(expectedBuyer1Fill, actualCompany1Fills, "Company 1 did not fill the order correctly");
        Assert.AreEqual(expectedBuyer2Fill, actualCompany2Fills, "Company 2 did not fill the order correctly");
        Assert.AreEqual(expectedSellerFill, actualCompany3Fills, "Company 3 did not fill the order correctly");
    }
#endregion
}