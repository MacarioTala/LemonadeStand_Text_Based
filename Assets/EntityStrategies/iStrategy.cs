using System;
using System.Collections.Generic;

public interface iStrategy
{
    void GenerateGoals(iEconAgent company);
    decimal GetAggressionLevel();//Aggression level is a number from 0 to 1 determining how much 'extra' a population is willing to pay for the good
    LemonadeStandResultObject SetAggressionLevel(decimal aggressionLevel);
    public void PerformStrategy(ActionContext context);
    public void PerformStrategy(iEconAgent company);

    #region Default implementations
    /// <summary>
    /// The intention here is to have these be mostly static methods
    /// that can be used by any strategy.
    /// </summary>

    public static decimal GetCostAnchoredBid(Good good, EconAgent company)
    {
        var market = company.GetMarket();
        decimal perceivedCost;
        if (market != null)
        {
            var averagePrices = market.GetAverageMarketPrices();
            perceivedCost = company.GetPerceivedCostOfGood(good, averagePrices);
        }
        else
        {
            perceivedCost = company.GetMarketIgnorantAssumedCOG();
        }
        
        var initialBid = perceivedCost;
        return initialBid;
    }
    public static int GetQuantityDemandedAtState(Good good, EconAgent company, Dictionary<ElasticDemandComponentEnum, float> stateChanges)
    {
        var demand = company.GetDemandFor(good);
        var market = company.GetMarket();

        if (demand == null || demand.MaxDemand <= 0)
        {
            return 0; // No demand for this good
        }

        // If no other demand component evaluates
        // to other than 0, then we return the minimum demand
        var demandToReturn = demand.CurrentDemand>0?demand.CurrentDemand:demand.MinDemand;

        //Get Perceived cost of good
        throw new NotImplementedException("this damn thing is still not implemented");

        //Apply state changes
        //Note: We're going to apply a delay effect here in the future.
        //      This was originally going to be a period to period adjuster for quantity demanded at price
        //      but now it might be ok to use it to come up with the initial bid
        //      where the only state change is price (from perceived cost of good to current cost )

        return demandToReturn;
    }
    #endregion
}
    