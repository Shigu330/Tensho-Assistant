using Dalamud.Bindings.ImGui;
using TenshoAssistant.Community;

namespace TenshoAssistant;

/// <summary>
/// N-05 (0.445): Maps › « Plots for sale » (folded) — the open plots of the world you are on, from PaissaDB (read only, only the world id
/// is sent): « Fetch » on click; with « Watch » (setting, off by default) a read every N minutes (10-240) and a chat line for each plot
/// newly open that matches the size filter. Districts named in the game's language when the id is a residential area.
/// </summary>
internal sealed partial class MainWindow
{
    internal PaissaDbService? Paissa { get; set; }
    internal Action<string>? PlotAlert { get; set; }
    private DateTimeOffset plotWatchAt;

    private bool PlotMatches(OpenPlot plot)=>configuration.PlotWatchSize==0 || plot.Size==configuration.PlotWatchSize-1;

    /// <summary>Called every frame: the watch's next read when it is due.</summary>
    internal void WatchPlots()
    {
        if(!configuration.PlotWatch || Paissa is not { } paissa || market.State.WorldId==0)return;
        var now=DateTimeOffset.UtcNow;
        if(now<plotWatchAt)return;
        plotWatchAt=now.AddMinutes(Math.Clamp(configuration.PlotWatchMinutes,10,240));
        var world=market.State.WorldName;
        paissa.Request(market.State.WorldId,(previous,current)=>
        {
            foreach(var plot in PaissaDb.New(previous,current).Where(PlotMatches))
                PlotAlert?.Invoke(localization.Format("Plots.Alert",world,PlotDistrict(plot),plot.Ward,plot.Plot,PlotSize(plot.Size),plot.Price.ToString("N0")));
        });
    }

    private string PlotDistrict(OpenPlot plot)=>HousingDistricts?.Invoke().FirstOrDefault(district=>district.Territory==plot.District).Name is { Length:>0 } name ? name:plot.DistrictName;
    private string PlotSize(int size)=>localization.Get(size switch{2=>"Plots.Large",1=>"Plots.Medium",_=>"Plots.Small"});

    private void DrawPlots()
    {
        if(Paissa is not { } paissa)return;
        ImGui.TextWrapped(localization.Get("Plots.Help"));
        var world=market.State;
        BoolSetting("Plots.Watch","Plots.WatchTip",()=>configuration.PlotWatch,value=>configuration.PlotWatch=value,c=>c.PlotWatch);
        IntSetting("Plots.Minutes","Plots.MinutesTip",()=>configuration.PlotWatchMinutes,value=>configuration.PlotWatchMinutes=value,10,240,DefaultConfiguration.PlotWatchMinutes);
        // X-76 H5 (0.915): shared combo row (rollback when saving fails, « ↺ »); 0 = any size, then small, medium, large.
        ComboSetting("Plots.Size","Plots.SizeTip",()=>configuration.PlotWatchSize,value=>configuration.PlotWatchSize=value,
            [(0,localization.Get("Plots.AnySize")),(1,PlotSize(0)),(2,PlotSize(1)),(3,PlotSize(2))],DefaultConfiguration.PlotWatchSize);
        var state=paissa.State;
        ImGui.BeginDisabled(world.WorldId==0 || state.Loading);
        try{if(ImGui.SmallButton(localization.Format("Plots.Fetch",world.WorldName)))paissa.Request(world.WorldId);}
        finally{ImGui.EndDisabled();}
        if(state.Loading)ImGui.TextDisabled(localization.Get("Plots.Loading"));
        if(state.Error is { } error)DrawDataError(error,"Plots.Error");
        if(state.WorldId!=world.WorldId || state.Plots is not { } plots)return;
        if(state.ReadAt is { } at)ImGui.TextDisabled(localization.Format("Plots.ReadAt",at.ToLocalTime().ToString("t")));
        var shown=plots.Where(PlotMatches).ToArray();
        if(shown.Length==0){ImGui.TextDisabled(localization.Get("Plots.None"));return;}
        foreach(var plot in shown.Take(80))
            ImGui.TextUnformatted(localization.Format("Plots.Row",PlotDistrict(plot),plot.Ward,plot.Plot,PlotSize(plot.Size),plot.Price.ToString("N0"),
                localization.Get(plot.Lottery ? "Plots.Lottery":"Plots.FirstCome"),plot.LottoEntries?.ToString() ?? "?"));
        if(shown.Length>80)ImGui.TextDisabled(localization.Format("Events.More",shown.Length-80));
        ImGui.TextDisabled(localization.Get("Plots.Credit"));
    }
}
