using TenshoAssistant.Competitive;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using TenshoAssistant.Inventory;

namespace TenshoAssistant.Alarms;

/// <summary>
/// g, I-02 (0.344): a second server info bar entry, « Tensho Assistant · Monnaies » (fixed title, apart from the alarms' entry), shown
/// only while a tracked currency is at or over its threshold and the setting is on: « ⚠ n », its tooltip lists them, a click opens the
/// Currencies tab. Refreshed once a second on the framework thread from the watcher's published state. Removed on dispose.
/// </summary>
public sealed class CurrencyDtrEntry : IDisposable
{
    private readonly IFramework framework;
    private readonly Configuration configuration;
    private readonly Func<CurrencyWatchState?> state;
    private readonly Func<string,string> text;
    private readonly IDtrBarEntry entry;
    private DateTimeOffset nextRefresh;
    private IReadOnlyList<CurrencyBalance>? shown;
    private bool disposed;

    public CurrencyDtrEntry(IDtrBar bar,IFramework framework,Configuration configuration,Func<CurrencyWatchState?> state,Func<string,string> text,Action open)
    {
        this.framework=framework;this.configuration=configuration;this.state=state;this.text=text;
        entry=bar.Get("Tensho Assistant · Monnaies");
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
        if(!entry.Shown || ReferenceEquals(over,shown))return;
        shown=over;
        entry.Text=new SeStringBuilder().AddText($"{SeIconChar.BoxedLetterC.ToIconString()} {over.Count}").Build();
        entry.Tooltip=text("Currencies.BarTitle")+"\n"+string.Join("\n",over.Select(balance=>Line(balance)));
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
