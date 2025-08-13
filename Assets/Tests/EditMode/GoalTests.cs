using NUnit.Framework;
using UnityEngine;
using System.Linq;
using System;
[TestFixture]
public class GoalTests
{
#region GenerateGoals
    [Test]
    public void GenerateGoals_creates_double_cash_goal_for_company_with_BasicGrowthStrategy()
    {
        //arrange
        var company = EconAgent.Factory.Create("Test Company", AgentLevelEnum.Beginner, new BasicGrowthStrategy());
        company.GetStrategy().GenerateGoals(company);
        var expected_goal = new Goal("Double Initial Cash",
                                      "Double the initial cash of the company",
                                      null,
                                      (c, g) => g.SetOriginalValue("InitialCash", c.GetCash())
                                      );
        expected_goal.IsGoalMet = c => c.GetCash() >= expected_goal.GetOriginalValue<decimal>("InitialCash") * 2;
        //Act
        var actual_goal = company.Goals.Where(g => g.Name == "Double Initial Cash").FirstOrDefault();
        //Assert
        Assert.AreEqual(expected_goal, actual_goal);
    }
    [Test]
    public void GenerateGoals_creates_ten_lemonade_goal_for_company_with_BasicGrowthStrategy()
    {
        //arrange
        var company = 
                      EconAgentBuilder.For<EconAgent>()
                    .WithBehaviourStrategy(new BasicGrowthStrategy())
                    .AtLevel(AgentLevelEnum.Beginner)
                    .Named("Test Company")
                    .Build();

        company.GetStrategy().GenerateGoals(company);
        var expected_goal = new Goal("Have 10 Lemonade",
                                      "Have 10 Lemonade in stock",
                                      null,
                                      (c, g) => g.SetOriginalValue("InitialLemonade", 0)
                                      )
        {
            IsGoalMet = c => c.GetInventory().GetInventoryEntriesByGood("Lemonade").Sum(e => e.quantity) >= 10
        };
        //Act
        var actual_goal = company.Goals.Where(g => g.Name == "Have 10 Lemonade").FirstOrDefault();
        //Assert
        Assert.AreEqual(expected_goal, actual_goal);
    }

    [Test]
    public void GenerateGoalsCreatesReduceEnnuiGoalForReduceEnnuiStrategy()
    {
        // Arrange
        var company = PopulationAgent.PopulationAgentBuilder.Create()
            .Named("Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .WithInitialCash(1000)
            .WithBehaviourStrategy(new ReduceEnnuiStrategy())
            .Build();

        // Act
        company.GetStrategy().GenerateGoals(company);
        var goal = company.Goals.FirstOrDefault(g => g.Name == "Reduce Ennui");

        // Assert
        Assert.IsNotNull(goal, "Goal not found");
        Assert.AreEqual("Reduce Ennui", goal.Name, "Goal name does not match");
    }
#endregion
    [Test]
    public void IfPopulationCompanyAchievesNoEnnuiGoal_IsMetReturnsTrue()
    {
        // Arrange
        const float initialEnnui = .99f;
        var ReduceEnnuiGoal = new Goal()
            .Named("Reduce Ennui")
            .DescribedAs("Reduce the ennui of the population to 0")
            .WithGoalEvaluator(c => c is PopulationAgent populationCompany && populationCompany.Ennui == 0)
            .WithGoalInitializer((c, g) => g.SetOriginalValue("Ennui", initialEnnui));

        var company = PopulationAgent.PopulationAgentBuilder.Create()
            .Named("Population")
            .AtLevel(AgentLevelEnum.Beginner)
            .WithInitialCash(1000)
            .WithGoal(ReduceEnnuiGoal)
            .Build();
        
        company.Ennui=initialEnnui;
        
        const bool expected = true;
        const bool IsMetInitially = false;
        
        // Act
        var goal= company.Goals.FirstOrDefault(g => g.Name == "Reduce Ennui");
        var actualMetInitially = goal.IsGoalMet(company);
        company.Ennui = 0;
        var actualMetAfter = goal.IsGoalMet(company);
        
        // Assert
        Assert.IsNotNull(goal,"Goal not found");
        Assert.AreEqual(IsMetInitially, actualMetInitially, "Goal should not be met initially");
        Assert.AreEqual(expected, actualMetAfter, "Goal should be met after ennui is reduced to 0");
    }
    
    
        
}