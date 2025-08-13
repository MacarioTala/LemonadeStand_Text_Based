using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TextBasedStoryHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textScroll;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button EndTurnButton;
    [SerializeField] private TextMeshProUGUI ActionPointsText;
    public static TextBasedStoryHandler Instance { get; private set; }
#region Game Variables
    private EconAgent PlayerCompany;
    private bool isWaitingForPlayerInput = false;
    private Market initialMarket;
    private int playerActionsRemaining;

    private MenuStateEnum CurrentMenuState = MenuStateEnum.Splash;
#endregion
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if(!isWaitingForPlayerInput) return;

        if(Input.GetKeyDown(KeyCode.Space))
        {
            isWaitingForPlayerInput = false;
            ClearTextScroll();
            DisplayChoices();
        }

        if(CurrentMenuState==MenuStateEnum.MainMenu) ChoicesMainMenu();

        if(CurrentMenuState==MenuStateEnum.OrderSupplies) ChoicesOrderSupplies();
    }

    private void ChoicesMainMenu()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseFromMainMenu(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseFromMainMenu(2);
    }

    private void ChoicesOrderSupplies()
    {
        throw new NotImplementedException();
    }

    public void StartTextBasedGame()
    {
        InitializeMarket();
        InitializePlayer();
        WireUpButtons();
        playerActionsRemaining = PlayerCompany.GetActionsRemaining();
        ActionPointsText.text = playerActionsRemaining.ToString();
        if (Instance == null)
        {
            Debug.Log("TextBasedStoryHandler is not initialized.");
            return;
        }
        Instance.StartCoroutine(Instance.StartGameLoop());
    }

    private void WireUpButtons()
    {
        EndTurnButton.onClick.AddListener(EndTurn);
    }

    private void EndTurn()
    {
        Debug.Log("End Turn");
    }

    private void InitializeMarket()
    {
        initialMarket = TheEconomy.Instance.GetMarketByName("Episode 1 Market");
        var testMarket = TheEconomy.Instance.GetMarketByName("The First Market");
        TheEconomy.Instance.RemoveMarket(testMarket);
        var inventory = initialMarket.GetInventory();
        initialMarket.SetTradeProcessor(new BasicTradeProcessor());

        var period = TheEconomy.Instance.tradingPeriod;
        var Lemon = Good.CreateInstance("Lemons", new PriceBand(1, 3), RarityEnum.Common);
        var Sugar = Good.CreateInstance("Sugar", new PriceBand(1, 3), RarityEnum.Common);
        var Water = Good.CreateInstance("Water", new PriceBand(1, 3), RarityEnum.Common);
        var lemonEntry=inventory.AddGood(new InventoryEntry(Lemon, 1000, Lemon.GetPrice(),period));
        lemonEntry.SetPrice(Lemon.GetPrice()*1.1m);
        var sugarEntry= inventory.AddGood(new InventoryEntry(Sugar, 1000, Sugar.GetPrice(),period));
        sugarEntry.SetPrice(Sugar.GetPrice()*1.1m);
        var waterEntry=inventory.AddGood(new InventoryEntry(Water, 1000, Water.GetPrice(),period));
        waterEntry.SetPrice(Water.GetPrice()*1.1m);
    }
    private void InitializePlayer()
    {
        PlayerCompany= EconAgent.Factory.Create("Player1",AgentLevelEnum.Beginner);
        PlayerCompany.IsPlayer= true;
        playerActionsRemaining = PlayerCompany.GetActionsRemaining();
        initialMarket.RegisterMarketParticipant(PlayerCompany);
    }

    private IEnumerator StartGameLoop()
    {
        textScroll.text = "";
        LogMessage("Welcome to Lemonade Stand!");
        yield return new WaitForSeconds(1);
        LogMessage("Can you save Capitalism?");
        yield return new WaitForSeconds(1);
        LogMessage("Let's find out!");
        yield return new WaitForSeconds(1);
        DisplayChoices();
    }

    private void ClearTextScroll()
    {
        if(textScroll!=null) textScroll.text = "";
    }
    private void ChooseFromMainMenu(int choice)
    {
        ClearTextScroll();
        switch (choice)
        {
            case 1:
                DisplayInventory();
                break;
            case 2:
                SetLemonadePrice();
                break;
            default:
                LogMessage("Invalid choice. Please choose again.");
                DisplayChoices();
                break;
        }
        isWaitingForPlayerInput = true;
        LogMessage("Press Space to continue.");
    }

    private void DisplayChoices()
    {
        isWaitingForPlayerInput = true;
        CurrentMenuState = MenuStateEnum.MainMenu;
        LogMessage("What would you like to do?");
        LogMessage("1. Check Inventory");
        LogMessage("2. Set Lemonade Price");
        LogMessage("\n");
    }

    private void CheckNews()
    {
        LogMessage($"It is Period : {TheEconomy.Instance.tradingPeriod}.");
        LogMessage($"You have {PlayerCompany.GetCash()} credits.");
        LogMessage($"The people in your neighbourhood are {initialMarket.GetEnnuiLevel()}");
        LogMessage("The news is not available yet.");
    }
    private void DisplayInventory()
    {
        var inventory = PlayerCompany.GetInventory().GetInventoryEntries();
        LogMessage("Inventory:");
        foreach (var item in inventory)
        {
            LogMessage($"Item: {item.good} Quantity: {item.quantity} Acquired at: {item.Cost}");
        }
    }

    private void SetLemonadePrice()
    {
        LogMessage("Cannot set Lemonade Price yet");
    }

    private void OrderSupplies()
    {
        var inventory = initialMarket.GetInventory().GetInventoryEntries();

        if (inventory.Count == 0)
        {
            LogMessage("The grocery store is out of supplies.");
        }
        else
        {
            LogMessage($"The following supplies are available in this store:");
        
            foreach (var item in inventory)
            {
                LogMessage($"{item.quantity} {item.good} at {item.Cost}");
            }
            
            LogMessage("What would you like to buy?");
        }
    }

    public void LogMessage(string message)
    {
        if (textScroll != null)
        {
            textScroll.text += "\n" + message;
            textScroll.ForceMeshUpdate();

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        else
        {
            Debug.Log("GameLogTMP is not assigned in the inspector.");
        }
    }
    
}

internal enum MenuStateEnum
{
    Splash = 0,
    MainMenu,
    CheckInventory,
    CheckNews,
    OrderSupplies,
    SetPrice,

}