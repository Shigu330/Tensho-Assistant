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

    public AirshipTableState State=>Volatile.Read(ref state);

    public AirshipVoyageTable(IFramework framework,IClientState client,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.log=log;
        framework.Update+=OnUpdate;
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
        int speed;uint[] points;
        lock(gate)
        {
            if(disposed || requestedSpeed==0)return;
            speed=requestedSpeed;points=requestedPoints;
        }
        // Already computed for this request (the points array is replaced on every new request); otherwise publish only what changed.
        var current=State;
        if(current.Table is { } done && done.Speed==speed && ReferenceEquals(computedFor,points))return;
        if(failed){if(!current.Unavailable || current.RequestedSpeed!=speed)Publish(new(null,speed,true,false));return;}
        if(!client.IsLoggedIn){if(!current.NotLoggedIn || current.RequestedSpeed!=speed)Publish(new(null,speed,false,true));return;}
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
                var duration=HousingManager.GetAirshipSurveyDuration((byte)to,(short)speed);
                if(duration!=0)survey[to]=duration;
                foreach(var from in points.Prepend(AirshipPlanner.Start))
                {
                    if(from==to)continue;
                    uint time=0,distance=0;
                    HousingManager.GetAirshipVoyageTimeAndDistance((byte)from,(byte)to,(short)speed,&time,&distance);
                    // A leg with neither time nor distance is not a computed leg: left out (the route is then « unknown »).
                    if(time!=0 || distance!=0)legs[(from,to)]=(time,distance);
                }
            }
            computedFor=points;
            Publish(new(new AirshipLegTable(speed,legs,survey),speed,false,false));
        }
        catch(Exception error)
        {
            failed=true;log.Warning(error,"Airship voyage times could not be computed.");
            Publish(new(null,speed,true,false));
        }
    }

    private void Publish(AirshipTableState next){lock(gate){if(!disposed)Volatile.Write(ref state,next);}}

    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        framework.Update-=OnUpdate;
    }
}
