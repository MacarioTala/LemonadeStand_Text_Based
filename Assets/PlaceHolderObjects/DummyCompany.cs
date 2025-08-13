using System.Collections.Generic;
using UnityEngine;

public class DummyCompany : iEconAgent
{
    public string Name {get; set;}="Raw Materials Source";
    public decimal InfiniteCash => 10000000;
        
    public Inventory Inventory{get; private set;} = new Inventory();

    public DummyCompany()
    {
        foreach (var good in new List<Good> {Good.CreateInstance("Lemon", new PriceBand(.5m, 1.0m), RarityEnum.Common),
                                             Good.CreateInstance("Water", new PriceBand(.5m, 1.0m), RarityEnum.Common),
                                             Good.CreateInstance("Sugar", new PriceBand(.5m, 1.0m), RarityEnum.Common),
                                             Good.CreateInstance("Lemonade", new PriceBand(1.0m, 3.0m), RarityEnum.Uncommon)})
        {
            Inventory.AddGood(new InventoryEntry(good, 1000000, 1, 0));
        }
    }

    public decimal GetCash()
    {
        return InfiniteCash;
    }

    public List<FixedCost> FixedCosts { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public iFixedCostStrategy FixedCostStrategy { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public List<Goal> Goals { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public int CurrentPeriod { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public int StartingPeriod { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public void BuyGood(Good good, int quantity, decimal price, int period)
    {
        throw new System.NotImplementedException();
    }

    public decimal CalculateFixedCostsForPeriod(int period)
    {
        throw new System.NotImplementedException();
    }

    public void CheckCompanyGoals()
    {
        throw new System.NotImplementedException();
    }

    public void CompleteGoal(Goal goal)
    {
        throw new System.NotImplementedException();
    }

    public void ExpireGoods(int period)
    {
        throw new System.NotImplementedException();
    }

    public Inventory GetInventory()
    {
        return Inventory;
    }

    public LemonadeStandResultObject QueueOrder(ActionContext context)
    {
        throw new System.NotImplementedException();
    }

    public void SellGood(Good good, int quantity, decimal price, int period)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateCurrentPeriod(int period)
    {
        throw new System.NotImplementedException();
    }

    public void SetCash(decimal newCash)
    {
        Debug.Log("Null cash transaction for dummy company");
    }

    public void SetStrategy(iStrategy strategy)
    {
        throw new System.NotImplementedException();
    }

    public iStrategy GetStrategy()
    {
        throw new System.NotImplementedException();
    }

    public DemandData GetDemandFor(Good good)
    {
        throw new System.NotImplementedException();
    }

    public void SetAggressionLevel(decimal aggressionLevel)
    {
        throw new System.NotImplementedException();
    }

    public decimal GetAggressionLevel()
    {
        throw new System.NotImplementedException();
    }
}
