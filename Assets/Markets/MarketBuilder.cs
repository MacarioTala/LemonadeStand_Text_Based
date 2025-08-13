using System.Collections.Generic;
using UnityEngine;

public static class MarketBuilder
{
    public static Market WithDemandStrategy(this Market market, iDemandStrategy strategy)
    {
        market.SetDemandStrategy(strategy);
        return market;
    }
    public static Market WithDataService(this Market market, iMarketDataService dataService)
    {
        market.SetMarketDataService(dataService);
        return market;
    }
    public static Market WithDemographicManager(this Market market, iDemographicManager demographicManager)
    {
        market.SetDemographicManager(demographicManager);
        demographicManager?.SetMarket(market);
        return market;
    }

    public static Market WithPriceManager(this Market market, iPriceManager priceManager)
    {
        market.SetPriceManager(priceManager);
        return market;
    }
    public static Market WithSupplyProvider(this Market market, iSupplyProvider supplyProvider)
    {
        market.SetSupplyProvider(supplyProvider);
        return market;
    }
    
    public static Market WithTradeProcessor(this Market market, iTradeProcessor tradeProcessor)
    {
        market.SetTradeProcessor(tradeProcessor);
        return market;
    }
    public static Market WithTransactionManager(this Market market, iTransactionManager transactionManager)
    {
        market.SetTransactionManager(transactionManager);
        return market;
    }

    public static Market WithOrderFulfilledEvents(this Market market)
    {
        if(market.DemandStrategy == null)
        {
            Debug.LogError("Market.DemandStrategy not set, cannot wire orderFulfilled event handler.");
            return market;
        }
        
        market.OrderFulfilled += market.DemandStrategy.OnOrderFulfilled;
        return market;
    }
    public static Market WithCash(this Market market, decimal cash)
    {
        market.SetCash(cash);
        return market;
    }
    public static Market WithMarketDataManager(this Market market, iMarketDataManager marketDataManager)
    {
        market.SetMarketDataManager(marketDataManager);
        return market;
    }

    public static Market WithPriceModifier(this Market market, iPriceModifier priceModifier)
    {
        market.AddPriceModifier(priceModifier);
        return market;
    }

    public static Market WithMarketStrategy(this Market market, iStrategy strategy)
    {
        market.SetStrategy(strategy);
        return market;
    }

    public static Market WithLevel(this Market market, AgentLevelEnum level)
    {
        market.company_level = level;
        return market;
    }
    public static Market Named(this Market market, string name)
    {
        market.Name = name;
        return market;
    }

    public static Market PopulatedWith(this Market market, List<PopulationAgent> marketParticipants)
    {

        foreach  (var particpant in marketParticipants)
        {
            market.RegisterMarketParticipant(particpant);
        }
        return market;
    }

    public static Market InitializedWith(this Market market, iMarketInitializer initializer)
    {
        if (initializer == null) 
        {
            Debug.LogError("MarketBuilder: initializer is null, cannot initialize");
            return market;
        }
        initializer.InitializeMarket(market);
        return market;
    }
}