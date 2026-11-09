using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace TenshoAssistant.Gathering;

/// <summary>
/// R-07 (0.418): the ocean fishing voyage in progress, read twice a second on the framework thread through the game's event framework
/// (GetInstanceContentOceanFishing; the director functions behind it are used only once their addresses are resolved). Fields only;
/// nothing is sent. Dropped outside the voyage; a route or stop out of range is not published.
/// </summary>
internal sealed class OceanVoyageReader : IDisposable
{
    private readonly object gate=new();
    private readonly IFramework framework;private readonly IClientState client;private readonly IPluginLog log;
    private bool disposed;private long nextRead;private int warned;
    private OceanVoyageFrame? frame;
    private (int,int) lastLogged=(-1,-1);
    /// <summary>X-98 (0.958): developer mode, a reading of the voyage fields logged at each zone or status change.</summary>
    public Func<bool>? Developer { get; set; }
    public OceanVoyageFrame? Frame=>Volatile.Read(ref frame);
    /// <summary>
    /// X-85 V2 (0.964): each new catch at a stop with the points it brought (the zone's FishData points grew by that much for one more fish): item, points,
    /// territory. Called on the framework thread.
    /// </summary>
    public Action<uint,int>? CatchPoints { get; set; }
    private readonly Dictionary<uint,(int Count,int Points)> zoneSeen=[];private int zoneSeenFor=-1;private bool scripsLogged;

    public OceanVoyageReader(IFramework framework,IClientState client,IPluginLog log)
    {this.framework=framework;this.client=client;this.log=log;TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);}

    private unsafe void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var now=Stopwatch.GetTimestamp();if(now<nextRead)return;nextRead=now+Stopwatch.Frequency/2;
            try
            {
                if(!client.IsLoggedIn || (nint)EventFramework.MemberFunctionPointers.GetContentDirector==0
                   || (nint)EventFramework.MemberFunctionPointers.GetInstanceContentDirector==0){Volatile.Write(ref frame,null);return;}
                var events=EventFramework.Instance();
                var voyage=events==null ? null:events->GetInstanceContentOceanFishing();
                if(voyage==null){Volatile.Write(ref frame,null);return;}
                var zone=(int)voyage->CurrentZone;var status=(int)voyage->Status;
                if(zone is < 0 or > 2 || status is < 0 or > 4 || voyage->CurrentRoute is 0 or > 1000){Volatile.Write(ref frame,null);return;}
                var player=voyage->LocalPlayerAllResult;
                // X-98 (0.958): the results table holds the local player's line at LocalIndexInAllResult once it is filled; preferred when it is.
                var local=voyage->LocalIndexInAllResult;var size=voyage->AllResultSize;
                if(size>0 && local<size && local<voyage->AllResults.Length && voyage->AllResults[local].TotalPoints is var tablePoints && tablePoints>0)
                    player=voyage->AllResults[local];
                if(Developer?.Invoke()==true && (zone,status)!=lastLogged)
                {
                    lastLogged=(zone,status);
                    log.Information("Ocean voyage reading: route {0}, zone {1}, status {2}, local line {3} pts / {4} fish, table {5} of {6}, missions {7}/{8}/{9}.",
                        voyage->CurrentRoute,zone,status,voyage->LocalPlayerAllResult.TotalPoints,voyage->LocalPlayerAllResult.CaughtFish,local,size,
                        voyage->Mission1Progress,voyage->Mission2Progress,voyage->Mission3Progress);
                }
                // X-85 V2 (0.964): the current stop's fish counts and points (FirstZoneFishData, SecondZoneFishData, ThirdZoneFishData), copied.
                var zoneData=zone switch{0=>voyage->FirstZoneFishData,1=>voyage->SecondZoneFishData,_=>voyage->ThirdZoneFishData};
                var zoneFish=new List<(uint,int,int,int)>();
                foreach(ref readonly var fish in zoneData)
                    if(fish.ItemId!=0)zoneFish.Add((fish.ItemId%1_000_000,fish.NqAmount,fish.HqAmount,(int)Math.Min(fish.TotalPoints,int.MaxValue)));
                if(zone!=zoneSeenFor){zoneSeen.Clear();zoneSeenFor=zone;foreach(var (item,nq,hq,points) in zoneFish)zoneSeen[item]=(nq+hq,points);}
                else
                    foreach(var (item,nq,hq,points) in zoneFish)
                    {
                        var was=zoneSeen.GetValueOrDefault(item);
                        if(nq+hq==was.Count+1 && points>was.Points)CatchPoints?.Invoke(item,points-was.Points);
                        zoneSeen[item]=(nq+hq,points);
                    }
                // X-85 V6 (0.964): the voyage's result, logged once in developer mode to prove which scrip field is which (orange or purple).
                if(status==4 && !scripsLogged && Developer?.Invoke()==true)
                {
                    scripsLogged=true;var result=voyage->IndividualResult;
                    log.Information("Ocean voyage result: route {0}, points {1}, experience {2}, scrip1 {3}, scrip2 {4}.",voyage->CurrentRoute,result.TotalPoints,result.ExperiencePoints,result.Scrip1Amount,result.Scrip2Amount);
                }
                if(status!=4)scripsLogged=false;
                Volatile.Write(ref frame,new OceanVoyageFrame(voyage->CurrentRoute,zone,status,voyage->SpectralCurrentActive,voyage->WeatherId,
                    [(voyage->Mission1Type,voyage->Mission1Progress),(voyage->Mission2Type,voyage->Mission2Progress),(voyage->Mission3Type,voyage->Mission3Progress)],
                    player.TotalPoints,player.CaughtFish,DateTimeOffset.UtcNow){ZoneFish=zoneFish});
            }
            catch(Exception error)
            {
                Volatile.Write(ref frame,null);nextRead=now+Stopwatch.Frequency*5;
                if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Ocean fishing voyage unavailable; retry in five seconds.");
            }
        }
    }

    public void Dispose(){lock(gate){if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);Volatile.Write(ref frame,null);}}
}
