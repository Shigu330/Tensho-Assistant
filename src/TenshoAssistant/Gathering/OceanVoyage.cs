namespace TenshoAssistant.Gathering;

/// <summary>
/// R-07 (0.418): what Tensho read of the ocean fishing voyage in progress (FFXIVClientStructs InstanceContentOceanFishing, named fields):
/// route (IKDRoute row), stop index 0-2, status (0 waiting, 1 switching zone, 2 fishing, 3 new zone, 4 finished), spectral current,
/// weather, the three missions (IKDPlayerMissionCondition row, progress), the player's points and fish caught.
/// </summary>
public sealed record OceanVoyageFrame(uint RouteId,int Zone,int Status,bool Spectral,uint WeatherId,
    IReadOnlyList<(uint Type,int Progress)> Missions,int Points,int Caught,DateTimeOffset ObservedAt);

/// <summary>An ocean fishing place (IKDSpot): its name and its two fishing spots, the usual one and the spectral current's.</summary>
public sealed record OceanSpot(uint Id,string Place,uint MainSpot,uint SpectralSpot);

/// <summary>A mission (IKDPlayerMissionCondition): its text in the game's language and the number to reach.</summary>
public sealed record OceanMission(string Text,int Target);

/// <summary>
/// The game's ocean fishing data, copied once: routes (IKDRoute: places and moment rows of the three stops), places (IKDSpot), the
/// fish of each fishing spot (FishingSpot.Item) and the missions. docs/evidence/0.418-ocean-voyage.md.
/// </summary>
public sealed record OceanVoyageData(IReadOnlyDictionary<uint,(uint[] Spots,uint[] Times)> Routes,IReadOnlyDictionary<uint,OceanSpot> Spots,
    IReadOnlyDictionary<uint,IReadOnlyList<uint>> SpotFish,IReadOnlyDictionary<uint,OceanMission> Missions)
{
    /// <summary>X-85 V1 (0.917): the territories of the ocean fishing spots (FishingSpot.TerritoryType of every IKDSpot spot), read in the game data.</summary>
    public IReadOnlySet<uint> Territories { get; init; }=new HashSet<uint>();
}

/// <summary>The stop being fished: its place, moment, the spot (spectral or not) and its fish; the next stop when there is one.</summary>
public sealed record OceanStopView(int Index,OceanSpot Spot,OceanTime Time,bool Spectral,IReadOnlyList<uint> Fish,OceanSpot? NextSpot,OceanTime NextTime);

public static class OceanVoyages
{
    public static OceanStopView? Current(OceanVoyageData data,OceanVoyageFrame frame)
    {
        if(frame.Zone is < 0 or > 2 || !data.Routes.TryGetValue(frame.RouteId,out var route) || route.Spots.Length<=frame.Zone
           || !data.Spots.TryGetValue(route.Spots[frame.Zone],out var spot))return null;
        var spotId=frame.Spectral ? spot.SpectralSpot:spot.MainSpot;
        var fish=data.SpotFish.GetValueOrDefault(spotId) ?? [];
        var time=route.Times.Length>frame.Zone ? OceanSchedule.TimeOf(route.Times[frame.Zone]):OceanTime.Unknown;
        OceanSpot? next=null;var nextTime=OceanTime.Unknown;
        if(frame.Zone+1<route.Spots.Length && data.Spots.TryGetValue(route.Spots[frame.Zone+1],out var following))
        {next=following;nextTime=route.Times.Length>frame.Zone+1 ? OceanSchedule.TimeOf(route.Times[frame.Zone+1]):OceanTime.Unknown;}
        return new(frame.Zone,spot,time,frame.Spectral,fish,next,nextTime);
    }

    /// <summary>The missions as (text, progress, target); a type the data does not know is left out.</summary>
    public static IReadOnlyList<(string Text,int Progress,int Target)> Missions(OceanVoyageData data,OceanVoyageFrame frame)
        =>frame.Missions.Where(mission=>mission.Type!=0 && data.Missions.ContainsKey(mission.Type))
            .Select(mission=>(data.Missions[mission.Type].Text,mission.Progress,data.Missions[mission.Type].Target)).ToArray();
}
