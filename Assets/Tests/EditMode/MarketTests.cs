using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public partial class MarketTests
{
    private TheEconomy TestEconomy;

    private Market test_initial_market;
    PopulationAgent TestPopulation;

    iStrategy TestReduceEnnuiStrategy;
    EconAgent Company1;
    EconAgent Company2;
    Market TestMarket;

    const int Period = 0;

    //Goods
    Good lemon;
    Good water;
    Good sugar;
    Good lemonade;

    //Market Dependencies
    iDemandStrategy TestDemandStrategy;

    //Recipes
    private Recipe lemonade_recipe;

    readonly PriceBand band1 = new(.5m, 1.0m);
    readonly PriceBand band2 = new(1.0m, 3.0m);
    readonly ITradeLogger trade_logger = new MockLogger();
    readonly List<Good> test_goods = new();

    [SetUp]
    public void SetUp()
    {
        //Create the economy
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        //Setup Market Dependencies
        TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();

        //Setup Population Dependencies
        TestReduceEnnuiStrategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
            .WithAggressionLevel(.55m)
            .Build();

        //Setup Market
        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
            .WithDemandStrategy(TestDemandStrategy).WithDemandStrategy(TestDemandStrategy)
            .WithTradeProcessor(new BasicTradeProcessor())
            .WithPriceManager(new BasicPriceManager())
            .WithTransactionManager(new BasicTransactionManager())
            .WithMarketDataManager(new BasicMarketDataManager())
            .WithDemographicManager(new MockDemographicManager());


        //Setup Population
        TestPopulation = EconAgentBuilder.For<PopulationAgent>()
            .Named("Test Population")
            .AtLevel(AgentLevelEnum.Market)
            .WithInitialCash(10000)
            .WithBehaviourStrategy(TestReduceEnnuiStrategy)
            .WithEnnui(.99f)
            .WithPopulation(1000)
            .Build();
        TestReduceEnnuiStrategy.GenerateGoals(TestPopulation);

        //Setup Companies
        Company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company2", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);
        TestMarket.RegisterMarketParticipant(TestPopulation);

        SetupGoodsAndRecipes();

        //Setup the initial market
        SetupInitialMarket();
    }

    private void SetupInitialMarket()
    {
        test_initial_market = Market.Factory.CreateStarterMarket(companyName: "The First Market", 
                                                    companyLevel: AgentLevelEnum.Market,
                                                    demandStrategy: TestDemandStrategy);
        test_initial_market.InitializeDemandForSpecificGood(lemon, 1000);
    }

    private void SetupGoodsAndRecipes()
    {
        lemon = Good.CreateInstance("Lemon", band2, RarityEnum.Common);
        water = Good.CreateInstance("Water", band1, RarityEnum.Common);
        sugar = Good.CreateInstance("Sugar", band1, RarityEnum.Common);
        lemonade = Good.CreateInstance("Lemonade", band2, RarityEnum.Uncommon);
        lemonade.IsProducedGood = true;
        lemonade_recipe = new Recipe(RecipeName: "Basic Lemonade",
                                     product: lemonade, 
                                     ingredients: new List<Ingredient> { new(lemon, 9), 
                                                                        new(sugar, 2), 
                                                                        new(water, 7) });                
        test_goods.Add(lemon);
        test_goods.Add(water);
        test_goods.Add(sugar);
        test_goods.Add(lemonade);
    }

    #region Initialization tests
    [Test]
    public void When_an_economy_is_created_it_should_have_a_market()
    {
        // Arrange
        var expected = typeof(Market);
        // Act
        var actual = from company in TestEconomy.companies
                     where company.Name == "The First Market"
                     select company.GetType();
        // Assert
        Assert.AreEqual(expected, actual.First());
    }
    #endregion
    #region Demand tests

    [Test]
    public void PerceivedCostShouldBeAverageOfCosts()
    {
        // Arrange
        lemon.SetPrice(.5m);
        water.SetPrice(.75m);
        sugar.SetPrice(1.0m);
        TestMarket.MarketData.Add(new()
        {
            Company = TestMarket,
            Good = lemon,
            Bid = lemon.GetPrice(),
            Ask = lemon.GetPrice()
        });
        TestMarket.MarketData.Add(new()
        {
            Company = TestMarket,
            Good = water,
            Bid = water.GetPrice(),
            Ask = water.GetPrice()
        });
        TestMarket.MarketData.Add(new()
        {
            Company = TestMarket,
            Good = sugar,
            Bid = sugar.GetPrice(),
            Ask = sugar.GetPrice()
        });
        var asks = new Dictionary<Good, decimal>
        {
            { lemon, lemon.GetPrice() },
            { water, water.GetPrice() },
            { sugar, sugar.GetPrice() }
        };
        var recipe1 = new Recipe("Recipe 1", lemonade, new List<Ingredient> { new(lemon, 1), new(water, 1), new(sugar, 1) });
        var recipe2 = new Recipe("Recipe 2", lemonade, new List<Ingredient> { new(lemon, 2), new(water, 2), new(sugar, 3) });
        var recipe1Cost = recipe1.GetPerceivedCostPerUnit(asks);
        var recipe2Cost = recipe2.GetPerceivedCostPerUnit(asks);
        var expected = (recipe1Cost + recipe2Cost) / 2;
        Company1.AddRecipe(recipe1);
        Company2.AddRecipe(recipe2);

        // Act
        var actual = TestMarket.GetPerceivedCostOfGood(lemonade);
        // Assert
        Assert.AreEqual(expected, actual);
    }
#endregion
    #region Perishability tests
    [Test]
    public void PerishableGoodsShouldExpire()
    {
        // Arrange
        var tradingPeriod = 1;
        var testMarket = TestMarket;
        var company = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        testMarket.RegisterMarketParticipant(company);
        var francium = Good.CreateInstance("Francium", band2, RarityEnum.Very_Rare);
        francium.ExpiresAfterPeriods = 1;
        company.GetInventory().AddGood(new InventoryEntry(francium, 1,10000m,0));
        var expected = 0;
        // Act
        testMarket.ExpireGoods(tradingPeriod);
        var franciumEntry = company.GetInventory().GetInventoryEntriesByGood(francium.GoodName).FirstOrDefault();
        var actual = franciumEntry?.quantity??0;
        // Assert
        Assert.AreEqual(expected, actual);
        //Cleanup
        testMarket.RemoveMarketParticipant(company);
    }

    [Test]
    public void NonPerishableGoodsShouldNotExpire()
    {
        // Arrange
        var period = 1;
        var testMarket = TestMarket;
        var company = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        testMarket.RegisterMarketParticipant(company);
        var ripeLemon = Good.CreateInstance("Ripe Lemon", band2, RarityEnum.Common);
        ripeLemon.ExpiresAfterPeriods = 1;
        var ripeLemonInventoryEntry = new InventoryEntry(ripeLemon, 10, 3.0m,0);
        var waterInventoryEntry = new InventoryEntry(water, 10, 1.0m,0);
        var sugarInventoryEntry = new InventoryEntry(sugar, 10, 1.0m,0);
        company.GetInventory().AddGood(ripeLemonInventoryEntry);
        company.GetInventory().AddGood(waterInventoryEntry);
        company.GetInventory().AddGood(sugarInventoryEntry);
        var expectedWaterQuantity = 10;
        var expectedRipeLemonQuantity = 0;
        // Act
        testMarket.ExpireGoods(period);
        var actualWaterQuantity = company.GetInventory().GetInventoryEntriesByGood(water.GoodName).FirstOrDefault().quantity;
        var ripeLemonEntries = company.GetInventory().GetInventoryEntriesByGood(ripeLemon.GoodName).FirstOrDefault();
        var actualRipeLemonQuantity = ripeLemonEntries?.quantity??0;
        // Assert
        Assert.IsTrue(expectedWaterQuantity==actualWaterQuantity && expectedRipeLemonQuantity==actualRipeLemonQuantity);
        
        //Cleanup
        testMarket.RemoveMarketParticipant(company);
    }

    [Test]
    public void OnlyPerishableGoodsAtTheirExpiryPeriodShouldExpire()
    {
        // Arrange
        var tradingPeriod = 1;
        var company = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        var testMarket = TestMarket;
        testMarket.RegisterMarketParticipant(company);
        company.GetInventory().AddGood(new InventoryEntry(lemon, 10, 3.0m,0));
        lemon.ExpiresAfterPeriods=2;
        var ripeLemon = Good.CreateInstance("Ripe Lemon", band2, RarityEnum.Common);
        ripeLemon.ExpiresAfterPeriods=1;
        company.GetInventory().AddGood(new InventoryEntry(ripeLemon, 10, 3.0m,0));
        var expectedLemonQuantity = 10;
        var expectedRipeLemonQuantity = 0;
        // Act
        testMarket.ExpireGoods(tradingPeriod);
        var actualLemonEntries = company.GetInventory().GetInventoryEntriesByGood(lemon.GoodName).FirstOrDefault();
        var actualRipeLemonEntries = company.GetInventory().GetInventoryEntriesByGood(ripeLemon.GoodName).FirstOrDefault();
        var actualLemonQuantity = actualLemonEntries?.quantity??0;
        var actualRipeLemonQuantity = actualRipeLemonEntries?.quantity??0;
        // Assert
        Assert.AreEqual(expectedLemonQuantity, actualLemonQuantity);
        Assert.AreEqual(expectedRipeLemonQuantity, actualRipeLemonQuantity);

        //Cleanup
        testMarket.RemoveMarketParticipant(company);
    }   

    [Test]
    public void TheSameGoodBoughtAtDifferentTimesExpiresAtDifferentPeriods()
    {
        // Arrange
        var tradingPeriod = 1;
        var company = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        var testMarket = TestMarket;
        testMarket.RegisterMarketParticipant(company);
        
        var apple = Good.CreateInstance("Apple", band2, RarityEnum.Common);
        apple.ExpiresAfterPeriods=1;
        company.GetInventory().AddGood(new InventoryEntry(apple, 10, 3.0m,0));
        company.GetInventory().AddGood(new InventoryEntry(apple, 10, 3.0m,1));
        var expectedAppleQuantity = 10;
        // Act
        testMarket.ExpireGoods(tradingPeriod); //only 1 batch of apples expires
        var actualAppleEntry = company.GetInventory().GetInventoryEntriesByGood(apple.GoodName).FirstOrDefault();
        var actualAppleQuantity = actualAppleEntry?.quantity??0;
        // Assert
        Assert.AreEqual(expectedAppleQuantity, actualAppleQuantity);
        //Cleanup
        testMarket.RemoveMarketParticipant(company);
        }

#endregion   
#region Production tests
   [Test]
   public void Companies_cannot_make_goods_without_a_recipe()
   {
       // Arrange
       var company = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
       company.BuyGood(lemon, 10,3.0m);
       company.BuyGood(sugar, 10,3.0m);
       company.BuyGood(water, 10,3.0m);
       var expectedText = "not found in company's recipe book";
       System.Exception actual=null;
       var context = new ActionContext{Recipe = lemonade_recipe, QuantityToMake = 1};
       // Act
       try
       {
        company.MakeRecipe(context);
        }
        catch(System.Exception e)
        {
            actual = e;
        }
        // Assert
        Assert.That(actual, Is.TypeOf<RecipeException>());
        StringAssert.Contains(expectedText, actual.Message);
    }

   #endregion

   #region publish tests
    [Test]
    public void PublishMarketDataShouldAddOnePercentToPrice()
    {
        // Arrange
        var testMarket = TestMarket;
        testMarket.GetInventory().AddGood(new InventoryEntry(lemon, 1000, 3.0m, 0));
        var initialPrice = testMarket.GetInventory().GetInventoryEntriesByGood(lemon.GoodName).First().good.GetPrice();
        var expected = initialPrice * 1.01m;
        // Act
        testMarket.CalculateNewBidAskSpreadForMarket();
        var marketData = testMarket.MarketData;
        var actual = marketData.Where(entry => entry.Good.GoodName == lemon.GoodName
                                        && entry.Company.Name == testMarket.Name)
                                    .First().Ask;
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void PublishSpreadToMarket_should_update_existing_MarketData_if_spread_exists()
    {
        // Arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(company1);
        var context = new ActionContext{BidToSubmit = 2.0m, AskToSubmit = 3.0m, GoodToSubmit = lemon, MarketToSubmitTo = TestMarket};
        var expected = new List<MarketData>{new() { Good = lemon, Company = company1, Bid = 2.0m, Ask = 3.0m}};
        //Act
        company1.SubmitBidAskSpreadToMarket(context);
        TestMarket.CalculateNewBidAskSpreadForMarket();
        var actual = TestMarket.MarketData.Where(x=>x.Good == lemon).ToList();
        // Assert
        Assert.AreEqual(expected, actual);
        // Cleanup
        TestMarket.RemoveMarketParticipant(company1);
    }
    [Test]
    public void PublishSpreadToMarketReturnsErrorIfActionContextIsIncomplete()
    {
        // Arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(company1);
        var context = new ActionContext{BidToSubmit = 2.0m, AskToSubmit = 3.0m, GoodToSubmit = lemon};
        
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.MarketNotSet, "Market not set");
        //Act
        var actual = company1.SubmitBidAskSpreadToMarket(context);
        
        // Assert
        Assert.AreEqual(expected, actual);

        // Cleanup
        TestMarket.RemoveMarketParticipant(company1);
    }
                               
   #endregion
   #region Trade Tests
   [Test]
    public void ACompanyCannotBuyGoodsWithInsufficientCash()
    {
        // Arrange
        Company2.SetCash(0);
        var company2BuysLemons = new Order(Company2, null, lemon, 4000, 3.0m);
        var expected= LemonadeStandResultObject.Failure(ResultTypeEnum.InsufficientCash, "").Result;
        // Act
        var actual = Company2.QueueOrder(CreateActionContext(company2BuysLemons,TestMarket,0)).Result;
        TestMarket.ProcessCompanyOrders();
        var marketTradesInPeriod = TestMarket.GetExecutionsInPeriod(0);
        // Assert
        Assert.AreEqual(expected, actual);
        Assert.AreEqual(0, marketTradesInPeriod.Count);
    }
    [Test]
    public void CompaniesCanBuyGoodsFromEachOtherViaMatchingQueuedOrders()
    {
        // Arrange
        var period = 0;
        Company1.GetInventory().AddGood(new InventoryEntry(sugar,10,2.0m,period));
        Company2.GetInventory().AddGood(new InventoryEntry(sugar,10, 2.0m,period));
        
        var expectedCompany1Cash = Company1.GetCash() - 2;
        var expectedCompany2Cash = Company2.GetCash() + 2;
        var expectedCompany1SugarQuantity = 11;
        var expectedCompany2SugarQuantity = 10 - 1;

        var Company1BuysSugarFromCompany2 = new Order(Company1, Company2, sugar, 1, 2.0m);
        var Company2SellsSugarToCompany1 = new Order(Company1,Company2, sugar, 1, 2.0m);
        
        // Act
        Company1.QueueOrder(CreateActionContext(Company1BuysSugarFromCompany2,TestMarket,0));
        Company2.QueueOrder(CreateActionContext(Company2SellsSugarToCompany1,TestMarket,0));
        TestMarket.ProcessCompanyOrders();
        var actualCompany1Cash = Company1.GetCash();
        var actualCompany2Cash = Company2.GetCash();
        var actualCompany1SugarQuantity = Company1.GetInventory().GetInventoryEntries().FirstOrDefault(x => x.good == sugar && x.Cost==2.0m).quantity;
        var actualCompany2SugarQuantity = Company2.GetInventory().GetInventoryEntries().FirstOrDefault(x => x.good == sugar).quantity;
        // Assert
        Assert.AreEqual(expectedCompany1Cash, actualCompany1Cash);
        Assert.AreEqual(expectedCompany2Cash, actualCompany2Cash);
        Assert.AreEqual(expectedCompany1SugarQuantity, actualCompany1SugarQuantity);
        Assert.AreEqual(expectedCompany2SugarQuantity, actualCompany2SugarQuantity);
    }
    [Test]
    public void ACompanyCannotSellAGoodIfItHasInsufficientInventory()
    {
        // Arrange
        var expected = LemonadeStandResultObject.Failure(ResultTypeEnum.InsufficientGoods, "").Result;
        var Company1BuysLemons = new Order(Company1, null, lemon, 10, 3.0m);
        var Company2SellsLemons = new Order(null, Company2, lemon, 10, 3.0m);
        Company2.GetInventory().Clear();

        // Act
        Company1.QueueOrder(CreateActionContext(Company1BuysLemons,TestMarket,0));
        var actual=Company2.QueueOrder(CreateActionContext(Company2SellsLemons,TestMarket,0)).Result;
        TestMarket.ProcessCompanyOrders();
        var marketTradesInPeriod = TestMarket.GetExecutionsInPeriod(0);
        
        // Assert
        Assert.AreEqual(expected, actual);
        Assert.AreEqual(0, marketTradesInPeriod.Count);
    }

    [Test]
    public void MarketsShouldIgnoreTradesWhereMarketIsTheBuyerWhenCallingProcessCompanyOrders()
    {
        // Arrange
        lemonade.IsProducedGood = true;
        var radioactiveLemonade = Good.CreateInstance("Radioactive Lemonade", band2, RarityEnum.Very_Rare);
        radioactiveLemonade.IsProducedGood = true;

        Company1.GetInventory().AddGood(new InventoryEntry(radioactiveLemonade, 10, 3.0m, Period));
        Company2.GetInventory().AddGood(new InventoryEntry(lemonade, 10, 3.0m, Period));

        var radioactiveLemonadeTrade = new Order(TestMarket, Company1, radioactiveLemonade, 10, 3.0m);
        var radioactiveLemonadeContext = new ActionContext{TradeToSubmit = radioactiveLemonadeTrade, MarketToSubmitTo = TestMarket, Period = Period};

        var Company1BuysLemonadeFromCompany2 = new Order( Company1,Company2, lemonade, 10, 3.0m);
        var Company2SellsLemonadeToCompany1 = new Order(Company1,Company2, lemonade, 10, 3.0m);

        var expectedFilledQuantityForRadioactiveLemonade = 0;
        var expectedFilledQuantityForLemonade = 10;

        // Act
        TestMarket.QueueMarketOrder(radioactiveLemonadeContext);
        Company1.QueueOrder(CreateActionContext(Company1BuysLemonadeFromCompany2,TestMarket,Period));
        Company2.QueueOrder(CreateActionContext(Company2SellsLemonadeToCompany1,TestMarket,Period));

        TestMarket.ProcessCompanyOrders();
        var actualFilledQuantityForRadioactiveLemonade = radioactiveLemonadeTrade.FilledQuantity;
        var actualFilledQuantityForLemonade = Company1BuysLemonadeFromCompany2.FilledQuantity;
        // Assert
        Assert.AreEqual(expectedFilledQuantityForRadioactiveLemonade, actualFilledQuantityForRadioactiveLemonade);
        Assert.AreEqual(expectedFilledQuantityForLemonade, actualFilledQuantityForLemonade);
    }
   #endregion

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(lemon);
        Object.DestroyImmediate(water);
        Object.DestroyImmediate(sugar);
        Object.DestroyImmediate(TestEconomy);
        Object.DestroyImmediate(test_initial_market);
        Company1 = null;
        Company2 = null;
        TestMarket = null;
    }
}