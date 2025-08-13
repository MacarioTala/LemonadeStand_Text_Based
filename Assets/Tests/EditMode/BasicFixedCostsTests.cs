using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BasicFixedCostsTests
{
    /// <summary>
    /// Note: All of these tests use the BasicFixedCost Strategy implementation
    /// It was too much trouble to refactor the tests to just test the BasicFixedCost Strategy
    /// If a kind soul wants to refactor the tests to JUST test the BasicFixedCost Strategy, please do so
    /// </summary>
    [Test]
    public void CalcFixedCostsForPeriodSumsAllValidCosts()
    {
        // Arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner,null, new BasicFixedCostStrategy());
        
        var rent = new FixedCost
        {
            Description = "Rent",
            FixedCostType = FixedCostEnum.Rent,
            Amount = 1000,
            Frequency = 1,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(rent);
        var salaries = new FixedCost
        {
            Description = "Salaries",
            FixedCostType = FixedCostEnum.Salaries,
            Amount = 5000,
            Frequency = 1,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(salaries);
        var expected = 6000m;
        // Act
        var actual = company1.CalculateFixedCostsForPeriod(2);
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void WhenCalculatingForPeriod2FixedCostsShouldIgnoreCostsWithFrequency2()
    {
        // Arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner,null, new BasicFixedCostStrategy());
        var rent = new FixedCost
        {
            Description = "Rent",
            FixedCostType = FixedCostEnum.Rent,
            Amount = 1000,
            Frequency = 2,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(rent);
        var salaries = new FixedCost
        {
            Description = "Salaries",
            FixedCostType = FixedCostEnum.Salaries,
            Amount = 5000,
            Frequency = 1,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(salaries);
        var expected = 5000m;
        // Act
        var actual = company1.CalculateFixedCostsForPeriod(2);
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void WhenCalculatingForPeriod2FixedCostsShouldIgnoreCostsAcquiredInPeriod2()
    {
        // Arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner,null, new BasicFixedCostStrategy());
        var rent = new FixedCost
        {
            Description = "Rent",
            FixedCostType = FixedCostEnum.Rent,
            Amount = 1000,
            Frequency = 1,
            PeriodAcquired = 2
        };
        company1.FixedCosts.Add(rent);
        var salaries = new FixedCost
        {
            Description = "Salaries",
            FixedCostType = FixedCostEnum.Salaries,
            Amount = 5000,
            Frequency = 1,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(salaries);
        var expected = 5000m;
        // Act
        var actual = company1.CalculateFixedCostsForPeriod(2);
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void FixedCostsWithInvalidFrequencyShouldThrowException()
    {
        // Arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner,null, new BasicFixedCostStrategy());
        var rent = new FixedCost
        {
            Description = "Rent",
            FixedCostType = FixedCostEnum.Rent,
            Amount = 1000,
            Frequency = 0,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(rent);
        // Act and Assert
        Assert.Throws<System.ArgumentException>(() => company1.CalculateFixedCostsForPeriod(2));
    }
    [Test]
    public void IfMultipleFixedCostsExistAcquiredInMultiplePeriodsOnlySumValidCosts()
    {
        //arrange
       var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner,null, new BasicFixedCostStrategy());
        var rent = new FixedCost
        {
            Description = "Rent",
            FixedCostType = FixedCostEnum.Rent,
            Amount = 1000,
            Frequency = 1,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(rent);
        var salaries = new FixedCost
        {
            Description = "Salaries",
            FixedCostType = FixedCostEnum.Salaries,
            Amount = 5000,
            Frequency = 2,
            PeriodAcquired = 2
        };
        company1.FixedCosts.Add(salaries);
        var roughNeighbourhood = new FixedCost
        {
            Description = "Rough Neighbourhood",
            FixedCostType = FixedCostEnum.Other,
            Amount = 10000,
            Frequency = 1,
            PeriodAcquired = 3
        };
        company1.FixedCosts.Add(roughNeighbourhood);
        var Insurance = new FixedCost
        {
            Description = "Insurance",
            FixedCostType = FixedCostEnum.Insurance,
            Amount = 5000,
            Frequency = 1,
            PeriodAcquired = 4
        };
        company1.FixedCosts.Add(Insurance);
        var Utilities = new FixedCost
        {
            Description = "Utilities",
            FixedCostType = FixedCostEnum.Utilities,
            Amount = 5000,
            Frequency = 4,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(Utilities);
        var period = 4;
        var expected = 16000m;
        //act
        var actual = company1.CalculateFixedCostsForPeriod(period);
        //assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FixedCostsForFreq2AcquiredOnP1IsNotIncludedInP4()
    {
        //arrange
       var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner,null, new BasicFixedCostStrategy());
        var rent = new FixedCost
        {
            Description = "Rent",
            FixedCostType = FixedCostEnum.Rent,
            Amount = 1000,
            Frequency = 2,
            PeriodAcquired = 1
        };
        company1.FixedCosts.Add(rent);
        var period = 4;
        var expected = 0m;
        //act
        var actual = company1.CalculateFixedCostsForPeriod(period);
        //assert
        Assert.AreEqual(expected, actual); 
    }
    [Test]
    public void IfFixedCostsAreEmptyReturnZero()
    {
        //arrange
        var company1 = EconAgent.Factory.Create("Company 1", AgentLevelEnum.Beginner,null, new BasicFixedCostStrategy());
        var period = 4;
        var expected = 0m;
        //act
        var actual = company1.CalculateFixedCostsForPeriod(period);
        //assert
        Assert.AreEqual(expected, actual);
    }
}