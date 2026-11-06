using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace TenshoAssistant.Island;

/// <summary>
/// N-10 (0.671): reads the island, on the player's click and on the framework thread. Field reads only: Tensho never asks the server for the demand and never
/// schedules anything. The game fills supply and demand once the player has opened the agenda; before that the reading says so.
/// </summary>
internal static class IslandReader
{
    public static unsafe IslandSnapshot Read(IslandCatalog catalog)
    {
        var now=DateTimeOffset.UtcNow;
        // R-14: an address lost after a game update throws; null below only means the player is elsewhere.
        if((nint)MJIManager.StaticAddressPointers.ppInstance==0)throw new InvalidOperationException("MJIManager did not resolve.");
        var manager=MJIManager.Instance();
        if(manager==null || !manager->IsPlayerInSanctuary)return IslandSnapshot.Of(IslandReadStatus.NotOnIsland,now);
        var rank=(int)manager->IslandState.CurrentRank;
        var supplyDemand=manager->SupplyAndDemandShifts.ToArray();
        var status=IslandValue.Check(catalog,rank,manager->CurrentPopularity,manager->NextPopularity,manager->DemandDirty,supplyDemand);
        var slots=new (int Index,int Level,bool Building,int Place,int Marker)[4];
        for(var index=0;index<4;index++)
        {
            var workshop=manager->IslandState.Workshops[index];
            slots[index]=(index,workshop.BuildingLevel+1,workshop.UnderConstruction,workshop.PlaceId,workshop.HoursToCompletion);
        }
        var workshops=IslandValue.BuiltWorkshops(slots);
        var landmarks=0;
        foreach(var landmark in manager->LandmarkPlacements)
            if(landmark.LandmarkId!=0 && landmark.UnderConstruction==0)landmarks++;
        uint? groove=null;
        var agent=AgentMJICraftSchedule.Instance();
        if(agent!=null && agent->Data!=null && agent->Data->UpdateState==2)groove=agent->Data->Groove;
        return new(status,now,rank,workshops,groove,manager->CurrentGroove,manager->CurrentCycleDay,manager->CurrentPopularity,manager->NextPopularity,supplyDemand,landmarks);
    }
}
