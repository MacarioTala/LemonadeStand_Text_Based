using System;
using System.Collections.Generic;

public class Goal
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Func<iEconAgent, bool> IsGoalMet { get; set; }
    public Action<EconAgent,Goal> InitializeGoal { get; set; }
    public MetricEnum AffectsMetric { get; set; }
    public float MetricTarget { get; set; }
    public bool IsReduce=true;

    public bool IsAchieved=false;

    public Dictionary<string,object> OriginalValues {get;private set;} = new ();

    public Goal(string name, string description, Func<iEconAgent, bool> isGoalMet,Action<EconAgent,Goal> initializeGoal=null)
    {
        Name = name;
        Description = description;
        IsGoalMet = isGoalMet;
        InitializeGoal = initializeGoal;
    }
    public Goal()
    {
        
    }

    public T GetOriginalValue<T>(string key)
    {
        return OriginalValues.ContainsKey(key) ? (T)OriginalValues[key] : default;
    }

    public void SetOriginalValue(string key, object value)
    {
        if (OriginalValues.ContainsKey(key))
        {
            OriginalValues[key] = value;
        }
        else
        {
            OriginalValues.Add(key, value);
        }
    }
    public void Initialize(EconAgent company)
    {
        InitializeGoal?.Invoke(company,this);
    }
    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object other)
    {
        if (other is Goal goal)
        {
            return Name == goal.Name;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public static class GoalBuilder
{
    
    public static Goal Named(this Goal goal,string name)
    {
        goal.Name = name;
        return goal;
    }

    public static Goal DescribedAs(this Goal goal, string description)
    {
        goal.Description = description;
        return goal;
    }

    public static Goal WithGoalEvaluator(this Goal goal, Func<iEconAgent, bool> goalEvaluator)
    {
        goal.IsGoalMet = goalEvaluator;
        return goal;
    }

    public static Goal WithGoalInitializer(this Goal goal, Action<EconAgent,Goal> goalInitializer)
    {
        goal.InitializeGoal = goalInitializer;
        return goal;
    }
    public static Goal Affecting(this Goal goal, MetricEnum metric)
    {
        goal.AffectsMetric = metric;
        return goal;
    }

    public static Goal WithGoalValue(this Goal goal, float value)
    {
        goal.MetricTarget = value;
        return goal;
    }
}