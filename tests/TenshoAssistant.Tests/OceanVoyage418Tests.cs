using TenshoAssistant.Gathering;

// R-07 (0.418): the ocean fishing voyage in progress — stop, moment, spectral spot, next stop, missions.
internal static class OceanVoyage418Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // Rows of docs/evidence/0.418-ocean-voyage.md: route 1 = spots 2,1,3 at moments 1,2,3; spot 1 = 237 / 238; missions 1 and 4.
    private static OceanVoyageData Data()=>new(
        new Dictionary<uint,(uint[],uint[])>{[1]=([2,1,3],[1,2,3])},
        new Dictionary<uint,OceanSpot>{[1]=new(1,"Galadion",237,238),[2]=new(2,"Merlthor S",239,240),[3]=new(3,"Merlthor N",243,244)},
        new Dictionary<uint,IReadOnlyList<uint>>{[237]=[10,11],[238]=[20],[239]=[30]},
        new Dictionary<uint,OceanMission>{[1]=new("Catch jellyfish or crabs",7),[4]=new("Catch fish with a weak bite (!)",34)});
    private static OceanVoyageFrame Frame(int zone,bool spectral)=>new(1,zone,2,spectral,0,[(1,3),(4,34),(99,1)],1200,9,DateTimeOffset.UnixEpoch);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean voyage: the stop, its moment and fish, the spectral spot, the next stop",() =>
        {
            var data=Data();
            var second=OceanVoyages.Current(data,Frame(1,false))!;
            Check(second.Spot.Place=="Galadion" && second.Time==OceanTime.Day && second.Fish.SequenceEqual([10u,11u]),"stop 2: place, moment row 2 = day, main spot fish");
            Check(second.NextSpot?.Place=="Merlthor N" && second.NextTime==OceanTime.Sunset,"next stop");
            Check(OceanVoyages.Current(data,Frame(1,true))!.Fish.SequenceEqual([20u]),"spectral current: the sub spot");
            Check(OceanVoyages.Current(data,Frame(2,false)) is { NextSpot:null, Fish.Count:0 },"last stop: no next; unknown spot fish: none");
            Check(OceanVoyages.Current(data,Frame(3,false)) is null && OceanVoyages.Current(data,Frame(0,false) with {RouteId=7}) is null,"out of range or unknown route");
        }),
        ("Ocean voyage: missions with their target, unknown types left out; reader checks the game functions",() =>
        {
            var missions=OceanVoyages.Missions(Data(),Frame(0,false));
            Check(missions.Count==2 && missions[0]==("Catch jellyfish or crabs",3,7) && missions[1].Progress==missions[1].Target,"missions");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/OceanVoyageReader.Dalamud.cs"));
            Check(reader.Contains("MemberFunctionPointers.GetContentDirector==0") && reader.Contains("MemberFunctionPointers.GetInstanceContentDirector==0")
                && reader.Contains("framework.IsInFrameworkUpdateThread") && reader.Contains("zone is < 0 or > 2"),"addresses checked, framework thread, bounds");
        }),
    ];
}
