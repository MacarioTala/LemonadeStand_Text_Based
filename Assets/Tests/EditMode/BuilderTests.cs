using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BuilderTests
{
#region Markets
    [Test]
    public void MarketBuilderWithDemandStrategyShouldSetDemandStrategy()
    {
        // Arrange
        var testDemandStrategy = ScriptableObject.CreateInstance<LinearDemandStrategy>();
        var market = Market.Create()
                    .WithDemandStrategy(testDemandStrategy);

        // Act
        market.WithDemandStrategy(testDemandStrategy);

        // Assert
        Assert.AreEqual(testDemandStrategy, market.DemandStrategy);
    }
#endregion

#region Companies
    [Test]
    public void CompanyNamedShouldSetName()
    {
        // Arrange
        const string expectedName = "Test Company";
        var testCompany = EconAgentBuilder.For<EconAgent>()
                    .Named(expectedName)
                    .Build();
        // Act
        var actual = testCompany.Name;
        // Assert
        Assert.AreEqual(expectedName, actual);
    }
    [Test]
    public void UsingTheBuilderToBuildPopulationCompanyShouldReturnPopulationCompany()
    {
        //Arrange
        var testCompany = EconAgentBuilder.For<PopulationAgent>()
                    .Build();
        var expected = typeof(PopulationAgent);
        //Act
        var actual = testCompany.GetType();
        //Assert
        Assert.AreEqual(expected, actual);
        Assert.IsTrue(testCompany is PopulationAgent);
    }
#endregion
}