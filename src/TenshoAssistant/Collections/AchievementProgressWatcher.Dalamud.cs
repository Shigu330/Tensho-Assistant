using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Collections;


/// <summary>
/// g, C-15 (0.368): achievement progress. The game keeps the progress of the last achievement asked (Achievement.ProgressAchievementId,
/// ProgressCurrent, ProgressMax) — asked when the player opens it in the game's window, or by Tensho on the player's click only
/// (RequestAchievementProgress sends a request to the server: never automatic). Read passively on the framework thread once a second
/// while logged in and kept per character in memory. Function address checked before the call; failures leave things as they were.
/// </summary>
public sealed unsafe class AchievementProgressWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly object gate=new();
    private Dictionary<uint,AchievementProgress> progress=[];
    private ulong character;
    private uint pendingRequest;
    private DateTimeOffset nextRead,lastRequest;
    private bool disposed,warned;

    // X-102 (0.987, user choice DEC-045, from C-15): what was seen is kept on disk per character (AchievementProgress.json), and a tracked
    // achievement seen reaching its maximum is said once (completed).
    private readonly string? file;
    private readonly Func<uint,bool>? tracked;
    private readonly Action<uint>? completed;
    private readonly BackgroundWriter writer;
    private Dictionary<ulong,Dictionary<uint,AchievementProgress>> all=[];
    public AchievementProgressWatcher(IFramework framework,IClientState client,IPlayerState player,IPluginLog log,string? file=null,Func<uint,bool>? tracked=null,Action<uint>? completed=null)
    {
        this.framework=framework;this.client=client;this.player=player;this.log=log;this.file=file;this.tracked=tracked;this.completed=completed;
        writer=new((error,what)=>log.Warning(error,"{0}",what));
        // 0.997 (review): a file that does not read (another shape, damaged) is kept as is: nothing is written over it.
        try{if(file is not null && File.Exists(file)){if(AchievementProgressStore.Read(File.ReadAllText(file)) is { } read)all=read;else{log.Warning("Saved achievement progress unreadable: kept as is, not written over.");this.file=null;}}}
        catch(Exception error){log.Warning(error,"Saved achievement progress unreadable: kept as is, not written over.");file=null;this.file=null;}
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>What was seen for the current character (copy).</summary>
    public IReadOnlyDictionary<uint,AchievementProgress> Progress{get{lock(gate)return new Dictionary<uint,AchievementProgress>(progress);}}

    /// <summary>On the player's click (UI thread): ask the server once for this achievement's progress; one request per 3 seconds.</summary>
    private DateTimeOffset failedUntil;
    public bool Request(uint achievementId)
    {
        lock(gate)
        {
            // R-14 (0.507): a function lost after a game update refuses the click, instead of accepting it and doing nothing.
            if(disposed || achievementId==0 || DateTimeOffset.UtcNow-lastRequest<TimeSpan.FromSeconds(3) || (nint)Achievement.MemberFunctionPointers.RequestAchievementProgress==0)return false;
            pendingRequest=achievementId;lastRequest=DateTimeOffset.UtcNow;return true;
        }
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        if(!client.IsLoggedIn){lock(gate){if(progress.Count>0){progress=[];character=0;}pendingRequest=0;}return;}
        // R-14 (0.507): after a failure, a pause: the next line threw on every frame.
        if(now<failedUntil)return;
        try
        {
            var achievement=Achievement.Instance();
            if(achievement==null)return;
            uint request;
            lock(gate)
            {
                // Another character: what was seen belongs to the previous one.
                if(player.ContentId!=character){character=player.ContentId;progress=all.TryGetValue(character,out var saved) ? new(saved):[];}
                request=pendingRequest;pendingRequest=0;
            }
            if(request!=0 && (nint)Achievement.MemberFunctionPointers.RequestAchievementProgress!=0)achievement->RequestAchievementProgress(request);
            if(now<nextRead)return;
            nextRead=now.AddSeconds(1);
            var id=achievement->ProgressAchievementId;var max=achievement->ProgressMax;
            if(id!=0 && max>0)
                lock(gate)
                {
                    var current=Math.Min(achievement->ProgressCurrent,max);
                    if(!progress.TryGetValue(id,out var known) || known.Current!=current || known.Max!=max)
                    {
                        progress[id]=new(current,max,now);
                        if(AchievementProgressStore.JustCompleted(known,progress[id]) && tracked?.Invoke(id)==true)completed?.Invoke(id);
                        if(file is not null && character!=0)
                        {
                            all[character]=new(progress);
                            var text=AchievementProgressStore.Write(all);var path=file;
                            writer.Enqueue("Achievement progress could not be saved.",()=>AtomicFile.Write(path,text));
                        }
                    }
                }
            warned=false;
        }
        catch(Exception error)
        {
            failedUntil=now.AddSeconds(30);
            if(!warned){warned=true;log.Warning(error,"Achievement progress could not be read.");}
        }
    }

    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        writer.Flush(TimeSpan.FromSeconds(3));
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
