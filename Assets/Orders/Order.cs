using System;
using System.Collections.Generic;

public class Order
{
    public Guid Id {get;private set;} = Guid.NewGuid();
    public iEconAgent SubmittingCompany;
    public iEconAgent Buyer;
    public iEconAgent Seller;
    public Good Good;
    public int Quantity;
    public int FilledQuantity=0;
    public int RemainingQuantity=>Quantity-FilledQuantity;
    public decimal Price;
    private readonly List<Execution> _executions = new();
    public LemonadeStandResultObject AddExecution(Execution execution)
    {
        _executions.Add(execution);
        return LemonadeStandResultObject.Success();
    }
    public List<Execution> GetExecutions() => _executions;

    public bool IsFullyFilled => RemainingQuantity == 0;
    public bool IsPartiallyFilled => RemainingQuantity > 0 && FilledQuantity > 0;

    public bool IsUnfilled => RemainingQuantity == Quantity;
    private bool IsSelfTrade => Buyer == Seller;

    public bool IsBuy() => Buyer == SubmittingCompany;
    public bool IsSell() => Seller == SubmittingCompany;

    public LemonadeStandResultObject OrderStatus { get; set; }

    public LemonadeStandResultObject IsOrderValid()
    {
        if ((Seller == null) && (Buyer == null)) return LemonadeStandResultObject.Failure(ResultTypeEnum.OrderHasNoActors, "Order has no actors");
        if (Good == null) return LemonadeStandResultObject.Failure(ResultTypeEnum.OrderHasNoGood, "Order has no good");
        if (Quantity == 0) return LemonadeStandResultObject.Failure(ResultTypeEnum.OrderHasInvalidQuantity, "Order has an invalid quantity");
        if (Price <= 0) return LemonadeStandResultObject.Failure(ResultTypeEnum.OrderHasInvalidPrice, "Order has an invalid price");
        if (IsSelfTrade) return LemonadeStandResultObject.Failure(ResultTypeEnum.SelfTrade, "Order is a self trade");
        return LemonadeStandResultObject.Success();
    }

    public Order(iEconAgent buyer, iEconAgent seller, Good good, int quantity, decimal price)
    {
        Buyer = buyer;
        Seller = seller;
        Good = good;
        Quantity = quantity;
        Price = price;
    }
#region Overrides
    public override string ToString()
    {
        string action;
        string counterParty;
        string preposition;
        if (Buyer !=null)
        {
            action = Buyer.Equals(SubmittingCompany) ? " buys " : " sells ";
            counterParty = Seller?.Name?? " anyone ";
            preposition = " from ";
        }
        else
        {
            action = " sells ";
            counterParty = Buyer?.Name?? " anyone ";
            preposition=" to ";
        }
        
        return "Order: " + SubmittingCompany + action + Quantity + " " + Good.GoodName + preposition + counterParty + " at " + Price;
    }
    
    public override bool Equals(object other)
    {
        if (other is Order otherOrder)
        {
            return SubmittingCompany == otherOrder.SubmittingCompany 
                     && Buyer == otherOrder.Buyer 
                     && Seller == otherOrder.Seller 
                     && Good == otherOrder.Good 
                     && Quantity == otherOrder.Quantity 
                     && Price == otherOrder.Price;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return SubmittingCompany.GetHashCode() 
                ^ Buyer.GetHashCode() 
                ^ Seller.GetHashCode() 
                ^ Good.GetHashCode() 
                ^ Quantity.GetHashCode() 
                ^ Price.GetHashCode();
    }
#endregion
}
