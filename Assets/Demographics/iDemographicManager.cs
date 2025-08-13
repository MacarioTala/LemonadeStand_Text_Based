using System;
using System.Collections.Generic;

public interface iDemographicManager
{
    //PIT Demographics
    float GetMarketInstability();
    int GetPopulation();
    float GetPopulationEnnui();
    float GetPopulationGrowthRate(int startingPeriod, int endingPeriod);
    float GetPopulationHappiness();

    //Set Datahandlers
    void SetMarket(Market market);
    iDemographicManager SetPopulationHistoryHandler(iDataHandler<PopulationHistory> handler);

    LemonadeStandResultObject SetMarketInstability(float newInstability); 
    LemonadeStandResultObject SetPopulation(int newPopulation, PopulationAgent marketParticipant);
    LemonadeStandResultObject SetPopulationHappiness(float newHappiness);

    //Demographics History
    List<PopulationHistory> GetPopulationHistory(Guid marketId);
    
    //Record History
    LemonadeStandResultObject RecordDemographicSnapshot(Guid marketId, int period, TurnPhase phase);
#region default implementations
string GetEnnuiLevel()
    {
        if (GetPopulationEnnui() == 0) return "Inspired";
        if (GetPopulationEnnui() > 0 && GetPopulationEnnui() < .25) return "Thriving";
        if (GetPopulationEnnui() >= .25 && GetPopulationEnnui() < .5) return "Content";
        if (GetPopulationEnnui() >= .5 && GetPopulationEnnui() < .75) return "Dissatisfied";
        if (GetPopulationEnnui() >= .75 && GetPopulationEnnui() < 1) return "Bored";
        if (GetPopulationEnnui() == 1) return "Bored to death";
        return "Unknown";
    }
#endregion
}