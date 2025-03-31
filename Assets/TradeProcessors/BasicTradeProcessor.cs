using System.Collections.Generic;
using System.Linq;

public class BasicTradeProcessor : iTradeProcessor
{
     private List<iOrderPrioritizer> _orderPrioritizers = new();
     private readonly iTransactionManager _transactionManager;

    public BasicTradeProcessor()
    {   
        _orderPrioritizers.Add(new LessaizfairePrioritizer());
        _transactionManager = new BasicTransactionManager();
    }
    
    public List<Order> GetOrderResults(ActionContext context)
    {
        return context.MarketToSubmitTo.GetOrdersSentToMarket()
                .Where(x => x.Buyer == context.TradeToSubmit.Buyer || x.Seller == context.TradeToSubmit.Seller)
                .ToList();
    }
    
    public List<Order> ProcessCompanyOrders(Market market)
    {
        List<Order> executedTrades = new();
        var ordersInPeriod = market.GetOrdersSentToMarket();
        List<Good> goodsToTradeThisPeriod = ordersInPeriod
                                            .Select(x => x.Good).Distinct().ToList();

        foreach (var good in goodsToTradeThisPeriod)
        {
           executedTrades.AddRange(ExecuteBestTradesForGood(good,market,ordersInPeriod));
        }
        market.RemoveFilledOrders();
        return executedTrades;
    }

    internal List<Order> ExecuteBestTradesForGood(Good good, Market market, List<Order> OrdersSentToMarket)
    {
        List<Order> executedOrders = new();
        var relevantOrders = OrdersSentToMarket.Where(x => x.Good.Equals(good)
                                               )
                                               .ToList();
        foreach (var prioritizer in _orderPrioritizers)
        {
            var prioritizedOrders = prioritizer.Filter(relevantOrders);
            
            for (var i =0; i< prioritizedOrders.Count;i++)
            {
                if(prioritizedOrders.Count == 1)
                {
                    prioritizedOrders[i].OrderStatus = LemonadeStandResultObject.Failure(ResultTypeEnum.NoMatchingCounterParties, "No matching counterparty found");
                    break;
                } 
                var order = prioritizedOrders[i];

                if (order.IsFullyFilled) 
                {
                    prioritizedOrders.RemoveAt(i); 
                    i--;
                    continue;
                }
                
                if (FindCounterPartiesForOrder(market,good).ExtraData is not List<Order> counterPartyOrders)
                {
                    order.OrderStatus = LemonadeStandResultObject.Failure(ResultTypeEnum.NoMatchingCounterParties, "No matching counterparty found");
                }
                else
                {
                   var transactionResult = _transactionManager.ProcessTransaction(
                        new ActionContext
                        {
                            PrimaryOrder = order,
                            CounterPartyOrders = counterPartyOrders,
                            MarketToSubmitTo = market,
                            Period = market.CurrentPeriod
                        }
                    );
                    if(!transactionResult.Result.Equals(LemonadeStandResultObject.Success().Result))
                        order.OrderStatus = transactionResult;
                      
                    executedOrders.Add(order);
                    prioritizedOrders.RemoveAt(i);
                    i--;
                }
            }
        }
        return executedOrders;
    }

    internal Order GeneratePrimaryOrder(Market market, Good good)
    {
        var buyOrders = market.GetOrdersSentToMarket()
                            .Where(x => (x.Buyer??default) == x.SubmittingCompany
                            &&
                            !x.IsFullyFilled
                            && x.Good.Equals(good) 
                            )
                            .OrderByDescending(x=>x.Quantity)
                            .ThenByDescending(x=>x.Price)
                            .ToList();
        
        if ( buyOrders.Any() ) return buyOrders.First();

        var sellOrders = market.GetOrdersSentToMarket()
                            .Where(x => (x.Seller??default) == x.SubmittingCompany
                            &&
                            !x.IsFullyFilled
                            && x.Good.Equals(good)
                            )
                            .OrderByDescending(x=>x.Quantity)
                            .ThenBy(x=>x.Price)
                            .ToList();
        return sellOrders.Any() ? sellOrders.First() : null;
    }

    private bool SellOrdersExistInMarket(Market market)
    {
        var sellOrdersExist = market.GetOrdersSentToMarket().Any(x => x.Seller!=null);
        return sellOrdersExist;
    }
    private bool BuyOrdersExistInMarket(Market market)
    {
        var buyOrdersExist = market.GetOrdersSentToMarket().Any(x => x.Buyer!=null);
        return buyOrdersExist;
    }

    internal LemonadeStandResultObject FindCounterPartiesForOrder(Market market,Good good)
    {
        var primaryOrder = GeneratePrimaryOrder(market,good);
        var counterPartiesForOrder = market.GetOrdersSentToMarket()
                                    .Where (x=>IsValidCounterParty(x, primaryOrder, market)
                                    && x.Good.Equals(primaryOrder.Good))
                                    .OrderBy(x=>x.Buyer != null? -x.Price:x.Price)
                                    .ToList();

        if (!counterPartiesForOrder.Any()) 
           return LemonadeStandResultObject.Failure(
                            ResultTypeEnum.NoMatchingCounterParties
                            , "No matching counterparties found");
                                    
        return LemonadeStandResultObject.Success(counterPartiesForOrder);
    }

    internal bool IsValidCounterParty(Order order, Order primaryOrder, Market market)
    {
        //Goods must match
        if(!order.Good.Equals(primaryOrder.Good)) return false;

        //Order cannot be fully filled
        if(order.IsFullyFilled) return false;
        
        //The submitting company cannot be the counterparty
        if(order.SubmittingCompany == primaryOrder.SubmittingCompany) return false;
        
        //Readability helpers for final condition
        var orderIsASellPrimaryIsBuy=order.Seller is not null
                                    &&
                                    primaryOrder.Buyer is not null;
        var orderIsABuyPrimaryIsSell=order.Buyer is not null
                                    &&
                                    primaryOrder.Seller is not null;
        var orderIsASellPrimaryIsSell=order.Seller is not null
                                    || primaryOrder.Seller is not null;
        var orderIsABuyPrimaryIsBuy=order.Buyer is not null
                                    || primaryOrder.Buyer is not null;
        var buyOrdersExistInMarket = BuyOrdersExistInMarket(market);
        var sellOrdersExistInMarket = SellOrdersExistInMarket(market);
        
        //If only buys or sells exist, there is no counterparty
        if(orderIsASellPrimaryIsSell && !buyOrdersExistInMarket) return false;
        if(orderIsABuyPrimaryIsBuy && !sellOrdersExistInMarket) return false;

        //Valid counterparty for buy order is a sell order
        //in a market where buy orders exist
        var isCounterPartyForBuy =  orderIsASellPrimaryIsBuy 
                                    && buyOrdersExistInMarket;
                                
        //Valid counterparty for sell order is a buy order
        //in a market where sell orders exist
        var isCounterPartyForSell = orderIsABuyPrimaryIsSell 
                                    && sellOrdersExistInMarket;

        return isCounterPartyForBuy || isCounterPartyForSell;
    }
    public LemonadeStandResultObject QueueOrder(ActionContext context)
    {
        return context.MarketToSubmitTo.QueueOrder(context);
    }
}