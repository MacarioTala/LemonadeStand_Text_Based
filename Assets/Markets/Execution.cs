using System;
using System.Collections.Generic;

public class Execution
{
    public Guid TransactionId {get;private set;} = Guid.NewGuid();
    public Order RecordedTrade{get; private set;}
    public List<Order> CounterPartyTrades{get; private set;} = new List<Order>();
    public int Period{get; private set;}
    public iEconAgent Buyer;
    public iEconAgent Seller;
    public int Quantity;
    public TradeType TradeType;
    public decimal Price;

    public Execution(Order recordedTrade, int period)
    {
        RecordedTrade = recordedTrade;
        Period = period;
    }

    public Execution(   Order order, 
                        iEconAgent buyer, 
                        iEconAgent seller, 
                        int quantity, 
                        decimal price,
                        int period)
    {
        RecordedTrade = order;
        Buyer = buyer;
        Seller = seller;
        Quantity = quantity;
        Price = price;
        Period = period;
    }

    public void AddCounterPartyTrade(Order counterPartyTrade)
    {
        CounterPartyTrades.Add(counterPartyTrade);
    }
    public override bool Equals(object other)
    {
        if (other is Execution otherTrade)
        {
            return TransactionId.Equals(otherTrade.TransactionId);
        }
        return false;
    }
    public override int GetHashCode()
    {
        return TransactionId.GetHashCode();
    }
    public override string ToString()
    {
        var actor = "";
        var action = "";
        int quantity = Quantity;

        if (RecordedTrade.IsBuy())
        { 
            actor = RecordedTrade.Buyer.ToString();
            action = "bought";
        }
        else
        {
            actor = RecordedTrade.Seller.ToString();
            action = "sold";
        }
        return $"Executed: {actor} {action} {quantity} of {RecordedTrade.Good} in Period: {Period}";
    }
}
public enum TradeType
{
    Buy,
    Sell
}