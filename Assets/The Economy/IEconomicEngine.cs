using System;
using System.Collections.Generic;

/// <summary>
/// Interface defining the core economic engine functionality.
/// This replaces the singleton TheEconomy pattern with proper dependency injection.
/// </summary>
public interface IEconomicEngine
{
    /// <summary>
    /// Current trading period number
    /// </summary>
    int TradingPeriod { get; }

    /// <summary>
    /// List of all registered companies in the economy
    /// </summary>
    IReadOnlyList<iCompany> Companies { get; }

    /// <summary>
    /// List of all available goods in the economy
    /// </summary>
    IReadOnlyList<Good> Goods { get; }

    /// <summary>
    /// Initializes the economic engine with the specified trade logger
    /// </summary>
    /// <param name="tradeLogger">Logger for recording trade transactions</param>
    void Initialize(ITradeLogger tradeLogger);

    /// <summary>
    /// Registers a new company in the economy
    /// </summary>
    /// <param name="company">Company to register</param>
    /// <exception cref="TheEconomy_CompanyException">Thrown when company name already exists</exception>
    void RegisterCompany(iCompany company);

    /// <summary>
    /// Removes a market from the economy
    /// </summary>
    /// <param name="market">Market to remove</param>
    void RemoveMarket(Market market);

    /// <summary>
    /// Gets a market by its name
    /// </summary>
    /// <param name="marketName">Name of the market to find</param>
    /// <returns>Market with the specified name, or null if not found</returns>
    Market GetMarketByName(string marketName);

    /// <summary>
    /// Gets the global market (the initial/primary market)
    /// </summary>
    /// <returns>The global market instance</returns>
    iCompany GetGlobalMarket();

    /// <summary>
    /// Starts a new trading period across all markets
    /// </summary>
    void StartTradingPeriod();

    /// <summary>
    /// Ends the current trading period and processes all trades
    /// </summary>
    void EndTradingPeriod();

    /// <summary>
    /// Handles bankruptcy of a company in a specific market
    /// </summary>
    /// <param name="market">Market where bankruptcy occurred</param>
    /// <param name="bankruptCompany">Company that went bankrupt</param>
    void HandleBankruptcy(Market market, Company bankruptCompany);

    /// <summary>
    /// Handles market failure scenarios
    /// </summary>
    /// <param name="market">Market that failed</param>
    void HandleMarketFailure(Market market);

    /// <summary>
    /// Gets all transactions for a specific trading period
    /// </summary>
    /// <param name="period">Trading period to query</param>
    /// <returns>Dictionary of transactions grouped by market ID</returns>
    Dictionary<Guid, List<Execution>> GetAllTransactions(int period);

    /// <summary>
    /// Creates initial goods in the economy
    /// </summary>
    /// <param name="goods">List of goods to create</param>
    void CreateInitialGoods(List<Good> goods);

    /// <summary>
    /// Clears all companies and goods from the economy
    /// </summary>
    void ClearEconomy();
}