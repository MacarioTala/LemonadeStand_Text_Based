using System;
using System.Collections.Generic;
using System.Linq;

public class BasicDemographicManager : iDemographicManager
{
    Market _market;
    public void SetMarket(Market market)=> _market = market;
    
    //PIT Demographics
    int _population;
    float _populationEnnui;
    float _populationGrowthRate;
    float _populationHappiness;
    float _marketInstability;
    //Demographics History
    iDataHandler<PopulationHistory> _populationHistoryHandler;

    //Set Handlers
    public iDemographicManager SetPopulationHistoryHandler(iDataHandler<PopulationHistory> handler)
    {
        _populationHistoryHandler = handler;
        return this;
    }   

    public float GetMarketInstability()
    {
        return _marketInstability;
    }

    public int GetPopulation()
    {
        var economicActors = _market.GetMarketParticipants();
        // Sum the population of all population companies
        _population = economicActors
            .OfType<PopulationAgent>()
            .Sum(company => company.Population);
        return _population;
    }

    public float GetPopulationEnnui()
    {
        return _populationEnnui;
    }

    public float GetPopulationGrowthRate(int startingPeriod,int endingPeriod)
    {
        var populationHistory = _populationHistoryHandler
                    .LoadHistorical(
                                    x => x.Period >= startingPeriod 
                                    && x.Period <= endingPeriod
                                    );
        throw new NotImplementedException();
        
    }

    public float GetPopulationHappiness()
    {
        return _populationHappiness;
    }

    public List<PopulationHistory> GetPopulationHistory(Guid marketId)
    {
        return _populationHistoryHandler
                    .LoadHistorical(
                                    x => x.MarketId == marketId
                                    ).ToList();
    }

    public LemonadeStandResultObject RecordDemographicSnapshot(Guid marketId, int period, TurnPhase phase)
    {
        SavePopulationHistory(marketId, period, phase);

        return LemonadeStandResultObject.Success();
    }

    public LemonadeStandResultObject SetMarketInstability(float newInstability)
    {
        if (newInstability < 0 || newInstability > 1)
        {
            return LemonadeStandResultObject.Failure(ResultTypeEnum.NumericRangeExceeded,"Market Instability must be between 0 and 1");
        }
        _marketInstability = newInstability;
        return LemonadeStandResultObject.Success();
    }

    public LemonadeStandResultObject SetPopulation(int newPopulation , PopulationAgent marketParticipant)
    {
        marketParticipant.Population = newPopulation;
        return LemonadeStandResultObject.Success();
    }

    public LemonadeStandResultObject SetPopulationHappiness(float newHappiness)
    {
        _populationHappiness = newHappiness;
        return LemonadeStandResultObject.Success();
    }

    //Privates
    private void SavePopulationHistory(Guid marketId, int period, TurnPhase phase)
    {
        var population = GetPopulation();
        var currentPopulationHistory = new PopulationHistory()
        {
            MarketId = marketId,
            Period = period,
            Population = population,
            Phase = phase
        };
        _populationHistoryHandler.Save(currentPopulationHistory);
    }
}