using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Collections;

/// <summary>Progress of one achievement as the game received it: current, maximum, when Tensho saw it.</summary>
public sealed record AchievementProgress(uint Current,uint Max,DateTimeOffset SeenAt);

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

    public AchievementProgressWatcher(IFramework framework,IClientState client,IPlayerState player,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.player=player;this.log=log;
        framework.Update+=OnUpdate;
    }

    /// <summary>What was seen for the current character (copy).</summary>
    public IReadOnlyDictionary<uint,AchievementProgress> Progress{get{lock(gate)return new Dictionary<uint,AchievementProgress>(progress);}}

    /// <summary>On the player's click (UI thread): ask the server once for this achievement's progress; one request per 3 seconds.</summary>
    public bool Request(uint achievementId)
    {
        lock(gate)
        {
            if(disposed || achievementId==0 || DateTimeOffset.UtcNow-lastRequest<TimeSpan.FromSeconds(3))return false;
            pendingRequest=achievementId;lastRequest=DateTimeOffset.UtcNow;return true;
        }
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        if(!client.IsLoggedIn){lock(gate){if(progress.Count>0){progress=[];character=0;}pendingRequest=0;}return;}
        try
        {
            var achievement=Achievement.Instance();
            if(achievement==null)return;
            uint request;
            lock(gate)
            {
                // Another character: what was seen belongs to the previous one.
                if(player.ContentId!=character){character=player.ContentId;progress=[];}
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
                    if(!progress.TryGetValue(id,out var known) || known.Current!=current || known.Max!=max)progress[id]=new(current,max,now);
                }
            warned=false;
        }
        catch(Exception error)
        {
            nextRead=now.AddSeconds(30);
            if(!warned){warned=true;log.Warning(error,"Achievement progress could not be read.");}
        }
    }

    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        framework.Update-=OnUpdate;
    }
}
