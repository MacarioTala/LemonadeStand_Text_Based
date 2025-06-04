using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pure C# implementation of the economic engine without Unity dependencies.
/// This replaces the singleton TheEconomy MonoBehaviour with proper dependency injection.
/// </summary>
public class EconomicEngine : IEconomicEngine
{
    private readonly List<iCompany> _companies = new();
    private readonly List<Good> _goods = new();
    private ITradeLogger _tradeLogger;
    private Market _initialMarket;
    private bool _isInitialized = false;

    public int TradingPeriod { get; private set; } = 0;

    public IReadOnlyList<iCompany> Companies => _companies.AsReadOnly();

    public IReadOnlyList<Good> Goods => _goods.AsReadOnly();

    public void Initialize(ITradeLogger tradeLogger)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("The Economic Engine is already initialized");
            return;
        }

        _tradeLogger = tradeLogger ?? throw new ArgumentNullException(nameof(tradeLogger));
        CreateInitialGoods(_goods);
        CreateInitialMarket();
        _isInitialized = true;
        Debug.Log("Economic Engine initialized successfully.");
    }

    public void RegisterCompany(iCompany company)
    {
        if (company == null)
            throw new ArgumentNullException(nameof(company));

        if (_companies.Any(x => x.Name == company.Name))
        {
            throw new TheEconomy_CompanyException($"{company.Name} already registered");
        }

        _companies.Add(company);
    }

    public void RemoveMarket(Market market)
    {
        if (market == null)
            throw new ArgumentNullException(nameof(market));

        _companies.Remove(market);
    }

    public Market GetMarketByName(string marketName)
    {
        if (string.IsNullOrEmpty(marketName))
            return null;

        return _companies.OfType<Market>().FirstOrDefault(x => x.Name == marketName);
    }

    public iCompany GetGlobalMarket() => _initialMarket;

    public void StartTradingPeriod()
    {
        ThrowIfNotInitialized();

        var markets = _companies.OfType<Market>().ToList();
        foreach (var market in markets)
        {
            market.StartTradingPeriod();
        }
    }

    public void EndTradingPeriod()
    {
        ThrowIfNotInitialized();

        var executedTrades = new List<Order>();

        // Update prices and process trades
        foreach (Market market in _companies.OfType<Market>())
        {
            executedTrades = market.ProcessCompanyOrders();
            market.UnleashMarketForces(TradingPeriod);
        }

        TradingPeriod++;

        _tradeLogger?.SaveDailySummary(executedTrades);
    }

    public void HandleBankruptcy(Market market, Company bankruptCompany)
    {
        if (market == null)
            throw new ArgumentNullException(nameof(market));
        if (bankruptCompany == null)
            throw new ArgumentNullException(nameof(bankruptCompany));

        Debug.Log($"{bankruptCompany.Name} in {market.Name} has gone bankrupt");
        ShowBankruptcySummary(bankruptCompany);

        if (bankruptCompany.IsPlayer)
        {
            OnGameEnd?.Invoke();
        }
    }

    public void HandleMarketFailure(Market market)
    {
        throw new NotImplementedException();
    }

    public Dictionary<Guid, List<Execution>> GetAllTransactions(int period)
    {
        ThrowIfNotInitialized();

        var markets = _companies.OfType<Market>().ToList();
        return _tradeLogger?.GetAllTransactions(markets, period);
    }

    public void CreateInitialGoods(List<Good> goods)
    {
        if (goods == null)
            return;

        // Clear existing goods
        _goods.Clear();
        _goods.AddRange(goods);

        // If we have an initial market, populate it with goods
        if (_initialMarket != null)
        {
            PopulateMarketWithGoods(_initialMarket, goods);
        }
    }

    public void ClearEconomy()
    {
        _companies.Clear();
        _goods.Clear();
        TradingPeriod = 0;
        _initialMarket = null;
        _isInitialized = false;
    }

    /// <summary>
    /// Event fired when the game should end (e.g., player bankruptcy)
    /// </summary>
    public event Action OnGameEnd;

    private void CreateInitialMarket()
    {
        _initialMarket = Market.Factory.CreateStarterMarket(
            "The First Market",
            CompanyLevelEnum.Market,
            ScriptableObject.CreateInstance<LinearDemandStrategy>());
        
        RegisterCompany(_initialMarket);
        
        // Populate with any existing goods
        if (_goods.Count > 0)
        {
            PopulateMarketWithGoods(_initialMarket, _goods);
        }
    }

    private void PopulateMarketWithGoods(Market market, List<Good> goods)
    {
        // Limits for good quantities based on rarity
        var commonRange = UnityEngine.Random.Range(1, 1001);
        var uncommonRange = UnityEngine.Random.Range(1, 501);
        var rareRange = UnityEngine.Random.Range(1, 101);
        var veryRareRange = UnityEngine.Random.Range(1, 11);

        foreach (Good good in goods)
        {
            // Generate quantity based on rarity
            int quantity = good.GetRarity() switch
            {
                RarityEnum.Common => commonRange,
                RarityEnum.Uncommon => uncommonRange,
                RarityEnum.Rare => rareRange,
                RarityEnum.Very_Rare => veryRareRange,
                _ => throw new ArgumentOutOfRangeException()
            };

            market.GetInventory().AddGood(new InventoryEntry(good, quantity, good.GetPrice(), TradingPeriod));
        }
    }

    private void ShowBankruptcySummary(Company bankruptCompany)
    {
        Debug.Log($"{bankruptCompany.Name} has gone bankrupt after {TradingPeriod} trading periods");
    }

    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Economic Engine must be initialized before use. Call Initialize() first.");
        }
    }
}