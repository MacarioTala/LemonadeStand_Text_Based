using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using static TestHelpers;

[TestFixture]
public class BasicConsumptionManagerTests
{
    TheEconomy TestEconomy;
    Market TestMarket;
    iDemandStrategy TestDemandStrategy ;
    iMarketDataService TestMarketDataService;
    iDemographicManager TestDemographicManager;
    iSupplyProvider TestSupplyProvider;
    const int Period = 0;

    EconAgent Company1;
    EconAgent Company2;

    Good lemon;
    Good water;
    Good sugar;
    Good lemonade;

    Recipe lemonadeRecipe;
    readonly List<Good> testGoods = new();

    //Pricing bands
    readonly PriceBand band1 = new(.5m, 2m);
    readonly PriceBand band2 = new(2.1m, 3m);
    readonly PriceBand band3 = new(3.1m, 6m);

    
    [SetUp]
    public void SetUp()
    {
        TheEconomy.SetupForTests(new MockLogger());
        TestEconomy = TheEconomy.Instance;

        SetupGoodsAndRecipes();

        TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        TestMarketDataService = new MockMarketDataService();
        TestDemographicManager = new MockDemographicManager();
        TestSupplyProvider= new MockSupplyProvider();
        TestMarket = Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
                            .WithDemandStrategy(TestDemandStrategy)
                            .WithTradeProcessor(new BasicTradeProcessor())
                            .WithTransactionManager(new BasicTransactionManager())
                            .WithPriceManager(new BasicPriceManager())
                            .WithDataService(TestMarketDataService)
                            .WithSupplyProvider(TestSupplyProvider)
                            .WithDemographicManager(TestDemographicManager);
        TestMarket.InitializeDemandForSpecificGood(lemonade, 1000);

        Company1 = EconAgent.Factory.Create("Company1", AgentLevelEnum.Beginner);
        Company2 = EconAgent.Factory.Create("Company2", AgentLevelEnum.Beginner);
        TestMarket.RegisterMarketParticipant(Company1);
        TestMarket.RegisterMarketParticipant(Company2);
    }

    private void SetupGoodsAndRecipes()
    {
        water = Good.CreateInstance("Water", band1, RarityEnum.Common);
        sugar = Good.CreateInstance("Sugar", band1, RarityEnum.Common);
        lemon = Good.CreateInstance("Lemon", band2, RarityEnum.Common);
        lemonade = Good.CreateInstance("Lemonade", band3, RarityEnum.Uncommon);
        lemonadeRecipe = new Recipe(RecipeName: "Basic Lemonade",
                                     product: lemonade, 
                                     ingredients: new List<Ingredient> { new(lemon, 9), 
                                                                        new(sugar, 2), 
                                                                        new(water, 7) });                
        testGoods.Add(lemon);
        testGoods.Add(water);
        testGoods.Add(sugar);
        testGoods.Add(lemonade);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(lemon);
        Object.DestroyImmediate(water);
        Object.DestroyImmediate(sugar);
        Object.DestroyImmediate(lemonade);
        Object.DestroyImmediate(TestEconomy);
        TestMarket = null;
        Company1 = null;
        Company2 = null;
    }
}