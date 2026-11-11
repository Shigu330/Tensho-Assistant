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
    private readonly Dictionary<uint,(int Count,int Points)> zoneSeen=[];private int zoneSeenFor=-1;private bool scripsLogged;private int resultWait;
    // 0.1094 (user report: seven reloads during one voyage): the voyage kept beside the settings, read again after a reload of the same voyage.
    private readonly string? recordPath;private readonly BackgroundWriter? writer;
    private OceanVoyageRecord? record;private bool scheduleCheckedAsked;
    /// <summary>0.1094: the voyage as kept (catches of every stop, missions), or null outside a voyage.</summary>
    public OceanVoyageRecord? Record{get{lock(gate)return frame is null ? null:record;}}

    public OceanVoyageReader(IFramework framework,IClientState client,IPluginLog log,string? recordPath=null,BackgroundWriter? writer=null)
    {
        this.framework=framework;this.client=client;this.log=log;this.recordPath=recordPath;this.writer=writer;
        if(recordPath is not null)
            try{record=File.Exists(recordPath) ? OceanVoyageRecord.FromJson(File.ReadAllText(recordPath)):null;}
            catch(Exception error){log.Warning(error,"Ocean voyage record unreadable; the voyage is read from the game alone.");}
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);
    }

    /// <summary>0.1094: the schedule check of this voyage is done (kept across a reload, so it is not logged again).</summary>
    public void MarkScheduleChecked(){lock(gate)scheduleCheckedAsked=true;}

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
                // 0.1094: the three stops are copied, for the voyage record (and the totals of the voyage).
                var gameCatches=new List<OceanZoneCatch>();
                for(var stop=0;stop<=zone;stop++)
                {
                    var stopData=stop switch{0=>voyage->FirstZoneFishData,1=>voyage->SecondZoneFishData,_=>voyage->ThirdZoneFishData};
                    foreach(ref readonly var fish in stopData)
                        if(fish.ItemId!=0)gameCatches.Add(new(stop,fish.ItemId%1_000_000,fish.NqAmount+fish.HqAmount,(int)Math.Min(fish.TotalPoints,int.MaxValue)));
                }
                var zoneData=zone switch{0=>voyage->FirstZoneFishData,1=>voyage->SecondZoneFishData,_=>voyage->ThirdZoneFishData};
                var zoneFish=new List<(uint,int,int,int)>();
                foreach(ref readonly var fish in zoneData)
                    if(fish.ItemId!=0)zoneFish.Add((fish.ItemId%1_000_000,fish.NqAmount,fish.HqAmount,(int)Math.Min(fish.TotalPoints,int.MaxValue)));
                var nowAt=DateTimeOffset.UtcNow;
                if(record?.SameVoyage(voyage->CurrentRoute,zone,nowAt)!=true){scripsLogged=false;scheduleCheckedAsked=false;}
                var kept=OceanVoyageRecord.Merge(record,voyage->CurrentRoute,zone,gameCatches,[voyage->Mission1Progress,voyage->Mission2Progress,voyage->Mission3Progress],nowAt);
                if(scheduleCheckedAsked || scripsLogged)kept=kept with{ScheduleChecked=kept.ScheduleChecked || scheduleCheckedAsked,ResultLogged=kept.ResultLogged || scripsLogged};
                if(kept.ResultLogged)scripsLogged=true;
                // A stop the game no longer lists after a reload keeps what was saved for it.
                foreach(var saved in kept.At(zone))
                    if(!zoneFish.Any(fish=>fish.Item1==saved.Item))zoneFish.Add((saved.Item,saved.Count,0,saved.Points));
                SaveRecord(kept,nowAt);
                if(zone!=zoneSeenFor){zoneSeen.Clear();zoneSeenFor=zone;foreach(var (item,nq,hq,points) in zoneFish)zoneSeen[item]=(nq+hq,points);}
                else
                    foreach(var (item,nq,hq,points) in zoneFish)
                    {
                        var was=zoneSeen.GetValueOrDefault(item);
                        if(nq+hq==was.Count+1 && points>was.Points)CatchPoints?.Invoke(item,points-was.Points);
                        zoneSeen[item]=(nq+hq,points);
                    }
                // X-85 V6 (0.964): the voyage's result, logged once in developer mode to prove which scrip field is which (orange or purple).
                // 0.1095 (user report: « points 0, experience 0 » at 15:30:08, the first reading of status 4): the result is filled a moment
                // later; logged once it holds something, or after 30 s of status 4 with what is there, beside Tensho's own count.
                if(status!=4)resultWait=0;
                if(status==4 && !scripsLogged && Developer?.Invoke()==true)
                {
                    var result=voyage->IndividualResult;
                    var filled=result.TotalPoints>0 || result.ExperiencePoints>0 || result.Scrip1Amount>0 || result.Scrip2Amount>0;
                    if(filled || ++resultWait>=60)
                    {
                        scripsLogged=true;
                        var counted=OceanVoyages.Totals(new OceanVoyageFrame(voyage->CurrentRoute,zone,status,false,0,[],player.TotalPoints,player.CaughtFish,nowAt){Catches=record!.Catches});
                        log.Information("Ocean voyage result: route {0}, points {1}, experience {2}, scrip1 {3}, scrip2 {4}; Tensho counted {5} points, {6} fish{7}.",voyage->CurrentRoute,
                            result.TotalPoints,result.ExperiencePoints,result.Scrip1Amount,result.Scrip2Amount,counted.Points,counted.Caught,filled ? "":" (result still empty after 30 s)");
                    }
                }
                var progress=record!.Missions;
                Volatile.Write(ref frame,new OceanVoyageFrame(voyage->CurrentRoute,zone,status,voyage->SpectralCurrentActive,voyage->WeatherId,
                    [(voyage->Mission1Type,progress.Count>0 ? progress[0]:voyage->Mission1Progress),(voyage->Mission2Type,progress.Count>1 ? progress[1]:voyage->Mission2Progress),
                     (voyage->Mission3Type,progress.Count>2 ? progress[2]:voyage->Mission3Progress)],
                    player.TotalPoints,player.CaughtFish,DateTimeOffset.UtcNow){ZoneFish=zoneFish,Catches=record.Catches,ScheduleChecked=record.ScheduleChecked});
            }
            catch(Exception error)
            {
                Volatile.Write(ref frame,null);nextRead=now+Stopwatch.Frequency*5;
                if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Ocean fishing voyage unavailable; retry in five seconds.");
            }
        }
    }

    /// <summary>0.1094: keeps the record, and saves it in the background when its content changed or every two minutes (so it stays recent).</summary>
    private void SaveRecord(OceanVoyageRecord kept,DateTimeOffset now)
    {
        var changed=!kept.SameContent(record) || record is null || now-record.Updated>=TimeSpan.FromMinutes(2);
        if(changed)record=kept;
        if(!changed || recordPath is null || writer is null)return;
        var json=kept.ToJson();var path=recordPath;
        writer.Enqueue("Ocean voyage record could not be saved.",()=>AtomicFile.Write(path,json));
    }

    public void Dispose(){lock(gate){if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);Volatile.Write(ref frame,null);}}
}
