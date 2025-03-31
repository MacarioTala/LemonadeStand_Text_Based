using System.Collections.Generic;

public interface iTradeProcessor
{   
    /// <summary>  
    /// Called by entities that placed orders to get the results of their orders
    /// </summary>
    List<Order> GetOrderResults (ActionContext context);
    List<Order> ProcessCompanyOrders(Market market);
    /// <summary>
    /// Public interface that lets an entity send an Order 
    /// to any entity that can Process Orders
    /// </summary>
    /// <param name="context"></param>
    LemonadeStandResultObject QueueOrder (ActionContext context);
}