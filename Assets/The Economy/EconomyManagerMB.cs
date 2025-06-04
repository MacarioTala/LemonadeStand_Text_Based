using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unity MonoBehaviour wrapper for the economic engine that manages dependency injection.
/// This replaces the singleton pattern with proper service management.
/// </summary>
public class EconomyManagerMB : MonoBehaviour
{
    [Header("Economy Configuration")]
    [SerializeField] private List<Good> initialGoods = new();
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool loadMarketsFromResources = true;

    private IEconomicEngine _economicEngine;
    private bool _isInitialized = false;

    /// <summary>
    /// Gets the economic engine instance (use dependency injection instead when possible)
    /// </summary>
    public IEconomicEngine EconomicEngine
    {
        get
        {
            if (_economicEngine == null)
            {
                _economicEngine = EconomicServiceContainer.Instance.Resolve<IEconomicEngine>();
            }
            return _economicEngine;
        }
    }

    /// <summary>
    /// Event fired when the economy is fully initialized
    /// </summary>
    public static event System.Action<IEconomicEngine> OnEconomyInitialized;

    private void Awake()
    {
        // Ensure service container is configured
        if (!EconomicServiceContainer.Instance.IsRegistered<IEconomicEngine>())
        {
            EconomicServiceContainer.Instance.ConfigureDefaults();
        }

        // Set up as singleton if this is the first instance
        if (FindObjectsOfType<EconomyManagerMB>().Length > 1)
        {
            Debug.LogWarning("Multiple EconomyManagerMB instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeEconomy();
        }
    }

    /// <summary>
    /// Initializes the economic engine with default settings
    /// </summary>
    public void InitializeEconomy()
    {
        if (_isInitialized)
        {
            Debug.LogWarning("Economy is already initialized");
            return;
        }

        try
        {
            // Get services from container
            _economicEngine = EconomicServiceContainer.Instance.Resolve<IEconomicEngine>();
            var tradeLogger = EconomicServiceContainer.Instance.Resolve<ITradeLogger>();

            // Initialize the engine
            _economicEngine.Initialize(tradeLogger);

            // Add initial goods if specified
            if (initialGoods != null && initialGoods.Count > 0)
            {
                _economicEngine.CreateInitialGoods(initialGoods);
            }

            // Load markets from resources if enabled
            if (loadMarketsFromResources)
            {
                LoadMarketsFromResources();
            }

            _isInitialized = true;
            OnEconomyInitialized?.Invoke(_economicEngine);
            
            Debug.Log("Economy Manager initialized successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize economy: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Starts a new trading period
    /// </summary>
    public void StartTradingPeriod()
    {
        EnsureInitialized();
        _economicEngine.StartTradingPeriod();
    }

    /// <summary>
    /// Ends the current trading period
    /// </summary>
    public void EndTradingPeriod()
    {
        EnsureInitialized();
        _economicEngine.EndTradingPeriod();
    }

    /// <summary>
    /// Gets the current trading period
    /// </summary>
    public int GetTradingPeriod()
    {
        EnsureInitialized();
        return _economicEngine.TradingPeriod;
    }

    /// <summary>
    /// Gets a market by name
    /// </summary>
    public Market GetMarketByName(string marketName)
    {
        EnsureInitialized();
        return _economicEngine.GetMarketByName(marketName);
    }

    /// <summary>
    /// Registers a company in the economy
    /// </summary>
    public void RegisterCompany(iCompany company)
    {
        EnsureInitialized();
        _economicEngine.RegisterCompany(company);
    }

    /// <summary>
    /// Gets all companies in the economy
    /// </summary>
    public IReadOnlyList<iCompany> GetCompanies()
    {
        EnsureInitialized();
        return _economicEngine.Companies;
    }

    /// <summary>
    /// Clears the economy state
    /// </summary>
    public void ClearEconomy()
    {
        if (_economicEngine != null)
        {
            _economicEngine.ClearEconomy();
            _isInitialized = false;
        }
    }

    private void LoadMarketsFromResources()
    {
        var markets = Resources.LoadAll<Market>("Markets");
        foreach (var market in markets)
        {
            _economicEngine.RegisterCompany(market);
            Debug.Log($"Loaded Market: {market.Name} id:{market.MarketId}");
        }
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            InitializeEconomy();
        }
    }

    /// <summary>
    /// Static method for setting up testing scenarios
    /// </summary>
    public static EconomyManagerMB SetupForTests(ITradeLogger logger = null)
    {
        // Clear any existing instances
        var existing = FindObjectsOfType<EconomyManagerMB>();
        foreach (var instance in existing)
        {
            DestroyImmediate(instance.gameObject);
        }

        // Configure service container for testing
        EconomicServiceContainer.Instance.ConfigureForTesting();
        
        if (logger != null)
        {
            EconomicServiceContainer.Instance.RegisterSingleton<ITradeLogger>(logger);
        }

        // Create test instance
        var testObject = new GameObject("TestEconomyManager");
        var testManager = testObject.AddComponent<EconomyManagerMB>();
        testManager.initializeOnStart = false; // Manual initialization for tests
        
        return testManager;
    }

    private void OnDestroy()
    {
        if (_economicEngine != null)
        {
            _economicEngine.ClearEconomy();
        }
    }
}