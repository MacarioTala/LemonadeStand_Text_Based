using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
[TestFixture]
public class GameOfLifeTest
{
    private TheEconomy LemonadeEconomy;
    private int TotalNumberOfCycles;
    private int TradesPerCycle;

    private Market LemonadeMarket;

    private readonly List<EconAgent> Companies = new();

    private Good Lemon;
    private Good Water;
    private Good Sugar;
    private Good Lemonade;

    private Recipe LemonadeRecipe;
    private readonly List<Good> TestGoods= new();

    [SetUp]
    public void SetUp()
    {
        // Create the economy
        TheEconomy.SetupForTests(new MockLogger());
        LemonadeEconomy = TheEconomy.Instance;

        // Fill it with goods
        MakeGoodsAndRecipes();

        // get the first market ready
        InitializeFirstMarket();

        // create companies and initialize them with cash and inventories
        CreateTestCompanies();

        TotalNumberOfCycles = 10;
        TradesPerCycle = 10;

    }

    private void MakeGoodsAndRecipes()
    {
        Lemon = Good.CreateInstance("Lemon", new PriceBand(1.0m, 3.0m), RarityEnum.Common);
        Water = Good.CreateInstance("Water", new PriceBand(1.0m, 1.0m), RarityEnum.Common);
        Sugar = Good.CreateInstance("Sugar", new PriceBand(1.0m, 2.0m), RarityEnum.Common);
        Lemonade = Good.CreateInstance("Lemonade", new PriceBand(4.0m, 5.0m), RarityEnum.Uncommon);
        TestGoods.Add(Lemon);
        TestGoods.Add(Water);
        TestGoods.Add(Sugar);
        TestGoods.Add(Lemonade);

        //Recipes
        var lemonIngredient = new Ingredient(Lemon, 1);
        var waterIngredient = new Ingredient(Water, 5);
        var sugarIngredient = new Ingredient(Sugar, 2);
        LemonadeRecipe = new Recipe(RecipeName:"Basic Lemonade",
                                    product:Lemonade, 
                                    ingredients:new List<Ingredient> { lemonIngredient, waterIngredient, sugarIngredient });
    }

    private void CreateTestCompanies()
    {
        //make a variable number of companies and give them varying amounts of goods and cash
        var random = new System.Random ();
        var numberOfCompanies = random.Next(1, 10);
        for (int i = 0; i < numberOfCompanies; i++)
        {
            var company = EconAgent.Factory.Create("Company" + i, AgentLevelEnum.Beginner);
            TheEconomy.Instance.RegisterCompany(company);
            Companies.Add(company);
        }
        foreach (var company in Companies)
        {
            foreach (var good in TestGoods)
            {
                company.BuyGood(good, random.Next(0, 1000), good.GetPrice());
            }
        }
            
    }

    private void InitializeFirstMarket()
    {
        //Get a reference to the company's market
        //Give it some goods
        //Let it demand Lemonade, Lemons, etc
        LemonadeMarket = (Market)LemonadeEconomy.GetGlobalMarket();
       
       foreach (var good in TestGoods)
        {
            LemonadeMarket.InitializeDemandForSpecificGood(good, Random.Range(100, 1000));
            LemonadeMarket.GetInventory().AddGood(new InventoryEntry(good,10000,.5m,0));
        }
    }
    [Test]
    public void SimulateRandom()
    {
        for (int cycle = 0; cycle < TotalNumberOfCycles; cycle++)
        {
            Debug.Log("Starting cycle:" + cycle);
            for (int i = 0; i < TradesPerCycle; i++) 
            {
                PerformRandomAction(cycle);
            }
            try{
                LemonadeMarket.ProcessCompanyOrders();
                }
            catch (System.Exception e)
            {
                Debug.Log("Error in cycle:" + cycle + " " + e.Message);
            }

            Debug.Log("Cycle:" + cycle + " completed");
        }

       //GenerateSummary();
    }

    private void PerformRandomAction(int cycle)
    {
        var randomAction = Random.Range(0, 2);
        if (randomAction == 0)
            PerformRandomTrade(cycle);
        else
            PerformRandomProduction(cycle);
    }

    private void PerformRandomProduction(int cycle)
    {
        Debug.Log("Performing random production");
    }

    private void PerformRandomTrade(int cycle)
    {
        var potentialSellers = new List<iEconAgent>(Companies) { LemonadeMarket };
        var potentialBuyers = new List<iEconAgent>(Companies) { LemonadeMarket };

        var buyer = potentialBuyers[Random.Range(0, potentialBuyers.Count)];
        var seller = potentialSellers[Random.Range(0, potentialSellers.Count)];

        if (buyer == seller) return;

        var goodToBuy = SelectRandomGood();
        if (goodToBuy == null) return;

        var doesSellerHaveRandomGood = seller.GetInventory().GetInventoryEntries().Exists(entry => entry.good == goodToBuy);
        if (!doesSellerHaveRandomGood) return;

        var quantity = Random.Range(1, 10);
        var price = goodToBuy.GetPrice();
        var trade = new Order(buyer, seller, goodToBuy, quantity, price);
        var context = new ActionContext { TradeToSubmit = trade, MarketToSubmitTo = LemonadeMarket };
        LemonadeMarket.QueueOrder(context);
        Debug.Log("Trade queued in cycle: " + cycle + " " + trade);
    }

    private Good SelectRandomGood()
    {
        return TestGoods[Random.Range(0, TestGoods.Count)];
    }
    
    [TearDown]
    public void Teardown()
    {
        // Clean up the test GameObject after each test
        Object.DestroyImmediate(LemonadeMarket);
        Object.DestroyImmediate(LemonadeEconomy.gameObject);
    }
}
