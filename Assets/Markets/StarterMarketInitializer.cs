using System;
using System.Collections.Generic;

public class StarterMarketInitializer : iMarketInitializer
{
    public void InitializeMarket(Market market)
    {
        if (market == null) throw new ArgumentNullException(nameof(market));
        SeedWithInitialGoods(market);
        CreateStarterDemand(market);
        SetInitialCash(market);
    }
    private void SeedWithInitialGoods (Market market)
    {
        var _inventory = market.GetInventory();
        var Lemonade = Good.CreateInstance("Lemonade", new PriceBand(8.0m, 13.0m), RarityEnum.Uncommon);
        var Lemon = Good.CreateInstance("Lemon", new PriceBand(1.0m, 3.0m), RarityEnum.Common);
        var Sugar = Good.CreateInstance("Sugar", new PriceBand(1.0m, 2.0m), RarityEnum.Common);
        var Water = Good.CreateInstance("Water", new PriceBand(.5m, 1.0m), RarityEnum.Common);
        _inventory.AddGood(new InventoryEntry(Lemon, 10000, 2.0m, 0));
        _inventory.AddGood(new InventoryEntry(Sugar, 10000, 1.5m, 0));
        _inventory.AddGood(new InventoryEntry(Water, 10000, .75m, 0));
        var LemonadeRecipe = new Recipe(RecipeName: "Basic Lemonade",
                                        product: Lemonade,
                                        ingredients: new List<Ingredient> { new(Lemon, 9),
                                                                           new(Sugar, 2),
                                                                           new(Water, 7) });
        market.AddRecipe(LemonadeRecipe);
    }

    private void SetInitialCash(Market market)
    {
        var company_level = market.company_level;
        switch(company_level)
        {
            case AgentLevelEnum.Beginner:
                market.SetCash(10000);
                break;
            case AgentLevelEnum.Intermediate:
                market.SetCash(5000);
                break;
            case AgentLevelEnum.Advanced:
                market.SetCash(1000);
                break;  
            case AgentLevelEnum.Market:
                market.SetCash(1000000000000);
                break;
        }
    }
    private void CreateStarterDemand (Market market)
    {
        //Initialize demand data
        //If no demand data is passed, demand defaults to 1000 units of Lemonade
        //This is a placeholder and will be replaced with a more sophisticated system
        var lemonade = Good.CreateInstance("Lemonade", new PriceBand(8.0m, 13.0m), RarityEnum.Uncommon);
        lemonade.IsProducedGood = true;
        market.InitializeDemandForSpecificGood(lemonade, 1000);
    }
}
