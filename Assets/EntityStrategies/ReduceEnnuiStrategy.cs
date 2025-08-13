using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
public class ReduceEnnuiStrategy : iStrategy
{
    //Manifestation of preferences
    readonly Dictionary<Good,BidAskSpread> _bidAskSpreads = new();
    decimal _aggressionLevel = 0.2m;
    
    public decimal GetAggressionLevel() => _aggressionLevel;
    public LemonadeStandResultObject SetAggressionLevel(decimal aggressionLevel)
    {
        if (aggressionLevel < 0 || aggressionLevel > 1)
            return LemonadeStandResultObject.Failure("Aggression level must be between 0 and 1.");
        _aggressionLevel = aggressionLevel;
        return LemonadeStandResultObject.Success();
    }
#region Goals
    public void GenerateGoals(iEconAgent company)
    {
        var ennuiGoal = new Goal()
                .Named("Reduce Ennui")
                .DescribedAs("Reduce the ennui of the population to 0")
                .WithGoalEvaluator(c => c is PopulationAgent populationCompany && populationCompany.Ennui == 0)
                .WithGoalInitializer((c, g) => g.SetOriginalValue("Ennui", ((PopulationAgent)c).Ennui))
                .Affecting(MetricEnum.Ennui)
                .WithGoalValue(0f);
        
        AddGoal(ennuiGoal, company);
    }

    public void AddGoal(Goal goal, iEconAgent company)
    {
        if (company.Goals.Contains(goal))
        {
            company.Goals.Remove(goal);
        }
        company.Goals.Add(goal);
    }
    public void RemoveGoals(string goalName, iEconAgent company)
    {
        var goal = company.Goals.Find(g => g.Name == goalName);
        if (goal != null)
        {
            company.Goals.Remove(goal);
        }
    }

    private static bool GoalNotMet(Goal goal, iEconAgent company) =>
    !(goal?.IsGoalMet(company) ?? false);

    #endregion
    #region Strategy Execution
    public void PerformStrategy(iEconAgent company)
    {
        var ennuiGoal = company.Goals.Find(g => g.Name == "Reduce Ennui");
        if (GoalNotMet(ennuiGoal, company) && company is PopulationAgent populationCompany)
        {
            PerformStrategicActions(populationCompany);
        }
    }
    private void PerformStrategicActions(PopulationAgent company)
    {
        var market = company.GetMarket();
        var marketSpreads = market.GetBidAskSpreadsFromMarket();
        GenerateBidAskSpreads(company);
        foreach (var action in CreateBuys(company))
        {
            company.QueueOrder(action);
        }
    }
     private void AllocateBudget(iEconAgent company)
    {
        var aggressionLevel = company.GetAggressionLevel();
        var totalCash = company.GetCash()*aggressionLevel;
        var goodsToAllocateBudgetTo = _bidAskSpreads.ToList();

        var totalScore = goodsToAllocateBudgetTo.Sum(x => x.Value.GoodQuality);

        var allocations = goodsToAllocateBudgetTo
            .Select(x =>
            {
                var good = x.Key;
                var proportion = x.Value.GoodQuality / totalScore;
                var updatedSpread =
                        new BidAskSpread(
                            bid: x.Value.Bid,
                            goodQuality: x.Value.GoodQuality,
                            ask: x.Value.Ask,
                            allocation: (float)proportion);
                return (Good: good, Spread: updatedSpread);
            }
                ).ToList();

        foreach (var allocation in allocations)
        {
            _bidAskSpreads[allocation.Good] = allocation.Spread;
        }
    }
    
    internal IEnumerable<ActionContext> CreateBuys(EconAgent company)
    {
        var actions = new List<ActionContext>();
        foreach(var spread in _bidAskSpreads)
        {
            var market = company.GetMarket();
            var period = market != null ? market.CurrentPeriod : 0;
            var good = spread.Key;
            var bid = Math.Max(spread.Value.Bid, company.GetMinimumBid());
            var aggressionLevel = company.GetAggressionLevel();
            var cash = company.GetCash()*aggressionLevel;
            var populationCompany = company as PopulationAgent;
            var maxDemandForGood = populationCompany.GetDemandFor(good).MaxDemand;

            //prioritize goods to bid for
            var cashAvailableForBuys = Math.Floor(cash * (decimal)spread.Value.Allocation);
            var quantity = Math.Min(
                            (int)Math.Floor(cashAvailableForBuys / bid),
                            maxDemandForGood
                                );

            var order = new Order(company,null,good,quantity,bid);
        
            var context = new ActionContextBuilder()
                .WithAction(ActionEnum.QueueTradeBuy)
                .ForMarket(company.GetMarket())
                .ForPeriod(period)
                .WithTrade(order)
                .Build();
            actions.Add(context);
        }
        return actions;
    }
    #endregion
    #region BidAskSpreads
    public Dictionary<Good, BidAskSpread> GetBidAskSpreads()
                        => new(_bidAskSpreads);
    
    internal Dictionary<Good,BidAskSpread> GenerateBidAskSpreads(iEconAgent company)
    {
        if (company is PopulationAgent populationCompany)
        {
            var demand = populationCompany.GetDemand()
                .Where(
                    x => x.Value.MaxDemand > 0 //Can't demand goods that can't be bought(embargo, etc.)
                    &&
                    (
                        x.Key.AffectsMetrics().Contains(MetricEnum.Ennui)
                        ||
                        x.Value.MinDemand > 0
                    )
                );
                
            var goal = populationCompany.Goals.Find(g => g.Name == "Reduce Ennui");

            foreach (var row in demand)
            {
                var spread = CalculateBidPerCapita(row.Key, populationCompany,goal);
                spread.Ask = 0m;//for now, the population is not selling anything
                AddOrReplaceBidAskSpread(row.Key,spread);
            }
            if(demand.Count()>0)
                AllocateBudget(company);
        };
        return _bidAskSpreads;
    }

    internal BidAskSpread CalculateBidPerCapita(Good good,
                                                PopulationAgent populationCompany,
                                                Goal goal)
    {
        //Ability
        var availableCashPerCapita = populationCompany.GetCash()/populationCompany.Population;
        var willingnessToSpend = availableCashPerCapita * _aggressionLevel;

        //Desire
        var ennui = populationCompany.Ennui;
        var targetEnnui = goal.MetricTarget;
        var costAnchoredBid = iStrategy.GetCostAnchoredBid(good, populationCompany);

        //Calculate Good Quality
        var impactOnEnnui = Math.Abs(good.ReducesMetric(MetricEnum.Ennui).By());
        var ennuiReductionRate = ennui*impactOnEnnui;
        var turnsToFinal = MathHelper.EstimatePeriodsToFinal(ennui, targetEnnui, ennuiReductionRate);
        var goodQuality = 1f/(1f + turnsToFinal);

        //Calculate Minimum Bid
        var minBid = Math.Clamp(willingnessToSpend * (decimal)goodQuality, costAnchoredBid, availableCashPerCapita);

        //Calculate Bid Premium or discount
        var market = populationCompany.GetMarket();
        IEnumerable<(Good Good, decimal Bid, decimal Ask)> marketSpreads;
        decimal minMarketAsk = 0m;
        if (market != null)
        {
            marketSpreads = market.GetBidAskSpreadsFromMarket()
            .Where(x => x.good.Equals(good));

            if (marketSpreads.Any())
            {
                minMarketAsk = marketSpreads
                .Where(x => x.Ask > 0)
                .Select(x => x.Ask)
                .Min();
            }
        }
        var bid = minMarketAsk > minBid && minMarketAsk > 0
            ? minMarketAsk
            : minBid;

        return new BidAskSpread(bid: bid, goodQuality: goodQuality, ask: 0m, allocation: 0f);
    }
    #endregion
   

    #region Interactions With Market

    public void AddOrReplaceBidAskSpread(Good good, BidAskSpread bidAskSpread)
    {
        _bidAskSpreads[good] = bidAskSpread;
    }
    public BidAskSpread? GetBidAskSpreadForGood(Good good)
    {
        if (_bidAskSpreads.TryGetValue(good, out var spread))
        {
            return spread;
        }
        return null;
    }

    
#endregion

    public void PerformStrategy(ActionContext context)
    {
        throw new NotImplementedException();
    }
}
