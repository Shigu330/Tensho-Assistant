using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.FreeCompany;

/// <summary>State of the airship leg table: the table for the speed asked, or why there is none.</summary>
public sealed record AirshipTableState(AirshipLegTable? Table,int RequestedSpeed,bool Unavailable,bool NotLoggedIn);

/// <summary>
/// DEC-023, point 3 (0.337, user's go-ahead): asks the game's own voyage functions for the airship leg times and distances
/// (HousingManager.GetAirshipVoyageTimeAndDistance, GetAirshipSurveyDuration: static, they only compute; nothing is sent to the server).
/// Only on the framework thread, only while logged in, only when the page asks for a new speed, once per speed (a few hundred calls),
/// only when the game resolved both functions (pointer checked before any call); any failure leaves the table « unavailable » for the
/// session (R-14). Evidence: docs/evidence/0.336-airship-sectors.md. Removed on dispose.
/// X-201 (0.1248, slice 2): the tables are kept by speed (AirshipSpeeds), so the page can give each registered airship its best route: the
/// page's speed first, then the other speeds asked (RequestAlso, only while the fleet list is open), one speed per framework update.
/// </summary>
public sealed unsafe class AirshipVoyageTable : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly object gate=new();
    private int requestedSpeed;
    private uint[] requestedPoints=[];
    private uint[]? computedFor;
    private bool disposed,failed;
    private AirshipTableState state=new(null,0,false,false);
    private int[] otherSpeeds=[];
    private uint[]? tablesFor;
    private IReadOnlyDictionary<int,AirshipLegTable> tables=new Dictionary<int,AirshipLegTable>();

    public AirshipTableState State=>Volatile.Read(ref state);

    /// <summary>X-201 (0.1248): the table of this speed once computed (over the sectors of the last request), else null.</summary>
    public AirshipLegTable? TableFor(int speed)=>Volatile.Read(ref tables).TryGetValue(Math.Clamp(speed,1,short.MaxValue),out var table) ? table : null;

    /// <summary>X-201 (0.1248): other speeds to compute after the page's own (UI thread); an empty list asks none.</summary>
    public void RequestAlso(IEnumerable<int> speeds)
    {
        var wanted=AirshipSpeeds.Wanted(speeds);
        lock(gate){if(!disposed && !wanted.SequenceEqual(otherSpeeds))otherSpeeds=wanted;}
    }

    public AirshipVoyageTable(IFramework framework,IClientState client,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>Asks for the table of this speed over these sectors (UI thread); computed at the next framework update if it changed.</summary>
    public void Request(int speed,IReadOnlyList<uint> points)
    {
        speed=Math.Clamp(speed,1,short.MaxValue);
        // Filtered before the comparison, so a request the filter changes is not taken as new on every frame (0.337 review).
        var wanted=points.Where(point=>point<=byte.MaxValue && point!=AirshipPlanner.Start).Distinct().ToArray();
        lock(gate)
        {
            if(disposed || speed==requestedSpeed && wanted.SequenceEqual(requestedPoints))return;
            requestedSpeed=speed;requestedPoints=wanted;
        }
    }

    private void OnUpdate(IFramework _)
    {
        int speed;uint[] points;int[] others;
        lock(gate)
        {
            if(disposed || requestedSpeed==0)return;
            speed=requestedSpeed;points=requestedPoints;others=otherSpeeds;
        }
        // X-201 (0.1248): the kept tables belong to the sectors they were computed over.
        if(!ReferenceEquals(tablesFor,points)){tablesFor=points;PublishTables(new Dictionary<int,AirshipLegTable>());}
        // Already computed for this request (the points array is replaced on every new request); otherwise publish only what changed.
        var current=State;
        var mainDone=current.Table is { } done && done.Speed==speed && ReferenceEquals(computedFor,points);
        var kept=Volatile.Read(ref tables);
        if(!mainDone && kept.TryGetValue(speed,out var cached)){computedFor=points;Publish(new(cached,speed,false,false));return;}
        if(AirshipSpeeds.Next(speed,others,kept) is not { } next)return;
        if(failed){if(!mainDone && (!current.Unavailable || current.RequestedSpeed!=speed))Publish(new(null,speed,true,false));return;}
        if(!client.IsLoggedIn){if(!mainDone && (!current.NotLoggedIn || current.RequestedSpeed!=speed))Publish(new(null,speed,false,true));return;}
        // A function the game did not resolve (signature changed by an update) is never called.
        if((nint)HousingManager.MemberFunctionPointers.GetAirshipVoyageTimeAndDistance==0 || (nint)HousingManager.MemberFunctionPointers.GetAirshipSurveyDuration==0)
        {
            failed=true;log.Warning("Airship voyage functions are not available in this game version.");
            Publish(new(null,speed,true,false));return;
        }
        try
        {
            var legs=new Dictionary<(uint,uint),(uint,uint)>();
            var survey=new Dictionary<uint,uint>();
            foreach(var to in points)
            {
                // A survey of 0 is not a computed survey: left out, so the routes through it stay « unknown » (never a 0 h survey).
                var duration=HousingManager.GetAirshipSurveyDuration((byte)to,(short)next);
                if(duration!=0)survey[to]=duration;
                foreach(var from in points.Prepend(AirshipPlanner.Start))
                {
                    if(from==to)continue;
                    uint time=0,distance=0;
                    HousingManager.GetAirshipVoyageTimeAndDistance((byte)from,(byte)to,(short)next,&time,&distance);
                    // A leg with neither time nor distance is not a computed leg: left out (the route is then « unknown »).
                    if(time!=0 || distance!=0)legs[(from,to)]=(time,distance);
                }
            }
            var table=new AirshipLegTable(next,legs,survey);
            PublishTables(AirshipSpeeds.Keep(kept,speed,others,table));
            if(next==speed){computedFor=points;Publish(new(table,speed,false,false));}
        }
        catch(Exception error)
        {
            failed=true;log.Warning(error,"Airship voyage times could not be computed.");
            Publish(new(null,speed,true,false));
        }
    }

    private void Publish(AirshipTableState next){lock(gate){if(!disposed)Volatile.Write(ref state,next);}}
    private void PublishTables(IReadOnlyDictionary<int,AirshipLegTable> next){lock(gate){if(!disposed)Volatile.Write(ref tables,next);}}

    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
