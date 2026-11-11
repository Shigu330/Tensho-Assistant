using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.1094: writes <see cref="PlayState"/> (PlayState.json in the settings folder) every two seconds at most, on the framework thread for the
/// reading, in the background for the file. Through the competitive gate like everything else (DEC-038): when competitive content cuts
/// Tensho, one last write says so, and the scripts keep it longer (playstate.py).
/// </summary>
internal sealed class PlayStateWriter : IDisposable
{
    private readonly IFramework framework;private readonly ICondition condition;private readonly Func<bool> enabled;private readonly Func<bool> voyage;
    private readonly string path;private readonly BackgroundWriter writer;private readonly object gate=new();
    private long nextCheck=-100_000;private bool written;private string? writtenReason;private DateTimeOffset writtenAt;private bool disposed;

    public PlayStateWriter(IFramework framework,ICondition condition,string folder,Func<bool> enabled,Func<bool> voyage,BackgroundWriter writer)
    {
        this.framework=framework;this.condition=condition;this.enabled=enabled;this.voyage=voyage;this.writer=writer;
        path=Path.Combine(folder,PlayState.FileName);
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);
        TenshoAssistant.Competitive.CompetitiveGate.Changed+=OnCut;
    }

    private void Update(IFramework unused)
    {
        var tick=Environment.TickCount64;if(tick-nextCheck<2_000)return;nextCheck=tick;
        if(!enabled())return;
        Write(PlayState.BusyReason(voyage(),false,
            condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95]));
    }

    private void OnCut(bool cut){if(cut && enabled())Write(PlayState.BusyReason(false,true,false));}

    private void Write(string? reason)
    {
        lock(gate)
        {
            if(disposed)return;
            var now=DateTimeOffset.UtcNow;
            if(!PlayState.ShouldWrite(reason,written,writtenReason,now,writtenAt))return;
            written=true;writtenReason=reason;writtenAt=now;
            var json=PlayState.ToJson(reason,now);
            writer.Enqueue("Play state could not be saved.",()=>AtomicFile.Write(path,json));
        }
    }

    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        TenshoAssistant.Competitive.CompetitiveGate.Changed-=OnCut;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);
    }
}
