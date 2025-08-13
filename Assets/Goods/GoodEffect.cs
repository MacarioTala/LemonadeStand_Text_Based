using System;

public class GoodEffect
{
    public string Name {get;internal set;}
    public string Description {get; internal set;}
    public MetricEnum AffectsMetric {get; internal set;}
    public MetricModifier<PopulationAgent> Effect{get; internal set;}
    public float Magnitude {get; internal set;}
    public bool IsReduce {get => Magnitude < 0;}

    public void Apply(PopulationAgent company,float percentageToApply=1f)
    {
        Effect?.Modify(company, percentageToApply*Magnitude);
    }
}

public static class GoodEffectBuilder
{
    public static GoodEffect Create()
    {
        return new GoodEffect();
    }
    public static GoodEffect Named(this GoodEffect effect, string name)
    {
        effect.Name = name;
        return effect;
    }
    public static GoodEffect DescribedAs(this GoodEffect effect, string description)
    {
        effect.Description = description;
        return effect;
    }
    public static GoodEffect Affecting(this GoodEffect effect, MetricEnum metric)
    {
        effect.AffectsMetric = metric;
        return effect;
    }
    public static GoodEffect WithEffect(this GoodEffect effect, MetricModifier<PopulationAgent> func)
    {
        effect.Effect = func;
        return effect;
    }
    public static GoodEffect WithEffectMagnitude(this GoodEffect effect, float magnitude)
    {
        if (magnitude < -1 || magnitude > 1)
            throw new ArgumentOutOfRangeException(nameof(magnitude), "Magnitude must be between 0 and 1.");
        effect.Magnitude = magnitude;
        return effect;
    }
}