using System;
using System.Collections.Generic;

public class PopulationAgent : EconAgent
{

    #region Demographics
    private int _population;

    private float _ennui;

    public bool IsMaxEnnui() => _ennui == 100;
    public float Ennui
    {
        get => _ennui;
        set
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Ennui must be between 0 and 1.");
            _ennui = value;
        }
    }
    public int Population
    {
        get => _population;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Population cannot be negative.");
            _population = value;
        }
    }
    #endregion

    private PopulationAgent() { }

    public static class PopulationAgentBuilder
    {
        public static EconAgentBuilder<PopulationAgent> Create()
                             => EconAgentBuilder.For<PopulationAgent>();
    }

    #region Consumption
    public void Consume()
    {
        var inventory = GetInventory().GetInventoryEntries();
        foreach (var inventoryEntry in inventory)
        {
            var good = inventoryEntry.good;
            var demand = GetDemandFor(good);

            var idealAmountConsumed = (int)Math.Floor(demand.ConsumptionRate * Population);
            var amountToConsume = idealAmountConsumed;
            if (amountToConsume > inventoryEntry.quantity)
            {
                amountToConsume = inventoryEntry.quantity;
            }
            inventoryEntry.quantity -= amountToConsume;

            var percentageOfIdealAmountConsumed = idealAmountConsumed==0?0f: (float)amountToConsume / idealAmountConsumed;
            inventoryEntry.good.ApplyEffects(this,percentageOfIdealAmountConsumed); 
        }
    }
    #endregion
    #region Demand
    
    public LemonadeStandResultObject InitializeDemandBasedOnPopulation
                                                (
                                                    Good good,
                                                    float percentOfPopulation,
                                                    int minDemand = 0,
                                                    int maxDemand = int.MaxValue,
                                                    List<ElasticDemandComponent> elasticDemandComponents = null,
                                                    float consumptionRate = 1f)
    {
        if (Population <= 0)
            return LemonadeStandResultObject.Failure("Population must be greater than zero to initialize demand.");

        var market = GetMarket();
        if (market == null)
            return LemonadeStandResultObject.Failure("Market is not set for this company.");

        var marketPrices = market.GetAverageMarketPrices();
        var perceivedCost = GetPerceivedCostOfGood(good,marketPrices);

        var initialDemand = (int)Math.Floor(Population * percentOfPopulation);

        var demand = new DemandData
        {
            InitialDemand = initialDemand,
            CurrentDemand = initialDemand,
            InitialPrice = perceivedCost,
            MinDemand = minDemand,
            MaxDemand = maxDemand,
            ConsumptionRate = consumptionRate // Default consumption rate is 1 per population unit
        };

        if (elasticDemandComponents != null)
        {
            demand.ElasticDemandComponents = elasticDemandComponents;
        }
        SetDemand(good, demand);
        return LemonadeStandResultObject.Success();
    }
    public LemonadeStandResultObject AddElasticDemandComponent
                                                (
                                                    Good good,
                                                    ElasticDemandComponentEnum type,
                                                    float elasticity,
                                                    float minPercentageChange = float.MinValue,
                                                    float maxPercentageChange = float.MaxValue,
                                                    float blackSwanToZeroLevel = float.MinValue,
                                                    float blackSwanToVerticalLevel = float.MinValue)
    {
        var demand = GetDemandFor(good);
        if (demand == null)
            return LemonadeStandResultObject.Failure("Demand for the good is not initialized.");
        var component = new ElasticDemandComponent
        {
            Type = type,
            Elasticity = elasticity,
            MinPercentageChange = minPercentageChange,
            MaxPercentageChange = maxPercentageChange,
            BlackSwanToZeroLevel = blackSwanToZeroLevel,
            BlackSwanToVerticalLevel = blackSwanToVerticalLevel
        };

        demand.ElasticDemandComponents.Add(component);
        return LemonadeStandResultObject.Success();
    }
    #endregion
}