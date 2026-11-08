using Dalamud.Plugin.Services;
using TenshoAssistant.Reminders;

namespace TenshoAssistant.Character;

/// <summary>
/// g, M-04 (0.374): every 30 seconds on the framework thread while a character is logged in, copies what the Reminders page reads
/// (GameTimers: ventures once the retainer list was opened, next treasure map, Wondrous Tails, hunt bills, daily roulettes) into that
/// character's status and writes its file when something changed (or every 10 minutes). The other characters' files are read once at
/// start. The Reminders page's « done » boxes are kept per character here; the former shared boxes go to the first character seen.
/// </summary>
public sealed class CharacterStatusWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private readonly Func<IReadOnlyList<RouletteRow>?> roulettes;
    private readonly string folder;
    private readonly object gate=new();
    private readonly Dictionary<ulong,CharacterStatus> statuses=[];
    private readonly HashSet<ulong> unreadable=[];
    private ulong current;
    private DateTimeOffset nextRead,nextSave;
    private bool disposed,warned;

    public CharacterStatusWatcher(IFramework framework,IPlayerState player,IPluginLog log,Configuration configuration,Action saveConfiguration,
        Func<IReadOnlyList<RouletteRow>?> roulettes,string folder)
    {
        this.framework=framework;this.player=player;this.log=log;this.configuration=configuration;this.saveConfiguration=saveConfiguration;
        this.roulettes=roulettes;this.folder=folder;
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
            };
            lock(gate)
            {
                statuses.TryGetValue(id,out var old);
                var merged=CharacterStatuses.Merge(old,fresh);
                // The former shared « done » boxes (before 0.374) go to the first character seen, once.
                var migrated=false;
                if(merged.Done is null && configuration.ReminderDone is { Count:>0 } shared){merged=merged with{Done=new(shared)};migrated=true;}
                var changed=CharacterStatuses.Changed(old,merged);
                statuses[id]=merged;
                if(changed || now>=nextSave)
                {
                    if(migrated){if(WriteLocked(id)){configuration.ReminderDone=null;saveConfiguration();}}
                    else QueueWrite(id);
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
