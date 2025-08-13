using System;
using System.Collections.Generic;

public class MockDemographicManager : iDemographicManager
{
    private float _marketInstability = 0f; // Stable market by default
    private int _population; // Default population
    iDataHandler<PopulationHistory> _populationHistoryHandler;
    IEnumerable<PopulationHistory> _populationHistory = new List<PopulationHistory>();
    public float GetMarketInstability()
    {
        return _marketInstability;
    }

    public int GetPopulation()
    {
        return _population;
    }

    public float GetPopulationEnnui()
    {
        throw new NotImplementedException();
    }

    public float GetPopulationGrowthRate(int startingPeriod, int endingPeriod)
    {
        throw new NotImplementedException();
    }

    public float GetPopulationHappiness()
    {
        throw new NotImplementedException();
    }

    public List<PopulationHistory> GetPopulationHistory(Guid marketid)
    {
        return _populationHistory != null ? new List<PopulationHistory>(_populationHistory) : new List<PopulationHistory>();
    }

    public LemonadeStandResultObject RecordDemographicSnapshot(Guid marketId, int period,TurnPhase phase)
    {
        ((List<PopulationHistory>)_populationHistory).Add(new PopulationHistory
        {
            MarketId = marketId,
            Period = period,
            Population = _population,
            Phase = phase
        });
        return LemonadeStandResultObject.Success();
    }

    public LemonadeStandResultObject SetMarketInstability(float newInstability)
    {
        _marketInstability = newInstability;
        return LemonadeStandResultObject.Success();
    }

    public LemonadeStandResultObject SetPopulation(int newPopulation, PopulationAgent marketParticipant)
    {
        _population = newPopulation;
        return LemonadeStandResultObject.Success();
    }

     public void SetPopulationHistory(IEnumerable<PopulationHistory> populationHistory)
    {
        _populationHistory = populationHistory;
    }

    public LemonadeStandResultObject SetPopulationHappiness(float newHappiness)
    {
        throw new NotImplementedException();
    }

    public iDemographicManager SetPopulationHistoryHandler(iDataHandler<PopulationHistory> handler)
    {
        _populationHistoryHandler = handler;
        return this;
    }

    public void SetMarket(Market market)
    {
        // This method is intentionally left empty for the mock implementation.
        // In a real implementation, you would set the market reference here.
    }
}