using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Gear;

/// <summary>
/// X-221 (0.1760, the user's request of 12/11/2026, K-37): the bonus points left of a manual's Grace (statuses 45 and 46, ManualGrace).
/// Once a second on the framework thread: your own statuses (their Param and seconds left) and the levels and experience of the jobs they
/// boost (PlayerState by ClassJob.ExpArrayIndex); each experience line of your job worn (LogMessage 589, its parameters: lnum1 the job,
/// lnum2 the points, lnum3 the bonus percent) adds the manual's part. A line whose job is not the one worn is not counted (its parameters
/// would not be the ones expected: said once in the log, R08). Kept per character in the settings (<c>store</c>, saved by <c>save</c>
/// when something changes). Your own data only; nothing is sent or done in the game. Cut in competitive content like the rest.
/// </summary>
public sealed unsafe class GraceWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IObjectTable objects;
    private readonly IChatGui chat;
    private readonly IPluginLog log;
    private readonly IReadOnlyDictionary<uint,int> expIndex;
    private readonly Func<Dictionary<ulong,List<GraceCount>>> store;
    private readonly Action save;
    private readonly Func<uint,string> statusName;
    private readonly object gate=new();
    private readonly List<(uint Job,long Gained,int Percent)> gains=[];
    private readonly Dictionary<uint,(uint Job,int Level)> levels=[];
    private readonly HashSet<uint> seenBefore=[];
    private ulong character;
    private bool firstLook=true,disposed,failed,mismatchSaid,gainSaid;
    private long nextLook=-100_000;
    private IReadOnlyList<GraceShown> shown=[];

    /// <summary>The Graces of the character logged in, for the Gear page (empty: none held, or not read).</summary>
    public IReadOnlyList<GraceShown> Shown=>Volatile.Read(ref shown);

    public GraceWatcher(IFramework framework,IClientState client,IPlayerState player,IObjectTable objects,IChatGui chat,IPluginLog log,IReadOnlyDictionary<uint,int> expIndex,
        Func<Dictionary<ulong,List<GraceCount>>> store,Action save,Func<uint,string> statusName)
    {
        this.framework=framework;this.client=client;this.player=player;this.objects=objects;this.chat=chat;this.log=log;this.expIndex=expIndex;
        this.store=store;this.save=save;this.statusName=statusName;
        chat.LogMessage+=OnLogMessage;
        try{TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);}
        catch{chat.LogMessage-=OnLogMessage;throw;}
    }

    private void OnLogMessage(Dalamud.Game.Chat.ILogMessage message)
    {
        if(disposed || failed || message.LogMessageId!=ManualGrace.ExperienceMessage || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        try
        {
            // Every line of this message is kept (any job): one whose job is not the one worn tells parameters that moved (said once).
            if(!message.TryGetIntParameter(0,out var job) || !message.TryGetIntParameter(1,out var gained) || !message.TryGetIntParameter(2,out var percent))return;
            lock(gate)if(gains.Count<64)gains.Add(((uint)Math.Max(job,0),gained,percent));
        }
        catch(Exception error) when(error is not OutOfMemoryException){}
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed || failed)return;
        bool pending;
        lock(gate)pending=gains.Count>0;
        var now=Environment.TickCount64;
        if(!pending && now-nextLook<1000)return;
        nextLook=now;
        try
        {
            // X-770 (0.1769): in a competitive content (Tensho cut, R03.2) nothing is read either, so that a status the game hides there is
            // not taken for one it took away; it is checked again when the content ends (seenBefore emptied).
            if(!client.IsLoggedIn || !player.IsLoaded || objects.LocalPlayer is not { } me || player.ContentId==0 || TenshoAssistant.Competitive.CompetitiveGate.Cut)
            {
                // X-769: a line dropped here (a loading screen) makes the next sighting check the jobs' experience (seenBefore emptied).
                lock(gate)gains.Clear();
                seenBefore.Clear();
                if(!client.IsLoggedIn){character=0;firstLook=true;Volatile.Write(ref shown,[]);}
                return;
            }
            if(player.ContentId!=character){character=player.ContentId;firstLook=true;levels.Clear();seenBefore.Clear();}
            var all=store();
            var counts=all.GetValueOrDefault(character) ?? [];
            var held=new Dictionary<uint,(ushort Param,float Remaining)>();
            foreach(var status in me.StatusList)
                if(status.StatusId is ManualGrace.CrafterGrace or ManualGrace.GathererGrace && !held.ContainsKey(status.StatusId))
                    held[status.StatusId]=(status.Param,status.RemainingTime);
            // X-769 (0.1765): a Grace not seen at this reading keeps its saved count (not shown); one seen again checks the jobs' experience.
            var next=ManualGrace.Read(counts,held,grace=>Snapshot(PlayerState.Instance(),grace),firstLook,seenBefore,out var current,out var changed);
            foreach(var count in current)
                if(!counts.Any(saved=>ReferenceEquals(saved,count)))
                    log.Information("Grace: {0} (Param {1}, {2:F0} s left) {3}.",count.Status,count.Param,count.Remaining,
                        count.StartSeen ? "seen from its start: its bonus points are counted":"held without its start seen (or experience gained while not counted): its points left are unknown");
            // X-770 (0.1769): a saved count dropped because its status was not seen for half a minute (the game took it away).
            foreach(var count in counts)
                if(!held.ContainsKey(count.Status) && !next.Any(kept=>ReferenceEquals(kept,count)))
                    log.Information("Grace: {0} not held at {1} readings in a row (the game took it away: cap reached, removed or ended): its count is dropped.",count.Status,ManualGrace.GoneReadings);
            firstLook=false;
            // The level of the job worn as last read before the line (a line that raises the level is counted at the level it was earned at).
            var worn=me.ClassJob.RowId;
            var level=(int)me.Level;
            (uint Job,long Gained,int Percent)[] lines;
            lock(gate){lines=[..gains];gains.Clear();}
            foreach(var (job,gained,percent) in lines)
            {
                if(job!=worn || gained<0 || percent is <0 or >10_000)
                {
                    if(!mismatchSaid){mismatchSaid=true;log.Warning("Grace: an experience line read as job {0}, {1} points, +{2}% while job {3} is worn (maybe after a game update): such lines are not counted.",job,gained,percent,worn);}
                    continue;
                }
                if(ManualGrace.StatusFor(job)==0)continue;
                var known=levels.TryGetValue(ManualGrace.StatusFor(job),out var last) && last.Job==job ? last.Level:level;
                if(current.FirstOrDefault(count=>count.Status==ManualGrace.StatusFor(job)) is not { } count)continue;
                if(ManualGrace.Gain(count,gained,percent,known))changed=true;
                if(!gainSaid){gainSaid=true;log.Information("Grace: an experience line read: {0} points (+{1}%) at level {2}, the manual's part counted; {3} bonus points left.",gained,percent,known,ManualGrace.Left(count)?.ToString() ?? "unknown");}
            }
            if(ManualGrace.StatusFor(worn) is var boosted and not 0)levels[boosted]=(worn,level);
            if(next.Count>0)all[character]=next;else if(all.Remove(character))changed=true;
            Volatile.Write(ref shown,current.Select(count=>new GraceShown(count.Status,statusName(count.Status),ManualGrace.Left(count),ManualGrace.Cap(count.Param),count.Param)).ToArray());
            if(changed)save();
        }
        catch(Exception error)
        {
            failed=true;
            Volatile.Write(ref shown,[]);
            log.Warning(error,"Grace: the manual's bonus points left could not be read (maybe after a game update): not shown until the plugin reloads.");
        }
    }

    // The levels and experience of the jobs a Grace boosts (PlayerState, by ClassJob.ExpArrayIndex).
    private string Snapshot(PlayerState* state,uint grace)
    {
        if(state==null || !state->IsLoaded)return "";
        var jobs=new List<(uint,int,int)>();
        foreach(var job in ManualGrace.JobsOf(grace))
            if(expIndex.TryGetValue(job,out var slot) && slot>=0 && slot<state->ClassJobLevels.Length && slot<state->ClassJobExperience.Length)
                jobs.Add((job,state->ClassJobLevels[slot],state->ClassJobExperience[slot]));
        return ManualGrace.Snapshot(jobs);
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        chat.LogMessage-=OnLogMessage;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
