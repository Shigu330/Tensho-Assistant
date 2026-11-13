using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TenshoAssistant.Competitive;

namespace TenshoAssistant.Inventory;

/// <summary>
/// X-107 (0.986, user choice DEC-045, from I-04): « Tensho Assistant (Gil) » in the server info bar: the character's gil of the day (net), its tooltip
/// gained and spent, a click opens the gil journal. Only with its setting (off by default). Refreshed every 2 s from the journal; removed on dispose.
/// X-162 (0.1129, user report: its game tooltip opened above while the others opened below): Tensho's own tooltip, under the entry (or above
/// it, setting) like the alarms', when its text is found on screen; the game's tooltip otherwise.
/// </summary>
public sealed class GilDtrEntry : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly Configuration configuration;
    private readonly Func<bool> enabled;
    private readonly Func<IReadOnlyList<GilEntry>?> entries;
    private readonly Func<string,string> text;
    private readonly IDtrBarEntry entry;
    private DateTimeOffset nextRefresh;
    private (long,long)? shown;
    private (Vector2 Min,Vector2 Max)? bounds;
    private string tip="";
    private bool disposed;

    public GilDtrEntry(IDtrBar bar,IFramework framework,IGameGui gui,Configuration configuration,Func<bool> enabled,Func<IReadOnlyList<GilEntry>?> entries,Func<string,string> text,Action open)
    {
        this.framework=framework;this.gui=gui;this.configuration=configuration;this.enabled=enabled;this.entries=entries;this.text=text;
        entry=bar.Get("Tensho Assistant (Gil)");
        entry.Shown=false;
        entry.OnClick=_=>open();
        CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        // DEC-038: gone from the server bar during a competitive cut.
        if(CompetitiveGate.Cut){entry.Shown=false;return;}
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRefresh)return;
        nextRefresh=now.AddSeconds(2);
        if(!enabled() || entries() is not { } all){entry.Shown=false;shown=null;bounds=null;return;}
        var today=GilJournal.Today(all,now);
        entry.Shown=true;
        var label=$"{SeIconChar.Gil.ToIconString()} {(today.Gained-today.Spent):+#,0;-#,0;0}";
        bounds=Alarms.AlarmDtrEntry.FindTextNode(gui,label);
        if(shown!=today)
        {
            shown=today;
            entry.Text=new SeStringBuilder().AddText(label).Build();
            tip=string.Format(TenshoAssistant.Localization.TenshoCulture.Current,text("GilJournal.BarTip"),today.Gained.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current),today.Spent.ToString("N0",TenshoAssistant.Localization.TenshoCulture.Current));
        }
        entry.Tooltip=configuration.AlarmBarTooltip && bounds is not null ? null:new SeStringBuilder().AddText(tip).Build();
    }

    /// <summary>Tensho's tooltip while the mouse is over the entry (ImGui, from the plugin's draw).</summary>
    public void DrawTooltip()
    {
        if(disposed || !configuration.AlarmBarTooltip || !entry.Shown || !TooltipScope.UnderBox(bounds,configuration.ServerBarTooltipAbove))return;
        using(TooltipScope.Begin())ImGui.TextUnformatted(tip);
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        CompetitiveGate.RemoveAlways(framework,OnUpdate);
        entry.Remove();
    }
}
