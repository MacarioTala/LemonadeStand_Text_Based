using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class EconAgent : ScriptableObject, iEconAgent, iMarketParticipant
{
#region Identity and Initialization
    //Fields to get around Unity's limitation of not having automatic backing properties.
    [SerializeField]private string _company_name;
    public string Name
    {
        get => _company_name;
        set => _company_name = value;
    }
    public AgentLevelEnum companyLevel;
    public bool IsBankrupt() => cash <= 0;

    public bool IsPlayer { get; set; } = false;
    private Market marketCompanyIsIn;
    public Market GetMarket() => marketCompanyIsIn;
    public LemonadeStandResultObject SetMarket(Market market) {
         if(marketCompanyIsIn != null)
         {
            return LemonadeStandResultObject.Failure(ResultTypeEnum.MarketAlreadySet, "Market already set");
         }
         marketCompanyIsIn = market;
         
         return LemonadeStandResultObject.Success();
         }
    public LemonadeStandResultObject LeaveMarket()
    {
        marketCompanyIsIn = null;
        return LemonadeStandResultObject.Success();
    }
    protected EconAgent() { }
    internal void Initialize (string companyName, AgentLevelEnum company_level,iStrategy strategy=null)
    {
        Name = companyName;
        companyLevel = company_level;
        _companyStrategy = strategy;

        //setup
        SetInitialCash();
        SetInitialActions();
    }

    public static class Factory
    {
        //default constructor -- Basic Fixed Cost Strategy and no AI Strategy. Use for players
        public static EconAgent Create(string companyName, AgentLevelEnum company_level,iStrategy strategy=null)
        {
            var company = CreateInstance<EconAgent>();
            company.Initialize(companyName, company_level, strategy);
            company.FixedCostStrategy = new BasicFixedCostStrategy();
            return company;
        }

        public static EconAgent Create(string companyName, AgentLevelEnum company_level, iStrategy strategy, iFixedCostStrategy fixedCostStrategy)
        {
            var company = CreateInstance<EconAgent>();
            company.Initialize(companyName, company_level, strategy);
            company.FixedCostStrategy = fixedCostStrategy;
            return company;
        }

        public static T Create<T>() where T : EconAgent
        {
            var company = CreateInstance<T>();
            return company;
        }
    }
    
#endregion
#region Action Economy
    private List<AllowedAction> allowedActions = new();
    private int actionsPerCycle;
    private int actionsRemaining = 0;
    public int GetActionsRemaining() => actionsRemaining;
    public void ResetActions() => actionsRemaining = actionsPerCycle;
    public void SetActionsPerCycle(int actions) => actionsPerCycle = actions;
    public void AddAllowedAction(AllowedAction action) => allowedActions.Add(action);
    public void SetInitialActions()
    {
        switch(companyLevel)
        {
            case AgentLevelEnum.Beginner:
                actionsPerCycle = 3;
                break;
            case AgentLevelEnum.Intermediate:
                actionsPerCycle = 2;
                break;
            case AgentLevelEnum.Advanced:
                actionsPerCycle = 1;
                break;
            case AgentLevelEnum.Market:
                actionsPerCycle = 1000;
                break;
        }
        ResetActions();
    }
    #endregion
    #region Perception
    public decimal GetPerceivedCostOfGood(Good good, Dictionary<Good, decimal> prices)
    {
        var minimumBid = GetMarketIgnorantAssumedCOG();
        var perceivedCost = Recipes.Where(x => x.GetProduct().Equals(good))
                                .Select(r => r.GetPerceivedCostPerUnit(prices))
                                .DefaultIfEmpty(minimumBid)
                                .Average();
        return perceivedCost;
    }
    #endregion
    #region Demand
    private decimal marketIgnorantAssumedCOG;
    public decimal GetMarketIgnorantAssumedCOG() => marketIgnorantAssumedCOG;
    public void SetMarketIgnorantAssumedCOG(decimal value) => marketIgnorantAssumedCOG = value;
    private readonly Dictionary<Good, DemandData> _demand = new();
    public Dictionary<Good, DemandData> GetDemand()
    {
        return _demand;
    }
    public DemandData GetDemandFor(Good good)
    {
        if (_demand.TryGetValue(good, out var demandData))
        {
            return demandData;
        }
        return null;
    }
    
    public void SetDemand(Good good, DemandData demandData)
    {
        _demand[good] = demandData;
    }

   
    #endregion

    #region Financials
    private decimal cash = 0;
    private decimal minimumBid;
    public void SetMinimumBid(decimal minBid) => minimumBid = minBid;
    public decimal GetMinimumBid() => minimumBid;
    public decimal GetCash() => cash;
    public void SetCash(decimal newCash) => cash = newCash;

    public List<FixedCost> FixedCosts {get;set;} = new();
    private void SetInitialCash()
        {
            switch(companyLevel)
            {
                case AgentLevelEnum.Beginner:
                    cash = 10000;
                    break;
                case AgentLevelEnum.Intermediate:
                    cash = 5000;
                    break;
                case AgentLevelEnum.Advanced:
                    cash = 1000;
                    break;  
                case AgentLevelEnum.Market:
                    cash = 1000000000000;
                    break;
            }
        }
    public iFixedCostStrategy FixedCostStrategy {get;set;} = null;
    public decimal CalculateFixedCostsForPeriod(int period)
    {
        if (FixedCostStrategy == null)
        {
            Debug.Log("FixedCostStrategy not set");
            return 0;
        }
        else
        {
            return FixedCostStrategy.CalculateFixedCosts(FixedCosts, period);
        }
    }
#endregion    
#region Inventory Management
    private Inventory inventory = new();
    public Inventory GetInventory() => inventory;
    public void SetInventory(Inventory newInventory) => inventory=newInventory;
    public List<Recipe> Recipes{get; private set;} = new();

    public void AddRecipe(Recipe recipe)
    {
        if(!Recipes.Contains(recipe))
        {
            Recipes.Add(recipe);
        }
    }
    public void RemoveRecipe(Recipe recipe)
    {
        if (Recipes.Contains(recipe))
        {
            Recipes.Remove(recipe);
        }
    }
    public void Add_recipe(Recipe recipe) => Recipes.Add(recipe);
     public void MakeRecipe(ActionContext context)
    {
        var recipe = context.Recipe;
        var quantity = context.QuantityToMake;
        if(!Recipes.Contains(recipe))
        {
            throw new RecipeException("Recipe for "+recipe.ToString()+" not found in company's recipe book");
        }
        try
        {
            var totalCost = recipe.GetCostPerUnit(inventory)*quantity;
            var (product, product_quantity) = recipe.Make_recipe(quantity, inventory);
            var inventoryEntryToAdd=new InventoryEntry(product, product_quantity, totalCost,context.Period);
            inventoryEntryToAdd.SetRecipe(recipe);
            inventory.AddGood(inventoryEntryToAdd);
        }
        catch(RecipeException e)
        {
            throw new RecipeException(e.Message);
        }
    }
    public void ExpireGoods(int period) => inventory.ExpireGoods(period);
#endregion    
#region Goals and strategies
    private iStrategy _companyStrategy = null;
    public iStrategy GetStrategy() => _companyStrategy;
    public void SetStrategy(iStrategy strategy) => _companyStrategy = strategy;
    
    public List<Goal> Goals {get;set;} = new();

    public void AddGoal(Goal goal)
    {
        if(!Goals.Contains(goal))
        {
            Goals.Add(goal);
        }
    }
    public void CompleteGoal(Goal goal)
    {
        Debug.Log("Goal Completed: "+goal);
    }
    public void CheckCompanyGoals()
    {
        foreach (var goal in Goals)
        {
            if (goal.IsAchieved)
            {
                Debug.Log($"{Name} has completed the goal: {goal}");
                CompleteGoal(goal);
            }
        }

    }
    public decimal GetAggressionLevel() => _companyStrategy.GetAggressionLevel();
    public void SetAggressionLevel(decimal aggressionLevel)
    {
        _companyStrategy.SetAggressionLevel(aggressionLevel);
    }
    public void PerformStrategy(int period)
    {
        if (_companyStrategy != null)
        {
            _companyStrategy.PerformStrategy(this);
        }
        else
        {
            Debug.LogWarning("Company strategy not set for " + Name);
        }
    }
#endregion 
#region Buy/Sell and helper methods
    public void BuyGood(Good good, int quantity,decimal price,int period=0)
//Currently public for testing purposes
//Make private or internal afterwards
//period currently does nothing for companies, but is used in Market which implements iCompany
    {
        var money_needed = price * quantity;
        if(HasMoney(money_needed))
        {
            var inventory_entry = new InventoryEntry(good, quantity, price, period);
            inventory.AddGood(inventory_entry);
            cash -= money_needed;
        }
        else
        {
            throw new InsufficientFundsException("Insufficient funds to buy good");
        }
    }

    internal bool HasMoney(decimal money_needed)
    {
       return cash >= money_needed;
    }

    internal bool HasGood(Good good, int quantity)
    {
        var good_in_inventory = inventory.GetInventoryEntriesByGood(good.GoodName).FirstOrDefault();
        if(good_in_inventory == null)
        {
            return false;
        }
        return good_in_inventory != null && good_in_inventory.quantity >= quantity;
    }

    public void SellGood(Good good, int quantity, decimal price,int period=0)
    {
        //period currently does nothing for companies, but is used in Market which implements iCompany
        if(HasGood(good, quantity))
        {
            inventory.RemoveGood(good, quantity, price);
            cash += price * quantity;
        }
        else
        {
            throw new InventoryException("Company does not have enough of the good to sell");
        }
    }
#endregion
#region Time
    public int CurrentPeriod {get;set;}
    public int StartingPeriod {get;set;}
    public void UpdateCurrentPeriod(int period)
    {
        CurrentPeriod = period;
    }
    #endregion
    #region Market Actions

    public void SubtractFixedCostsForPeriod(int period) => SetCash(cash - CalculateFixedCostsForPeriod(period));
   
    private LemonadeStandResultObject IsValidOrder(ActionContext context)
    {
        var contextValidationResult = context.HasSubmittingCompany();
        if ( !contextValidationResult.Equals(LemonadeStandResultObject.Success()) )
            return contextValidationResult;
            
        var contextOrderValidationResult = context.ContainsValidTrade();
        if ( !contextOrderValidationResult.Equals(LemonadeStandResultObject.Success()) )
            return context.ContainsValidTrade();
        
        var orderValidationResult = context.TradeToSubmit.IsOrderValid();
        if (!orderValidationResult.Equals(LemonadeStandResultObject.Success()) ) 
            return orderValidationResult;

        //Validate cash
        if(context.TradeToSubmit.Buyer?.Equals(this)==true)
        {
            if(!HasMoney(context.TradeToSubmit.Price*context.TradeToSubmit.Quantity))
            {
                return LemonadeStandResultObject.Failure(ResultTypeEnum.InsufficientCash, "Insufficient Cash to queue order");
            }
        }
        //Validate inventory
        if(context.TradeToSubmit.Seller?.Equals(this)==true)
        {
            if(!HasGood(context.TradeToSubmit.Good, context.TradeToSubmit.Quantity))
            {
                return LemonadeStandResultObject.Failure(ResultTypeEnum.InsufficientGoods, "Insufficient Inventory to queue order");
            }
        }

        return LemonadeStandResultObject.Success();
    }
        
    public LemonadeStandResultObject QueueOrder(ActionContext context)
    {
        context.SubmittingCompany = this;
        var orderValidationResult = IsValidOrder(context);
        if(!orderValidationResult.Equals(LemonadeStandResultObject.Success())) return orderValidationResult;

        var MarketToSubmitTo = context.MarketToSubmitTo;
        var queueOrderResult=MarketToSubmitTo.QueueOrder(context);
        if(!queueOrderResult.Equals(LemonadeStandResultObject.Success())) return queueOrderResult;
        
        return LemonadeStandResultObject.Success();
    }
    public LemonadeStandResultObject SubmitBidAskSpreadToMarket(ActionContext context)
    {
        context.SubmittingCompany = this;
        var bidAskSpreadValidationResult = context.DoesContextContainValidBidAskSpread();
        if(!bidAskSpreadValidationResult.Equals(LemonadeStandResultObject.Success()))
           return bidAskSpreadValidationResult;
       
        context.MarketToSubmitTo.PublishSpreadToMarket(context);
        return LemonadeStandResultObject.Success();
    }
    #endregion
#region Overrides
    public override string ToString() => Name;

    public override bool Equals(object other)
    {
        if(other is EconAgent company)
        {
            return Name == company.Name;
        }
        return false;
    }
    public override int GetHashCode() => Name.GetHashCode();
#endregion
}
