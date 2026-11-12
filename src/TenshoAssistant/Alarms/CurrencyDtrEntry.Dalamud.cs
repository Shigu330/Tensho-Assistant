using TenshoAssistant.Competitive;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using TenshoAssistant.Inventory;

namespace TenshoAssistant.Alarms;

/// <summary>
/// g, I-02 (0.344): a second server info bar entry, « Tensho Assistant (Currencies) » (fixed title, apart from the alarms' entry; 0.910: renamed from
/// a French title, with the user's agreement: Dalamud keys the entry's settings by its title, so they start afresh), shown
/// only while a tracked currency is at or over its threshold and the setting is on: « ⚠ n », its tooltip lists them, a click opens the
/// Currencies tab. Refreshed once a second on the framework thread from the watcher's published state. Removed on dispose.
/// X-162 (0.1129): Tensho's own tooltip, under the entry (or above it, setting) like the alarms', when its text is found on screen.
/// </summary>
public sealed class CurrencyDtrEntry : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly Configuration configuration;
    private readonly Func<CurrencyWatchState?> state;
    private readonly Func<string,string> text;
    private readonly IDtrBarEntry entry;
    private DateTimeOffset nextRefresh;
    private IReadOnlyList<CurrencyBalance>? shown;
    private (Vector2 Min,Vector2 Max)? bounds;
    private IReadOnlyList<string> lines=[];
    private bool disposed;

    public CurrencyDtrEntry(IDtrBar bar,IFramework framework,IGameGui gui,Configuration configuration,Func<CurrencyWatchState?> state,Func<string,string> text,Action open)
    {
        this.framework=framework;this.gui=gui;this.configuration=configuration;this.state=state;this.text=text;
        entry=bar.Get("Tensho Assistant (Currencies)");
        entry.Shown=false;
        entry.OnClick=_=>open();
        CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        // DEC-038: gone from the server bar during a competitive cut; the next tick after it shows it again.
        if(CompetitiveGate.Cut){entry.Shown=false;return;}
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRefresh)return;
        nextRefresh=now.AddSeconds(1);
        var over=configuration.CurrencyServerBar ? state()?.Over ?? []:[];
        entry.Shown=over.Count>0;
        if(!entry.Shown){bounds=null;return;}
        var label=$"{SeIconChar.BoxedLetterC.ToIconString()} {over.Count}";
        bounds=AlarmDtrEntry.FindTextNode(gui,label);
        if(!ReferenceEquals(over,shown))
        {
            shown=over;
            entry.Text=new SeStringBuilder().AddText(label).Build();
            lines=over.Select(balance=>Line(balance)).ToArray();
        }
        entry.Tooltip=configuration.AlarmBarTooltip && bounds is not null ? null:new SeStringBuilder().AddText(text("Currencies.BarTitle")+"\n"+string.Join("\n",lines)).Build();
    }

    /// <summary>Tensho's tooltip while the mouse is over the entry (ImGui, from the plugin's draw).</summary>
    public void DrawTooltip()
    {
        if(disposed || !configuration.AlarmBarTooltip || !entry.Shown || !TooltipScope.UnderBox(bounds,configuration.ServerBarTooltipAbove))return;
        using(TooltipScope.Begin())
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,text("Currencies.BarTitle"));
            foreach(var line in lines)ImGui.TextUnformatted(line);
        }
    }

    /// <summary>« Name : 3 400 / 4 000 (85 %) ».</summary>
    public static string Line(CurrencyBalance balance)
        =>$"{balance.Currency.Name} : {balance.Amount:N0} / {balance.Cap:N0} ({balance.Percent:0} %)";

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        CompetitiveGate.RemoveAlways(framework,OnUpdate);
        entry.Remove();
    }
}
