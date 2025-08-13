using System;
using System.Linq;
using UnityEngine;

[Tooltip("Population change as a percentage. Use negative values to decrease population.")]
public class ChangePopulationEffect : iMarketEffect
{
    iMarketEvent _parentEvent;
    public float PopulationChangePercentage;
    float _originalPopulationChangePercentage;
    float _conversion => PopulationChangePercentage / 100f;
    
    public void Apply(Market market)
    {
        var marketPopulations = market.GetMarketParticipants()
            .Where(x => x is PopulationAgent)
            .ToList();

        foreach (PopulationAgent populationCompany in marketPopulations)
        {
            var population = populationCompany.Population;
            var delta = (int)Math.Round(population * _conversion,0);
            populationCompany.Population += delta;
        }
    }

    public ChangePopulationEffect(float populationChangePercentage)
    {
        PopulationChangePercentage = populationChangePercentage;
        _originalPopulationChangePercentage = populationChangePercentage;
    }

    public void ChangeEffectMultiplier(float multiplier)
    {
        PopulationChangePercentage *= multiplier;
    }

    public void Reset()
    {
        PopulationChangePercentage = _originalPopulationChangePercentage;
    }
    public void SaveOriginalState()
    {
        //Noop: Original state is saved in constructor
    }
    public void SetParentEvent(iMarketEvent marketEvent)
    {
        _parentEvent = marketEvent;
    }

    public iMarketEvent GetParentEvent()
    {
        return _parentEvent;
    }
}
