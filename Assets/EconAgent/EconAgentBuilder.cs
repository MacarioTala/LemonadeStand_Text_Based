using System.Collections.Generic;
using UnityEngine;

public class EconAgentBuilder<T> where T : EconAgent
{
    private readonly T companyToReturn;

    public EconAgentBuilder(T company) => companyToReturn = company;
    public static EconAgentBuilder<T> Create() => new(ScriptableObject.CreateInstance<T>());
    public T Build() => companyToReturn;

    public EconAgentBuilder<T> WithFixedCostStrategy(iFixedCostStrategy fixedCostStrategy)
    {
        companyToReturn.FixedCostStrategy = fixedCostStrategy;
        return this;
    }
    public EconAgentBuilder<T> WithBehaviourStrategy(iStrategy behaviourStrategy)
    {
        companyToReturn.SetStrategy(behaviourStrategy);
        return this;
    }
    public EconAgentBuilder<T> WithInitialCash(decimal initialCash)
    {
        companyToReturn.SetCash(initialCash);
        return this;
    }
    public EconAgentBuilder<T> WithActionsPerTurn(int actionsPerTurn)
    {
        companyToReturn.SetActionsPerCycle(actionsPerTurn);
        return this;
    }
    public EconAgentBuilder<T> Named(string name)
    {
        companyToReturn.Name = name;
        return this;
    }

    public EconAgentBuilder<T> AtLevel(AgentLevelEnum companyLevel)
    {
        companyToReturn.companyLevel = companyLevel;
        return this;
    }

    public EconAgentBuilder<T> WithPopulation(int population)
    {
        if (companyToReturn is PopulationAgent populationCompany)
        {
            populationCompany.Population = population;
        }
        else
        {
            Debug.LogError("Company is not a PopulationCompany.");
        }
        return this;
    }

    public EconAgentBuilder<T> WithEnnui(float ennui)
    {
        if (companyToReturn is PopulationAgent populationCompany)
        {
            populationCompany.Ennui = ennui;
        }
        else
        {
            Debug.LogError("Company is not a PopulationCompany.");
        }
        return this;
    }

    public EconAgentBuilder<T> Demanding(Dictionary<Good, DemandData> demands)
    {
        if (companyToReturn is PopulationAgent populationAgent)
        {
            foreach (var demand in demands)
            {
                populationAgent.SetDemand(demand.Key, demand.Value);
            }
        }
        else
        {
            Debug.LogError("Economic Agent is not a PopulationAgent.");
        }
        return this;
    }
    public EconAgentBuilder<T> WithGoal(Goal goal)
    {
        companyToReturn.AddGoal(goal);
        return this;
    }

    public EconAgentBuilder<T> WithInventory(Inventory inventory)
    {
        companyToReturn.SetInventory(inventory);
        return this;
    }

    public EconAgentBuilder<T> AssumingNewGoodsCost(decimal value)
    {
        companyToReturn.SetMarketIgnorantAssumedCOG(value);
        return this;
    }
}

public static class EconAgentBuilder
{
    public static EconAgentBuilder<T> For<T>() where T : EconAgent 
            =>EconAgentBuilder<T>.Create();
}