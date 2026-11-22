using Dalamud.Plugin.Services;
using TenshoAssistant.Reminders;

namespace TenshoAssistant.Character;

/// <summary>
/// g, M-04 (0.374): every 30 seconds on the framework thread while a character is logged in, copies what the Reminders page reads
/// (GameTimers: ventures once the retainer list was opened, next treasure map, Wondrous Tails, hunt bills, daily roulettes) into that
/// character's status and writes its file when something changed (or every 10 minutes). The other characters' files are read once at
/// start. The Reminders page's « done » boxes are kept per character here (X-574, 0.1793: the shared boxes of before 0.374 no longer move).
/// </summary>
public sealed class CharacterStatusWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly Func<IReadOnlyList<RouletteRow>?> roulettes;
    // X-196 (0.1409): the job's abbreviation and the market town's name, in the client's language (null in the tests: texts left empty).
    private readonly Func<byte,string>? jobName,townName;
    private readonly string folder;
    private readonly object gate=new();
    private readonly Dictionary<ulong,CharacterStatus> statuses=[];
    private readonly HashSet<ulong> unreadable=[];
    private ulong current;
    private DateTimeOffset nextRead,nextSave;
    private bool disposed,warned;

    // X-469 (0.1762): the game's sale list of the open retainer, as RetainerSalesReader copies it (null: not wired, as in the tests).
    private readonly Func<TenshoAssistant.Market.RetainerSalesState?>? sales;
    private string? lastListing;
    private DateTimeOffset nextListing;
    private bool listingWarned;

    public CharacterStatusWatcher(IFramework framework,IPlayerState player,IPluginLog log,
        Func<IReadOnlyList<RouletteRow>?> roulettes,string folder,Func<byte,string>? jobName=null,Func<byte,string>? townName=null,
        Func<TenshoAssistant.Market.RetainerSalesState?>? sales=null)
    {
        this.sales=sales;
        this.framework=framework;this.player=player;this.log=log;
        this.roulettes=roulettes;this.folder=folder;this.jobName=jobName;this.townName=townName;
        foreach(var status in CharacterStatuses.ReadAll(folder,(path,error)=>
                {
                    if(ulong.TryParse(Path.GetFileNameWithoutExtension(path),out var id))unreadable.Add(id);
                    log.Warning(error,"A character file could not be read; it is not written over.");
                }))
            statuses[status.ContentId]=status;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>Every known character, the logged-in one first, then by name.</summary>
    public IReadOnlyList<CharacterStatus> All{get{lock(gate)return statuses.Values.OrderByDescending(status=>status.ContentId==current).ThenBy(status=>status.Name).ToArray();}}
    public ulong Current{get{lock(gate)return current;}}

    /// <summary>The logged-in character's « done » time of a Reminders row; false when no character is logged in (shared boxes then).</summary>
    public bool TryDone(string key,out DateTimeOffset? at)
    {
        lock(gate)
        {
            at=null;
            if(current==0 || unreadable.Contains(current) || !statuses.TryGetValue(current,out var status))return false;
            if(status.Done?.TryGetValue(key,out var done)==true)at=done;
            return true;
        }
    }

    /// <summary>Ticks or clears a « done » box of the logged-in character and writes its file; false when none is logged in.</summary>
    public bool SetDone(string key,DateTimeOffset? at)
    {
        lock(gate)
        {
            if(current==0 || unreadable.Contains(current) || !statuses.TryGetValue(current,out var status))return false;
            var done=new Dictionary<string,DateTimeOffset>(status.Done ?? []);
            if(at is { } when)done[key]=when;else done.Remove(key);
            statuses[current]=status with{Done=done};
            QueueWrite(current);
            return true;
        }
    }

    /// <summary>Erases another character's status and file (the logged-in one is recreated at once, so it is refused).</summary>
    public bool Erase(ulong id)
    {
        lock(gate)
        {
            if(id==0 || id==current)return false;
            statuses.Remove(id);unreadable.Remove(id);
            try{File.Delete(Path.Combine(folder,CharacterStatuses.FileName(id)));}
            catch(Exception error){log.Warning(error,"A character file could not be deleted.");return false;}
            return true;
        }
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        ReadListing(now);
        if(now<nextRead)return;
        nextRead=now.AddSeconds(30);
        try
        {
            var id=player.IsLoaded ? player.ContentId:0;
            lock(gate){if(id!=current)current=id;}
            if(id==0 || unreadable.Contains(id))return;
            var timers=GameTimers.Read(roulettes());
            var rouletteDone=timers.Roulettes is { Count:>0 } daily && daily.All(roulette=>roulette.Completed is not null) ? daily.Count(roulette=>roulette.Completed==true):(int?)null;
            var fresh=new CharacterStatus
            {
                ContentId=id,Name=Label(),SeenAt=now,
                Ventures=timers.Ventures?.Select(venture=>new VentureState(venture.Name,venture.Done)).ToArray(),
                NextMap=timers.NextMapAllowance,
                BingoHeld=timers.Bingo?.Held,BingoStickers=timers.Bingo?.Stickers,BingoExpires=timers.Bingo?.Expires,
                HuntBills=timers.HuntBillsHeld,
                RoulettesDone=rouletteDone,RoulettesTotal=rouletteDone is null ? null:timers.Roulettes!.Count,RoulettesAt=rouletteDone is null ? null:now,
                LeveAllowances=timers.LeveAllowances,NextLeveAllowances=timers.NextLeveAllowances,
                DeliveriesLeft=timers.DeliveriesLeft,AlliedQuestsLeft=(int?)timers.AlliedQuestsLeft,
                TimersAt=timers.DeliveriesLeft is null && timers.AlliedQuestsLeft is null ? null:now,
                SquadronMissionDone=timers.SquadronMission?.Done,SquadronTrainingDone=timers.SquadronTraining?.Done,
                Retainers=ReadRetainers(),
            };
            lock(gate)
            {
                statuses.TryGetValue(id,out var old);
                var merged=CharacterStatuses.Merge(old,fresh);
                // X-574 (0.1793, the user's choice of 17/11): the shared « done » boxes of before 0.374 no longer go to the first character seen;
                // the shared ones stay those shown while no character is logged in.
                var changed=CharacterStatuses.Changed(old,merged);
                statuses[id]=merged;
                if(changed || now>=nextSave)
                {
                    QueueWrite(id);
                    nextSave=now.AddMinutes(10);
                }
            }
            warned=false;
        }
        catch(Exception error)
        {
            nextRead=now.AddMinutes(2);
            if(!warned){warned=true;log.Warning(error,"The character status could not be read.");}
        }
    }

    /// <summary>X-76 H10 (0.925): a copy of the character's status written off the framework thread (and off the window's drawing).</summary>
    private void QueueWrite(ulong id)
    {
        var copy=statuses[id];
        (writer??=new((error,message)=>log.Warning(error,message))).Enqueue("A character file could not be saved.",()=>CharacterStatuses.Write(folder,copy));
    }
    private BackgroundWriter? writer;

    private bool WriteLocked(ulong id)
    {
        try{CharacterStatuses.Write(folder,statuses[id]);return true;}
        catch(Exception error){log.Warning(error,"A character file could not be saved.");return false;}
    }

    // X-196 (0.1409): guarded on its own: a failure leaves the retainers as last read, the rest of the status goes on.
    /// <summary>
    /// X-469 (0.1762, D-174): once a second while the game's sale list of a retainer is open and read (RetainerSalesReader, its own checks:
    /// « unavailable » rather than a partial table, R08), its items on sale are kept with that retainer (found by the open retainer's id,
    /// named from the game's list), dated; again only when the listing changes or the list reopens. A retainer not read at a summoning bell
    /// yet has no row to keep it in: nothing is kept then.
    /// </summary>
    private void ReadListing(DateTimeOffset now)
    {
        if(sales is null || now<nextListing)return;
        nextListing=now.AddSeconds(1);
        try
        {
            if(sales() is not { Status:TenshoAssistant.Market.RetainerSalesStatus.Ready,RetainerId:not 0 } open){lastListing=null;return;}
            var id=player.IsLoaded ? player.ContentId:0;
            if(id==0 || unreadable.Contains(id))return;
            var listed=RetainerStates.Listing(open.Listings.Select(row=>(row.ItemId,row.Hq,row.Quantity,row.UnitPrice)));
            var key=RetainerStates.ListingKey(open.RetainerId,listed);
            if(key==lastListing || RetainerReader.NameOf(open.RetainerId) is not { Length:>0 } name)return;
            lock(gate)
            {
                if(!statuses.TryGetValue(id,out var status) || status.Retainers is not { } retainers
                   || RetainerStates.WithListing(retainers,name,listed,now) is not { } next)return;
                statuses[id]=status with{Retainers=next};
                QueueWrite(id);
            }
            lastListing=key;
            listingWarned=false;
        }
        catch(Exception error)
        {
            nextListing=now.AddSeconds(60);
            if(!listingWarned){listingWarned=true;log.Warning(error,"A retainer's items on sale could not be kept (maybe after a game update).");}
        }
    }

    private RetainerState[]? ReadRetainers()
    {
        try{return RetainerReader.Read(jobName,townName);}
        catch(Exception error){log.Debug(error,"The retainers could not be read.");return null;}
    }

    // « Name (World) » of the logged-in character, as the Bank names its storages (0.345).
    private string Label()
    {
        var name=player.CharacterName;
        if(string.IsNullOrWhiteSpace(name))return "";
        var world=player.HomeWorld.IsValid ? player.HomeWorld.Value.Name.ExtractText():"";
        return world.Length>0 ? $"{name} ({world})":name;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        writer?.Flush(TimeSpan.FromSeconds(3));
        lock(gate){if(current!=0 && !unreadable.Contains(current) && statuses.ContainsKey(current))WriteLocked(current);}
    }
}
