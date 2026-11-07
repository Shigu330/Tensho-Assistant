using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Collections;

/// <summary>
/// g, X-03 (0.369): changes the worn title through the game's own TitleController.SendTitleIdUpdate (address checked first), on the
/// framework thread — on the player's click, or when the job changes and the player turned « title by job » on (the title set for that
/// job, once per change). Two seconds at least between two changes. Failures are logged once; nothing is retried.
/// </summary>
public sealed unsafe class TitleChanger : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly Func<bool> byJob;
    private readonly Func<IReadOnlyDictionary<uint,ushort>?> jobTitles;
    private readonly object gate=new();
    private ushort? pending;
    private uint lastJob;
    private DateTimeOffset lastSent;
    private bool disposed,warned;

    public TitleChanger(IFramework framework,IClientState client,IPlayerState player,IPluginLog log,Func<bool> byJob,Func<IReadOnlyDictionary<uint,ushort>?> jobTitles)
    {
        this.framework=framework;this.client=client;this.player=player;this.log=log;this.byJob=byJob;this.jobTitles=jobTitles;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>The job the character is on now (0 = unknown), read on the framework thread.</summary>
    public uint CurrentJob=>Volatile.Read(ref lastJob);

    /// <summary>On the player's click (UI thread): wear this title (0 = none) at the next framework update.</summary>
    public void Wear(ushort titleId){lock(gate)pending=titleId;}

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        if(!client.IsLoggedIn || !player.IsLoaded){Volatile.Write(ref lastJob,0);lock(gate)pending=null;return;}
        try
        {
            var job=player.ClassJob.RowId;
            if(job!=0 && job!=lastJob)
            {
                var first=lastJob==0;
                Volatile.Write(ref lastJob,job);
                // Not at login (the game already wears the saved title), only on a real job change.
                if(!first && byJob() && jobTitles()?.TryGetValue(job,out var title)==true)lock(gate)pending=title;
            }
            ushort send;
            lock(gate)
            {
                if(pending is not { } wanted || DateTimeOffset.UtcNow-lastSent<TimeSpan.FromSeconds(2))return;
                send=wanted;pending=null;lastSent=DateTimeOffset.UtcNow;
            }
            var ui=UIState.Instance();
            if(ui==null || (nint)TitleController.MemberFunctionPointers.SendTitleIdUpdate==0)
            {if(!warned){warned=true;log.Warning("The worn title cannot be changed: function not resolved.");}return;}
            // R-14 (0.454): JobTitles is shared by every character: only a title this character's list marks as obtained is sent (a set bit is reliable
            // before the list arrived, an unset one is not, so nothing is sent then); « none » (0) is always allowed.
            if(send!=0 && (!ui->TitleList.UnlockedTitlesBitArray.TryGet(send,out var owned) || !owned))
            {if(!warned){warned=true;log.Warning($"Title {send} not confirmed as obtained by this character; not worn.");}return;}
            ui->TitleController.SendTitleIdUpdate(send);
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"The worn title could not be changed.");}
        }
    }

    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
