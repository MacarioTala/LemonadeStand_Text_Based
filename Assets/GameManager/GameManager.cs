using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameRunning { get; private set; }

    [SerializeField] private float splashDuration = 3f;
    [SerializeField] private readonly GameObject splashAnimation;
    [SerializeField] private readonly GameObject mainMenu;
    [SerializeField] private readonly GameObject gameScreen;
    [SerializeField] private GameObject splashTypewriterPrefab;
    [SerializeField] private AudioSource splashScreenAudioSource;
    [SerializeField] private AudioClip splashScreenSoundClip;

    private TypeWriter typeWriterInstance;
    [SerializeField] private TextMeshProUGUI typeWrittenText;

    private IEconomicEngine _economicEngine;
    private ITradeLogger _tradeLogger;

    int Period=0;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Initialize();
        typeWriterInstance.Initialize(typeWrittenText,splashScreenAudioSource,splashScreenSoundClip);
        StartCoroutine(BlinkCursor());
        StartCoroutine(typeWriterInstance.TypeText(TypeWriter.splashMessage));
    }

    private void Initialize()
    {
        IsGameRunning = true;

        // Configure economic services if not already done
        if (!EconomicServiceContainer.Instance.IsRegistered<IEconomicEngine>())
        {
            EconomicServiceContainer.Instance.ConfigureDefaults();
        }

        // Get economic engine from dependency injection
        try
        {
            _economicEngine = EconomicServiceContainer.Instance.Resolve<IEconomicEngine>();
            _tradeLogger = EconomicServiceContainer.Instance.Resolve<ITradeLogger>();
            
            Period = _economicEngine.TradingPeriod;
            _economicEngine.Initialize(_tradeLogger);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize economic engine: {ex.Message}");
            return;
        }

        if(splashTypewriterPrefab != null)
        {
            var typeWriterInstanceObject = Instantiate(splashTypewriterPrefab);
            typeWriterInstance = typeWriterInstanceObject.GetComponent<TypeWriter>();
        }

        Debug.Log("Game Manager Initialized");
    }
// Splash screen
//TypeWriter
    public TextMeshProUGUI GetTypeWrittenText()
    {
        return typeWrittenText;
    }

    private void ShowSplashScreen()
    {
        splashAnimation.SetActive(true);
        mainMenu.SetActive(false);
        gameScreen.SetActive(false);

        StartCoroutine(TransitionToMainMenu());
    }


    [SerializeField] private TextMeshProUGUI cursor;
    private bool isCursorVisible = true;
    [SerializeField] private float blinkSpeed = 0.5f;
    private IEnumerator BlinkCursor()
    {
        while (true)
        {
            cursor.text = isCursorVisible ? "|" : "";
            isCursorVisible = !isCursorVisible;
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    private IEnumerator TransitionToMainMenu()
    {
        yield return new WaitForSeconds(splashDuration);
        splashAnimation.SetActive(false);
        mainMenu.SetActive(true);
    }

    // Game Flow
    public void StartTurn()
    {
        if(!IsGameRunning) return;

        Debug.Log($"Starting Turn {Period} ...");
        //UI code goes here
    }

    public void EndTurn()
    {
        if(!IsGameRunning) return;

        Debug.Log($"Ending Turn {Period} ...");
        _economicEngine.EndTradingPeriod();
        Period = _economicEngine.TradingPeriod;

        CheckGameStatus();
        //UI code goes here
    }

    private void CheckGameStatus()
    {
        //Win/Lose Conditions here
        throw new NotImplementedException();
    }

    public void TriggerEvent(string eventName)
    {
        Debug.Log($"Event Triggered : {eventName}");
    }

    public void EndGame(string message)
    {
        IsGameRunning = false;
        Debug.Log($"Game Over : {message}");
    }
}