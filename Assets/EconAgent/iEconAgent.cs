using System.Collections.Generic;

public interface iEconAgent
{
    #region Identity
        string Name{get;set;}
    #endregion

    #region Financials
        List<FixedCost> FixedCosts {get;set;}    
        iFixedCostStrategy FixedCostStrategy{get;set;}
        decimal GetCash ();
        void SetCash(decimal newCash);
        decimal CalculateFixedCostsForPeriod(int period);
    #endregion

    #region Demand
        DemandData GetDemandFor(Good good);
    #endregion

    #region Perception

    #endregion

    #region Goals and strategies
    List<Goal> Goals{get;set;}
        void CheckCompanyGoals();
        void CompleteGoal(Goal goal);
    decimal GetAggressionLevel();
    void SetAggressionLevel(decimal aggressionLevel);
        void SetStrategy(iStrategy strategy);
        iStrategy GetStrategy();
    #endregion
    
    #region Inventory Management
        void ExpireGoods(int period);
        Inventory GetInventory();
    #endregion

    #region Time
    int CurrentPeriod{get;set;} 
        int StartingPeriod{get;set;}
        void UpdateCurrentPeriod(int period);
    #endregion
    
    #region Trading
        LemonadeStandResultObject QueueOrder(ActionContext context);
    #endregion
}