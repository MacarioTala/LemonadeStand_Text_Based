using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BankruptcyTests
{
    private readonly TheEconomy testEconomy=TheEconomy.Instance;
    Market TestMarket;
    EconAgent Company1;

    readonly iFixedCostStrategy TestFixedCostStrategy = new BasicFixedCostStrategy();
    readonly iDemandStrategy TestDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
    readonly iMarketDataService TestMarketDataService = new MockMarketDataService();
    readonly iDemographicManager TestDemographicManager = new MockDemographicManager();
    readonly iSupplyProvider TestSupplyProvider = new MockSupplyProvider();

    [SetUp]
    public void Setup()
    {
        TheEconomy.SetupForTests(new MockLogger());
        var existingMarket = TheEconomy.Instance.GetMarketByName("The First Market");
        testEconomy.RemoveMarket(existingMarket);

        TestMarket= Market.Factory.CreateMarket("Test Market", AgentLevelEnum.Market)
                            .WithDataService(TestMarketDataService)
                            .WithDemographicManager(TestDemographicManager)
                            .WithSupplyProvider(TestSupplyProvider)
                            .WithDemandStrategy(TestDemandStrategy)
                            .WithTradeProcessor(new BasicTradeProcessor())
                            .WithTransactionManager(new BasicTransactionManager())
                            .WithPriceManager(new BasicPriceManager());

        TestSupplyProvider.Initialize(TestMarket);
        testEconomy.RegisterCompany(TestMarket);

        //Set Demographics
        var populationHistory = new List<PopulationHistory>() 
        {
            new() {MarketId=TestMarket.MarketId,Period=0, Population=1000},
            new() {MarketId=TestMarket.MarketId,Period=1, Population=1000},
            new() {MarketId=TestMarket.MarketId,Period=2, Population=1000},
        };
        ((MockMarketDataService)TestMarketDataService).SetPopulationHistory(populationHistory);

        Company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner, null, TestFixedCostStrategy);
        TestMarket.RegisterMarketParticipant(Company1);
    }

    [Test]
    public void CompanyWithoutCashGoesBankrupt()
    {
        //Arrange
        Company1.SetCash(0);
        var expected = true;
        //Act
        testEconomy.EndTradingPeriod();
        var actual = Company1.IsBankrupt();
        //Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void CompanyGoesBankruptWhenItCannotPayFixedCosts()
    {
        //Entities currently go bankrupt as soon as they hit zero cash
        //Change this behaviour when we implement TheEconomy.StartTradingPeriod()

        //Arrange
        const int tradingCycles=2;
        Company1.SetCash(1000);
 
        var rent = new FixedCost
        {
            Description = "Rent",
            FixedCostType = FixedCostEnum.Rent,
            Amount = 1000,
            Frequency = 1,
            PeriodAcquired = 0
        };
        Company1.FixedCosts.Add(rent);
        var expected = true;
        //Act
        for (int i = 0; i < tradingCycles; i++)
        {
            Debug.Log("Trading cycle: " + i);
            testEconomy.EndTradingPeriod();
        }
        var actual = Company1.IsBankrupt();
        //Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ACompanyGoingBankruptShouldBeRemovedFromItsMarket()
    {
        //Arrange
        Company1.SetCash(0);
        var originalMarket = Company1.GetMarket();

        //Act
        testEconomy.EndTradingPeriod();
        var currentMarket = Company1.GetMarket();

        //Assert
        Assert.IsTrue(originalMarket != null,$"Original market should not be null");
        Assert.IsTrue(originalMarket != currentMarket,"Original market should not be the same as the current market");
        Assert.IsTrue(currentMarket == null,$"After company goes bankrupt, market should be null, but was {currentMarket}");
    }

    [TearDown]
    public void TearDown()
    {
        if(testEconomy != null)
        {
            Object.DestroyImmediate(testEconomy.gameObject);
        }
        //Cleanup
        if (Company1 != null)
        {
            TestMarket.RemoveMarketParticipant(Company1);
            Company1 = null;
        }
        if (TestMarket != null)
        {
            testEconomy.RemoveMarket(TestMarket);
            TestMarket = null;
        }
    }
}
