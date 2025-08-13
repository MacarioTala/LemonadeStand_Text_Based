using System;
using System.Collections.Generic;

public class ActionContext
{
    //General
    public ActionEnum Action;
    public Market MarketToSubmitTo;
    public int Period;

    private iEconAgent _submittingCompany;
    public iEconAgent SubmittingCompany
    {
        get => _submittingCompany;
        set
        {
            _submittingCompany = value;
            if( TradeToSubmit is not null )
                { TradeToSubmit.SubmittingCompany = SubmittingCompany; }
        }
    }
    
    //Make Recipe
    public EconAgent RecipeMaker;
    public Recipe Recipe;
    public int QuantityToMake;

    //QueueTrade/transactions
    public Order TradeToSubmit;
    public List<Order> CounterPartyOrders;
    public Order PrimaryOrder;

    //Submit Bid/Ask
    public Good GoodToSubmit;
    public decimal BidToSubmit;
    public decimal AskToSubmit;

    public LemonadeStandResultObject HasSubmittingCompany()
    {
        if (SubmittingCompany == null) return LemonadeStandResultObject.Failure(ResultTypeEnum.OrderHasNoSubmittingCompany, "Order has no submitting company");
        return LemonadeStandResultObject.Success();
    }
    public LemonadeStandResultObject ContainsValidTrade()
    {
        if (TradeToSubmit == null) return LemonadeStandResultObject.Failure(ResultTypeEnum.ContextHasNoTrade, "Context has no trade");
        return LemonadeStandResultObject.Success();
    }

    public LemonadeStandResultObject DoesContextContainValidBidAskSpread()
    {
         if(BidToSubmit==0) return LemonadeStandResultObject.Failure(ResultTypeEnum.SpreadHasNoBid, "Spread has no bid");
         if(AskToSubmit==0) return LemonadeStandResultObject.Failure(ResultTypeEnum.SpreadHasNoAsk, "Spread has no ask");
         if(GoodToSubmit==null) return LemonadeStandResultObject.Failure(ResultTypeEnum.SpreadHasNoGood, "Spread has no good");
         if(MarketToSubmitTo==null) return LemonadeStandResultObject.Failure(ResultTypeEnum.MarketNotSet, "Market not set");
        return LemonadeStandResultObject.Success();
    }

    internal LemonadeStandResultObject ContainsValidPairedOrders()
    {
        if (MarketToSubmitTo == null) 
            return LemonadeStandResultObject.Failure(ResultTypeEnum.MarketNotSet, "Market not set");
        if (PrimaryOrder == null)
            return LemonadeStandResultObject.Failure(ResultTypeEnum.PrimaryOrderNotSet, "Primary order not set");
        if (CounterPartyOrders == null || CounterPartyOrders.Count == 0)
            return LemonadeStandResultObject.Failure(ResultTypeEnum.NoMatchingCounterParties, "No matching counterparty found");
        return LemonadeStandResultObject.Success();
    }
}

public class ContextException : Exception
{
    public ContextException(string message) : base(message)
    {
    }
}

public class ActionContextBuilder
{
    private ActionContext _contextToReturn;

    public ActionContextBuilder()
    {
        _contextToReturn = new ActionContext();
    }

    public ActionContextBuilder WithAction(ActionEnum action)
    {
        _contextToReturn.Action = action;
        return this;
    }

    public ActionContextBuilder ForMarket(Market market)
    {
        _contextToReturn.MarketToSubmitTo = market;
        return this;
    }
    public ActionContextBuilder ForPeriod(int period)
    {
        _contextToReturn.Period = period;
        return this;
    }

    public ActionContextBuilder WithTrade(Order trade)
    {
        _contextToReturn.TradeToSubmit = trade;
        return this;
    }

    public ActionContext Build()
    {
        if (_contextToReturn.ContainsValidTrade() != LemonadeStandResultObject.Success())
        {
            throw new ContextException("Trade is not valid");
        }

        return _contextToReturn;
    }
}