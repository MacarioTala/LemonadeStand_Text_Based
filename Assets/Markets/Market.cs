using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
[assembly: InternalsVisibleTo("Tests")]
[CreateAssetMenu(fileName = "Market", menuName = "LemonadeStandAssets/Market", order = 1)]
public class Market : ScriptableObject, iCompany
{
#region Fields, Properties

    [SerializeField] private string _marketId;
    public Guid MarketId 
    {
        get
        {
            if (_marketId == null)
            {
                _marketId = Guid.NewGuid().ToString();
         
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
            return Guid.Parse(_marketId);
        }
    }
    public List<MarketFeature> MarketFeatures = new();
    public List<MarketFeature> GetMarketFeatures() => MarketFeatures;
    public (int x,int y) MarketSize = (200,200);
    public int GetWidth() => MarketSize.x;
    public int GetHeight() => MarketSize.y;
    //Fields to get around Unity's limitation of not having automatic backing properties.
    [SerializeField] private string _companyName;
    public string Name
    {
        get => _companyName;
        set => _companyName = value;
    }
    public CompanyLevelEnum company_level;
    //Demand
    public List<MarketData> MarketData = new();//bid/ask spread for companies
    private readonly Dictionary<Good, DemandData> _marketDemand = new();

    //Cash and Inventory
    private decimal cash = 0;
    private readonly Inventory _inventory = new();
    private readonly List<Recipe> _recipes = new();

    //Companies
    private readonly List<Company> _marketParticipants = new();
    public List<Company> GetMarketParticipants() => _marketParticipants;
    public void RegisterMarketParticipant(Company marketParticipant)
    {
        if(!_marketParticipants.Contains(marketParticipant))
        {
            _marketParticipants.Add(marketParticipant);
            marketParticipant.SetMarket(this);
        }
        else
        {
            throw new TheEconomy_CompanyException($"Company {marketParticipant.Name} of type {marketParticipant.GetType()} already in Market {MarketId}");
        }
        TheEconomy.Instance.RegisterCompany(marketParticipant);
    }
    public LemonadeStandResultObject RemoveMarketParticipant(Company company)
    {
        if(_marketParticipants.Contains(company))
        {
            _marketParticipants.Remove(company);
            return LemonadeStandResultObject.Success();
        }
        return LemonadeStandResultObject.Failure(ResultTypeEnum.CompanyNotFound, $"Company {company.Name} not found in Market {Name}");
    }
#endregion

#region Demographics
    public float GetMarketInstability() => _demographicManager.GetMarketInstability();
    public LemonadeStandResultObject SetMarketInstability(float newInstability)
        =>_demographicManager.SetMarketInstability(newInstability);
    public float GetPopulationEnnui() => _demographicManager.GetPopulationEnnui();
    public string GetEnnuiLevel() => _demographicManager.GetEnnuiLevel();
    //Population
    public int GetPopulation() => _demographicManager.GetPopulation();
    public List<PopulationHistory> GetPopulationHistory() => _demographicManager.GetPopulationHistory(MarketId);
   
    public LemonadeStandResultObject SetPopulation(int newPopulation, PopulationCompany marketParticipant) 
    {
        var actor = _marketParticipants
            .OfType<PopulationCompany>()
            .Where(x=>x.Equals(marketParticipant))
            .FirstOrDefault();
        if(actor == null)
        {
            return LemonadeStandResultObject.Failure(ResultTypeEnum.CompanyNotFound, $"Company {marketParticipant.Name} not found in Market {Name}");
        }
        _demographicManager.SetPopulation(newPopulation,actor);
        return LemonadeStandResultObject.Success();
    }

    public float GetPopulationGrowthRate()=>_demographicManager.GetPopulationGrowthRate(0,CurrentPeriod);

    //Population Happiness
    public float GetPopulationHappiness()=>_demographicManager.GetPopulationHappiness();
    public LemonadeStandResultObject SetPopulationHappiness(float newHappiness)=>
        _demographicManager.SetPopulationHappiness(newHappiness);
    
    public LemonadeStandResultObject RecordDemographicSnapshot(TurnPhase phase)
    {
        _demographicManager.RecordDemographicSnapshot(MarketId,CurrentPeriod,phase);
        return LemonadeStandResultObject.Success();
    }
#endregion

    //Event Handlers
    public delegate void OrderFulfillmentHandler(OrderFulfilledEvent orderFulfilledEvent);
    public event OrderFulfillmentHandler OrderFulfilled;
    public void RaiseOrderFulfilledEvent(OrderFulfilledEvent orderFulfilledEvent)
    {
        OrderFulfilled?.Invoke(orderFulfilledEvent);
    }

    //Goals
    public List<Goal> Goals {get;set;}
    private iStrategy _marketStrategy;
    public iStrategy GetStrategy() => _marketStrategy;
    public void SetStrategy(iStrategy strategy) => _marketStrategy = strategy;
    
#region Market Events
    public List<iMarketEvent> PotentialMarketEvents { get; } = new();
    readonly List<(iMarketEvent Event, int PeriodStart,int duration)> _activeEvents = new();
    readonly List<(iMarketEvent Event, int PeriodStart,int periodEnd)> marketEventHistory = new();
    public List<(iMarketEvent Event, int PeriodStart,int periodEnd)> GetMarketEventHistory() => marketEventHistory;
    public void AddPotentialMarketEvent(iMarketEvent marketEvent)
    {
        if(!PotentialMarketEvents.Contains(marketEvent))
        {
            PotentialMarketEvents.Add(marketEvent);
        }
    }
    public List<(iMarketEvent Event, int PeriodStart,int duration)> GetActiveMarketEvents() => _activeEvents;
    public void RemovePotentialMarketEvent(iMarketEvent marketEvent)
    {
        if(PotentialMarketEvents.Contains(marketEvent))
        {
            PotentialMarketEvents.Remove(marketEvent);
        }
    }
    public void ResolveMarketEvents()
    {
        //Check if any active events have expired
        var expiredEvents = _activeEvents
                            .Where(x=>x.Event.IsExpiredAt(x.PeriodStart,CurrentPeriod))
                            .ToList();
        foreach (var marketEvent in expiredEvents)
        {
            _activeEvents.Remove(marketEvent);
            marketEventHistory.Add((marketEvent.Event,marketEvent.PeriodStart,CurrentPeriod));
            marketEvent.Event.Reset();
        }
        
        //Invoke any active events
        foreach (var marketEvent in _activeEvents)
        {
            marketEvent.Event.Invoke(this);
        }
    }
    public void RollForEvents()
    {
        foreach (var marketEvent in PotentialMarketEvents)
        {
            if(_activeEvents.Any(x=>!x.Event.IsCompatibleWith(marketEvent))) continue;
            
            if(_activeEvents.Any(x=>x.Event.Equals(marketEvent))) continue;

            if(EventRollSucceeds(marketEvent))
            {
                _activeEvents.Add((marketEvent,CurrentPeriod,marketEvent.GetDuration()));
            }
        }
    }

    private static bool EventRollSucceeds(iMarketEvent marketEvent)
    {
        var currentRoll = UnityEngine.Random.Range(0, 100);
        var chanceOfEvent = marketEvent.GetProbabilityOf();

        return chanceOfEvent>=currentRoll;;
    }

#endregion
    //Pricing  
    public List<FixedCost> FixedCosts { get; set; }
    public iFixedCostStrategy FixedCostStrategy {get;set;}
    private readonly List<iPriceModifier> _priceModifiers = new();
    public void AddPriceModifier(iPriceModifier priceModifier)
    {
        if(!_priceModifiers.Contains(priceModifier))
        {
            _priceModifiers.Add(priceModifier);
        }
    }

    //Reporting 
    readonly List<(Order Order, int Period)> OrdersSubmittedInPeriod = new(); // Read only used to get Order History. 
    public List<Order> GetOrdersSubmittedInPeriod(int period)
    {
        var ordersToReturn = OrdersSubmittedInPeriod
                            .Where(x => x.Period == period)
                            .Select(x => x.Order)
                            .ToList();    
        return ordersToReturn;
    }
    
    public void LogOrder(Order order, int period)
    {
        if(!OrdersSubmittedInPeriod.Contains((order,period)))
        {
            OrdersSubmittedInPeriod.Add((order,period));
        }
    }

    //Time
    public int CurrentPeriod{get;set;}=0;
    public int StartingPeriod{get;set;}
    //Trading
    private readonly List<Execution> _executedTradesInPeriod = new();
    private readonly List<(Order Order,int Period)> _ordersExecutedInPeriod = new();
    public List<(Order Order,int Period)> GetOrdersExecutedInPeriod(params int[] periods) => _ordersExecutedInPeriod.Where(x=>periods.Contains(x.Period)).ToList();
    public List<Execution> GetExecutionsInPeriod(int period)
    {
      var executions =  _ordersExecutedInPeriod
                        .Where(x=>x.Period==period)
                        .SelectMany(x=>x.Order.GetExecutions()).ToList();
      return executions;
    }

#region Convenience Methods
    public decimal GetCash() => cash;
    public Inventory GetInventory() => _inventory;
    public List<Order> GetOrdersSentToMarket() => _tradesSentToMarket;
    public List<Order> GetOrdersSentToMarketByCompany(Company company) => _tradesSentToMarket.Where(x=>x.SubmittingCompany.Equals(company)).ToList();
    public List<Recipe> GetRecipes()=>_recipes;
    public Dictionary<Good, DemandData> GetPopulationDemand()
    {
        // Currently assumes that only one PopulationCompany
        // will exist in the market.
        // In the future, we'll need to know how to merge the different demands
        // for the same good across multiple market segments/populationCompanies.
        var participantDemand = _marketParticipants
               .OfType<PopulationCompany>()
               .SelectMany(x => x.GetDemand())
               .ToDictionary(x => x.Key, x => x.Value);

        return participantDemand;  
    }
    
    public void SetMarketDemandForGood(Good good, DemandData demandData) => _marketDemand[good] = demandData;
     
    public List<iPriceModifier> GetPriceModifiers() => _priceModifiers;

    public LemonadeStandResultObject RecordOrderInPeriod(Order order, int period)
    {
        if(!_ordersExecutedInPeriod.Contains((order,period)))
        {
            _ordersExecutedInPeriod.Add((order,period));
            return LemonadeStandResultObject.Success();
        }
        return LemonadeStandResultObject.Failure(ResultTypeEnum.DuplicateOrder,"Order already recorded");
    }
    public void RecordTrade(Execution trade) 
    {
        if(!_executedTradesInPeriod.Contains(trade))_executedTradesInPeriod.Add(trade);
    }
    public void RemoveFilledOrders()
    {
        _tradesSentToMarket.RemoveAll(x => x.IsFullyFilled);
    }
    public void SetCash(decimal new_cash) => cash = new_cash;
#endregion

#region Creation and Initialization
    //Instantiate Markets using a factory
    private Market ()
    {
        // Intentionally blank. Do not add a constructor.
        // We will use ScriptableObject.CreateInstance<Market>() to create instances of this class.
        // Market.Create() will be used as syntactic sugar for tests.
    }
    public static Market Create()
    {
        return CreateInstance<Market>();
    }

    public static class Factory
    { 
        public static readonly StarterMarketInitializer starterMarketInitializer = new();

        public static Market CreateMarket(string companyName, CompanyLevelEnum companyLevel)
        {
            var market = CreateInstance<Market>();
            market.Name = companyName;
            market.company_level = companyLevel;
            if(market==null)
            {
                throw new Exception("Market could not be created");
            }
            return market;
        }

        public static Market CreateStarterMarket(string companyName, CompanyLevelEnum companyLevel, iDemandStrategy demandStrategy)
        {
            var market = CreateInstance<Market>()
                    .WithDemandStrategy(demandStrategy)
                    .WithMarketDataManager(new BasicMarketDataManager())
                    .WithPriceManager(new BasicPriceManager())
                    .WithTradeProcessor(new BasicTradeProcessor())
                    .WithTransactionManager(new BasicTransactionManager())
                    .WithPriceModifier(new SupplyDemandModifier())
                    .WithOrderFulfilledEvents()
                    .Named(companyName)
                    .WithLevel(companyLevel)
                    .InitializedWith(starterMarketInitializer);
            
            return market;
        }
    }
    
#endregion
#region Managers
    public iDemographicManager DemographicManager { get => _demographicManager;}
    private iDemographicManager _demographicManager;
    public void SetDemographicManager(iDemographicManager demographicManager) => _demographicManager = demographicManager;
    private iFeatureManager _featureManager;
    public void SetFeatureManager(iFeatureManager featureManager) => _featureManager = featureManager;
    private iMarketDataManager _marketDataManager;
    public void SetMarketDataManager(iMarketDataManager marketDataManager) => _marketDataManager = marketDataManager;
    private iPriceManager _priceManager;
    public void SetPriceManager(iPriceManager priceManager) => _priceManager = priceManager;
    private iSupplyProvider _supplyProvider;
    public void SetSupplyProvider(iSupplyProvider supplyProvider) => _supplyProvider = supplyProvider;
    private iTradeProcessor _tradeProcessor;
    public void SetTradeProcessor(iTradeProcessor tradeProcessor) => _tradeProcessor = tradeProcessor;
    private iTransactionManager _transactionManager;
    public void SetTransactionManager(iTransactionManager transactionManager) => _transactionManager = transactionManager;
    private iMarketDataService _marketDataService;
    public void SetMarketDataService(iMarketDataService marketDataService) => _marketDataService = marketDataService;
    [SerializeField] private ScriptableObject _demandStrategy;
    public iDemandStrategy DemandStrategy {get => _demandStrategy as iDemandStrategy;}
    public void SetDemandStrategy(iDemandStrategy demandStrategy)
    { _demandStrategy = demandStrategy as ScriptableObject; } 

#endregion
#region Company Interactions
    public void BankruptCompany(Company company)
    {
        if(company.IsBankrupt())
        {
            TheEconomy.Instance.HandleBankruptcy(this,company);
        }
    }
    public void ProcessOrder(ActionContext context)
    {
        _transactionManager.ProcessTransaction(context);
    }
    public LemonadeStandResultObject ProcessMarketOrder(ActionContext context)
    {
        return _transactionManager.ProcessMarketTransaction(context);
    }
    public List<Order> ProcessCompanyOrders()
    {
        var CompanyOrdersExecuted = _tradeProcessor.ProcessCompanyOrders(this);
        return CompanyOrdersExecuted;
    }
     public LemonadeStandResultObject QueueOrder(ActionContext context)
    {
        var contextValidationResult = context.ContainsValidTrade();
        if ( !contextValidationResult.Equals(LemonadeStandResultObject.Success()) )
            return context.ContainsValidTrade();
        
        var queueResult = _tradeProcessor.QueueOrder(context);
        if ( !queueResult.Equals(LemonadeStandResultObject.Success()) )
            return queueResult;
        //Record the order
        LogOrder(context.TradeToSubmit, context.Period);
        
        _tradesSentToMarket.Add(context.TradeToSubmit);
        return LemonadeStandResultObject.Success();
    }
   
    internal void UpdateCompanyStatuses(int period)
    {
        foreach (var company in _marketParticipants)
        {
            //Update Company Statuses
            company.ExpireGoods(period);
            company.SubtractFixedCostsForPeriod(period);
            company.UpdateCurrentPeriod(period+1);
            BankruptCompany(company);
        }
    }
#endregion
#region Consumption and Demand

    public Dictionary<Good,DemandData> GetDemandForPeriod()
    {
        return DemandStrategy.GetDemandInPeriod(this,CurrentPeriod);
    }
    [Obsolete("remove this in a future refactor. PopulationCompanies should handle their own consumption")]
    internal void ConsumeGoods()
        {
            //attempt to consume goods at current demand levels
            foreach(var good in _marketDemand.Keys)
            {
                var demanded_quantity = _marketDemand[good].CurrentDemand;
                //consume good
                var unfulfilledDemand = _inventory.TryConsumeGood(good.GoodName,demanded_quantity);
                // Do something with unfulfilled demand later
            }
        }
    public void ExpireGoods(int period)
        {
            foreach(var company in _marketParticipants)
                company.GetInventory().ExpireGoods(period); 
        }
#endregion
#region Fixed costs
    public decimal CalculateFixedCostsForPeriod(int period)
    {
        return FixedCostStrategy.CalculateFixedCosts(FixedCosts,period);
    }
#endregion
#region Goals and strategies
    public void CompleteGoal(Goal goal)
        {
            throw new NotImplementedException();
        }

    public void CheckCompanyGoals()
        {
            throw new NotImplementedException();
        }
#endregion
#region History
    public float GetPopulationPercentageChangeInPeriod()
    {
        return MathHelper.GetMetricPercentageChangeInPeriod(
            _demographicManager.GetPopulationHistory,
            x=>x.Population,
            CurrentPeriod,
            MarketId);       
    }
#endregion
#region Inventory Management
    public void AddRecipe(Recipe recipe)
    {
        if(_recipes.Contains(recipe))
        {
            throw new Exception("Recipe already exists in company");
        }
        else
        {
            _recipes.Add(recipe);
        }
    }
#endregion
#region Time 
    public void UpdateCurrentPeriod(int period)
    {
        CurrentPeriod = period;
    }
#endregion
#region Buy and sell 
    public int GetTotalBoughtByMarket(int tradingPeriod, Good good)//Currently public for testing purposes
    {
       return DemandStrategy.GetTotalBoughtByPopulation(this,good,tradingPeriod);
    }
    public int GetTotalSoldByMarket(int tradingPeriod, Good good) //currently public for testing purposes
    {
        return DemandStrategy.GetTotalSoldByMarket(this,tradingPeriod,good);
    }
#endregion
#region Demand
     internal LemonadeStandResultObject UpdateFulfillmentRates(int tradingPeriod=-1)
    {
        // You are here: update this to update the supply of the good too
        // since CalculateFulfillmentRates already calculates total supply
        // Maybe there's no need for a supply provider?

        //Get the demand and supply for the period
        var ordersSubmittedInPeriod = GetOrdersSubmittedInPeriod(tradingPeriod);
        var fulfillmentRates = MarketObserver.CalculateFulfillmentRates(ordersSubmittedInPeriod);

        // Note: Currently only supports one PopulationCompany per market.
        // In the future, we may need to merge fulfillment rates 
        // from multiple PopulationCompanies/market segments.
        var populationCompany = _marketParticipants
            .OfType<PopulationCompany>()
            .FirstOrDefault();
        if (populationCompany!=null)
            {
                foreach (var fulfillmentRate in fulfillmentRates)
                {
                    if (populationCompany.GetDemand().TryGetValue(fulfillmentRate.Good, out var demandEntry))
                    {
                        demandEntry.CurrentDemand = fulfillmentRate.TotalDemand;
                        demandEntry.FulfilmentRate = fulfillmentRate.FulfillmentRate;
                    }
                }
            }
        return LemonadeStandResultObject.Success();
    }

    public LemonadeStandResultObject GetEffectiveElasticityForGood(Good good, ElasticityTypeEnum elasticity)
    {
        if (!good.Elasticities.TryGetValue(elasticity, out float elasticityValue))
        {
            return LemonadeStandResultObject.Failure(ResultTypeEnum.ElasticityNotFound, "");
        }

        return LemonadeStandResultObject.Success(extraData:elasticityValue);
    }
    public int GetMarketDemandForGood(string good_name)
    {
        var good = _marketDemand.Keys.FirstOrDefault(x=>x.GoodName == good_name);
        return _marketDemand[good].CurrentDemand;
    }
    public void InitializeDemandForSpecificGood(Good good, int InitialDemand, int minDemand=iDemandStrategy.MinDemand, int maxDemand=iDemandStrategy.MaxDemand,float curvature=1f)
    {
       DemandStrategy.InitializeDemandForSpecificGood(this,good,InitialDemand,minDemand,maxDemand,curvature);
    }
    /// <summary>
    /// A Market order is an order initiated by the Market
    /// Use this in order to buy produced goods,
    /// Have the 'Population' buy goods from the market
    /// etc.
    /// </summary>
    /// <param name="context"></param>
    public LemonadeStandResultObject QueueMarketOrder(ActionContext context)
    {   context.TradeToSubmit.SubmittingCompany = this;
        var queueResult = QueueOrder(context);
        if ( !queueResult.Equals(LemonadeStandResultObject.Success()) )
            return queueResult;
        return LemonadeStandResultObject.Success();
    }
#endregion
#region Pricing
    internal void UpdatePrices()
        {
            _priceManager.UpdatePricesForMarket(this);
        }
    public void CalculateNewBidAskSpreadForMarket()
    {
        //remember to call CalculateNewBidAskSpreadForMarket 
        //as part of TheEconomy.Instance.ExecuteDailyTrades.
        //eventually
        _priceManager.CalculateNewBidAskSpreadForMarket(this);
    }
    public decimal GetMarketCostForGood(Market market, Good good)
    {
        return _priceManager.GetMarketCostForGood(market,good);
    }
#endregion
#region Publishing
    public List<MarketData> PublishMarketData()
    {
        return _marketDataManager.PublishMarketData(this);
    }

    public void PublishSpreadToMarket(ActionContext context)
    {
        _marketDataManager.PublishSpreadToMarket(context);
    }
#endregion
#region Supply
    public List<(Good Good, int Quantity, decimal Price)> GetSupplyInPeriod(int period)
    {
        return _supplyProvider.GetSupplyInPeriod(period);
    }
    
#endregion

#region Interacting with the Economy

    public void StartTradingPeriod()
    {
        _demographicManager.RecordDemographicSnapshot(MarketId,CurrentPeriod,TurnPhase.Beginning);
        RollForEvents();
        ResolveMarketEvents();
        //Local Agents
        LocalAgentsAct(CurrentPeriod);
    }

    public void UnleashMarketForces(int period)
    {
        UpdateFulfillmentRates(period);
        UpdatePrices();
        DemandStrategy.AdjustDemandInPeriod(this);
        ConsumeGoods();
        UpdateCompanyStatuses(period);
        RecordDemographicSnapshot(TurnPhase.End);
        CurrentPeriod++;
    }

    private void LocalAgentsAct(int period)
    {
        var marketParticipants = _marketParticipants
                                .OfType<PopulationCompany>()
                                .ToList();

        if (marketParticipants.Count() == 0)
        {
            Debug.LogWarning($"No local agents found in Market {Name}");
        }
        foreach (var participant in marketParticipants)
        {
            participant.PerformStrategy(period);
        }
    }
    #endregion
    #region Overrides
    public override string ToString()
    {
        return Name;
    }
    public override bool Equals(object obj)
    {
        if(obj is Market market)
        {
            return market.Name == Name;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
    #endregion
}
