using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TenshoAssistant.Competitive;
using TenshoAssistant.Diagnostics;

namespace TenshoAssistant.Alarms;

/// <summary>
/// X-165 (0.1245, slice 2, user request of 2026-11-12): Tensho's state in the server info bar, for every player, off by default
/// (Settings › General « Tensho's state in the server bar »): « Tensho: ok · 210 MB », or only when something is wrong (setting). Its
/// tooltip gives the dashboard's lines, a click opens the settings where the dashboard is shown. It reads the snapshot the main window
/// publishes once a second from its draw (MainWindow.TickDashboard: the measures belong to the draw thread); refreshed every 2 seconds on
/// the framework thread; gone during a competitive cut (DEC-038); removed on dispose. Title fixed (Dalamud keys the entry's settings by it).
/// </summary>
public sealed class StateDtrEntry : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly Configuration configuration;
    private readonly Func<MainWindow.DashboardSnapshot?> state;
    private readonly Func<string,string> text;
    private readonly Func<string,object[],string> format;
    private readonly IDtrBarEntry entry;
    private DateTimeOffset nextRefresh;
    private MainWindow.DashboardSnapshot? shown;
    private (Vector2 Min,Vector2 Max)? bounds;
    private bool disposed;

    internal StateDtrEntry(IDtrBar bar,IFramework framework,IGameGui gui,Configuration configuration,Func<MainWindow.DashboardSnapshot?> state,
        Func<string,string> text,Func<string,object[],string> format,Action open)
    {
        this.framework=framework;this.gui=gui;this.configuration=configuration;this.state=state;this.text=text;this.format=format;
        entry=bar.Get("Tensho Assistant (State)");
        entry.Shown=false;
        entry.OnClick=_=>open();
        CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if(CompetitiveGate.Cut){entry.Shown=false;return;}
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRefresh)return;
        nextRefresh=now.AddSeconds(2);
        var snapshot=configuration.StateServerBar ? state():null;
        entry.Shown=StateBar.Shown(snapshot?.Overall,configuration.StateServerBarOnlyProblems);
        if(!entry.Shown || snapshot is null){bounds=null;return;}
        var label=Label(snapshot);
        bounds=AlarmDtrEntry.FindTextNode(gui,label);
        if(!ReferenceEquals(snapshot,shown))
        {
            shown=snapshot;
            entry.Text=new SeStringBuilder().AddText(label).Build();
        }
        entry.Tooltip=configuration.AlarmBarTooltip && bounds is not null ? null:new SeStringBuilder().AddText(text("Dashboard.Title")+"\n"+string.Join("\n",snapshot.Lines)).Build();
    }

    private string Label(MainWindow.DashboardSnapshot snapshot)
        =>snapshot.HeapMegabytes is { } megabytes
            ? format("Dashboard.Bar",[text("Dashboard.Bar"+snapshot.Overall),Math.Round(megabytes).ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current)])
            :format("Dashboard.BarNoMemory",[text("Dashboard.Bar"+snapshot.Overall)]);

    /// <summary>Tensho's tooltip while the mouse is over the entry (ImGui, from the plugin's draw).</summary>
    public void DrawTooltip()
    {
        if(disposed || !configuration.AlarmBarTooltip || !entry.Shown || shown is not { } snapshot || !TooltipScope.UnderBox(bounds,configuration.ServerBarTooltipAbove))return;
        using(TooltipScope.Begin())
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,text("Dashboard.Title"));
            foreach(var line in snapshot.Lines)ImGui.TextUnformatted(line);
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        CompetitiveGate.RemoveAlways(framework,OnUpdate);
        entry.Remove();
    }
}
