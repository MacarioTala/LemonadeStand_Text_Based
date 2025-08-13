using System;
using System.Collections.Generic;
using System.Linq;

public interface iDemandStrategy
{
    public const int MinDemand = 0;
    public const int MaxDemand = 10000;
    LemonadeStandResultObject AdjustDemandInPeriod(Market market);
    [Obsolete("refactor this out. Demand is now driven by population and not the market.")]
    void InitializeDemandForSpecificGood(Market market, Good good, int initialDemand, int minDemand = MinDemand, int maxDemand = MaxDemand, float curvature = 1f);
    void OnOrderFulfilled(OrderFulfilledEvent orderFulfilledEvent);
#region Default Implementations
    
    public Dictionary<Good, DemandData> GetDemandInPeriod(Market market, int tradingPeriod)
    {
        Dictionary<Good, DemandData> calculatedDemand = new();
        var orders = market.GetOrdersSubmittedInPeriod(tradingPeriod);
        var allOrdersInPeriod = market.GetOrdersSubmittedInPeriod(tradingPeriod)
            .Where(x => x.Buyer.Equals(x.SubmittingCompany))
            .ToList();
        foreach (var order in allOrdersInPeriod)
        {
            var good = order.Good;
            var quantity = order.Quantity;

            if (calculatedDemand.ContainsKey(good))
            {
                calculatedDemand[good].CurrentDemand += quantity;
            }
            else
            {
                calculatedDemand[good] = new DemandData
                {
                    CurrentDemand = quantity
                };
            }
        }
        return calculatedDemand;
    }
    public Dictionary<Good, int> CalculateSupplyForPeriod(Market market, int tradingPeriod)
    {
        Dictionary<Good, int> calculatedSupply = new();
        var executedTrades = market.GetExecutionsInPeriod(tradingPeriod)
                                   .Where(x => x.RecordedTrade.IsSell());

        foreach (var trade in executedTrades)
        {
            if(calculatedSupply.ContainsKey(trade.RecordedTrade.Good))
            {
                calculatedSupply[trade.RecordedTrade.Good] += trade.RecordedTrade.Quantity;
            }
            else
            {
                calculatedSupply.Add(trade.RecordedTrade.Good, trade.RecordedTrade.Quantity);
            }
        }
        return calculatedSupply;
    }

    public int GetTotalBoughtByPopulation(Market market,Good good,int tradingPeriod)
    {
        var populationOrders = market.GetOrdersSubmittedInPeriod(tradingPeriod)
            .Where(x => x.Good.Equals(good))
            .Where(x => x.Buyer is PopulationAgent)
            .ToList();
        var TotalBought = populationOrders
            .Sum(x => x.FilledQuantity);
        return TotalBought;
            
    }

    public int GetTotalSoldByMarket(Market market,int tradingPeriod, Good good) //currently public for testing purposes
    {
        return market.GetExecutionsInPeriod(tradingPeriod)
            .Where(x => x.Period == tradingPeriod
                        && x.RecordedTrade.Good.Equals(good)
                        && x.RecordedTrade.Seller is Market
                        )
            .Sum(x => x.RecordedTrade.Quantity);
    }
#endregion
}