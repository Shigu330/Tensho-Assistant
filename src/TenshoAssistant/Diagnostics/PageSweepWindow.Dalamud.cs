using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using TenshoAssistant.Community;
using TenshoAssistant.Competitive;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.724: developer mode only, once per load, after the self-check. Draws every page in a window far outside the screen that takes no input, so that nothing can
/// be seen or clicked, and writes what each one cost and any error to the log: docs/evidence/0.724-page-sweep.md. It waits while the player is busy.
/// </summary>
internal sealed class PageSweepWindow : Window
{
    private readonly Func<bool> ready;
    private readonly Func<bool> busy;
    private readonly Func<IReadOnlyList<(string Name,Action Draw)>> entries;
    private readonly int skipped;
    private readonly IPluginLog log;
    private IReadOnlyList<(string Name,Action Draw)> pages=[];
    private PageSweep? sweep;
    private IReadOnlyDictionary<string,int> sentBefore=new Dictionary<string,int>();

    public PageSweepWindow(bool developerMode,Func<bool> ready,Func<bool> busy,Func<IReadOnlyList<(string Name,Action Draw)>> entries,int skipped,IPluginLog log)
        :base("###TenshoPageSweep",ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoInputs|ImGuiWindowFlags.NoFocusOnAppearing
            |ImGuiWindowFlags.NoBringToFrontOnFocus|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoDocking|ImGuiWindowFlags.NoCollapse,true)
    {
        this.ready=ready;this.busy=busy;this.entries=entries;this.skipped=skipped;this.log=log;
        Position=new Vector2(-30000,-30000);PositionCondition=ImGuiCond.Always;
        Size=new Vector2(1000,760);SizeCondition=ImGuiCond.Always;
        RespectCloseHotkey=false;DisableWindowSounds=true;AllowPinning=false;AllowClickthrough=false;ShowCloseButton=false;
        IsOpen=developerMode;
    }

    public override bool DrawConditions()
    {
        if(CompetitiveGate.Cut || busy())return false;
        if(sweep is null)
        {
            if(!ready())return false;
            pages=entries();
            sentBefore=BoundedHttp.Sent;
            sweep=new PageSweep(pages.Select(page=>page.Name).ToArray());
        }
        return sweep.Due();
    }

    public override void Draw()
    {
        if(sweep?.Current is not { } current)return;
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        string? failure=null;
        try{pages.First(page=>page.Name==current).Draw();}
        catch(Exception error)
        {
            failure=error.GetType().Name+": "+error.Message;
            log.Warning(error,"Page sweep: {0} failed to draw.",current);
        }
        sweep.Drawn(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,failure);
        if(!sweep.Done)return;
        IsOpen=false;
        var lines=sweep.Lines;
        foreach(var line in lines)log.Information("{0}",line.Format());
        var sent=BoundedHttp.Sent.Select(host=>(host.Key,Count:host.Value-sentBefore.GetValueOrDefault(host.Key))).Where(host=>host.Count>0).ToArray();
        log.Information("{0}",PageSweep.Summary(lines,skipped,sent.Sum(host=>host.Count),string.Join(", ",sent.Select(host=>$"{host.Key} {host.Count}"))));
    }
}
