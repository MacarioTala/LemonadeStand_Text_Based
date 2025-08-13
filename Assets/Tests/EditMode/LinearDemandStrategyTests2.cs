using System;
using System.Collections.Generic;
using NUnit.Framework;
using static TestHelpers;

public partial class LinearDemandStrategyTests
{
#region Integration Tests for AdjustDemandInPeriod

    [TestCase(TestName="AdjustDemandInPeriod: A good's demand should not change if the saturation elasticity is 1 and the good supply is equal to demand")]
    //You are here: Adjust this test case to use a PopulationCompany
    public void ADIP_SaturatedGoodElasticityOneDemandUnchanged()
    {
        //Arrange
        const int initialPopulation = 100;
        var strategy = StrategyBuilder.For<ReduceEnnuiStrategy>()
                      .WithAggressionLevel(1m)
                      .Build();
        var lemonadeDemand = new DemandData
                {
                    CurrentDemand=100,
                    MinDemand=0,
                    MaxDemand=100
                };
        var listOfDemands = new Dictionary<Good, DemandData>
                {
                    {Lemonade, lemonadeDemand}
                };
        var population = EconAgentBuilder.For<PopulationAgent>()
            .Named("Population Company")
            .WithInitialCash(10000)
            .AtLevel(AgentLevelEnum.Beginner)
            .WithPopulation(initialPopulation)
            .WithBehaviourStrategy(strategy)
            .WithEnnui(.99f)
            .Demanding(listOfDemands)
            .Build();
        TestMarket.RegisterMarketParticipant(population);

        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, 1);
        var Company1SellsLemonadeToAnyone = new Order(null,Company1,Lemonade,100,1);
        _= Company1.GetInventory().AddGood(new InventoryEntry(Lemonade, 100, 1, TestMarket.CurrentPeriod));
        Company1.QueueOrder(CreateActionContext(Company1SellsLemonadeToAnyone, TestMarket,0));
        var expectedDemand = 100;

        //Act
        TestDemandStrategy.AdjustDemandInPeriod(TestMarket);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
    [TestCase(TestName="AdjutDemandInPeriod: A good's demand should double if the saturation elasticity is 1 and the good is supplied at zero")]
    public void ADIP_UndersuppliedGoodElasticityOneDemandDoubles()
    {
        //Arrange
        var maxDemand = 100;
        var demandForLemonade = new DemandData { MinDemand = 0, MaxDemand = maxDemand };
        TestPopulation.SetDemand(Lemonade, demandForLemonade);
        var marketDemand = TestMarket.GetPopulationDemand();
        var lemonadeDemand = marketDemand[Lemonade];

        Lemonade.Elasticities.Add(ElasticityTypeEnum.SaturationElasticity, 1);
        var expectedDemand = 200;
        //Act
        TestDemandStrategy.AdjustDemandInPeriod(TestMarket);
        var actualDemand = TestMarket.GetPopulationDemand()[Lemonade].CurrentDemand;
        //Assert
        Assert.AreEqual(expectedDemand, actualDemand);
    }
#endregion
}