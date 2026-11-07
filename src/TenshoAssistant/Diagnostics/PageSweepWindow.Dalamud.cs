using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using TenshoAssistant.Community;
using TenshoAssistant.Competitive;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.724: developer mode only, once per load, after the self-check. Draws every page and writes what each one cost and any error to the log: docs/evidence/0.724-page-sweep.md. It waits while
/// the player is busy. 0.726: outside the screen ImGui skipped the content of child windows, so the window is now on the screen and marked hidden, as ImGui does for a new window's first frame:
/// its items run, it is not rendered and cannot be hovered. Should it not be hidden, the sweep stops at once.
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
    private const string Name="###TenshoPageSweep";
    private const int MaximumSilentFrames=600;
    private bool created;
    private int silentFrames;

    public PageSweepWindow(bool developerMode,Func<bool> ready,Func<bool> busy,Func<IReadOnlyList<(string Name,Action Draw)>> entries,int skipped,IPluginLog log)
        :base(Name,ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoInputs|ImGuiWindowFlags.NoFocusOnAppearing
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
        if(++silentFrames>MaximumSilentFrames)
        {
            IsOpen=false;
            log.Warning("Page sweep stopped: the window never drew.");
            return false;
        }
        return sweep.Due();
    }

    public override void PreDraw()
    {
        // Two: ImGui counts one down at Begin, and child windows hide while their parent's count is above zero.
        var window=ImGuiP.FindWindowByName(Name);
        if(!window.IsNull)window.HiddenFramesCannotSkipItems=2;
    }

    public override void Draw()
    {
        silentFrames=0;
        if(!created)
        {
            // Its first frame only makes it exist, out of sight.
            created=true;
            Position=ImGui.GetMainViewport().Pos;
            return;
        }
        if(!ImGuiP.GetCurrentWindow().Hidden)
        {
            IsOpen=false;
            log.Warning("Page sweep stopped: the window was not hidden.");
            return;
        }
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
