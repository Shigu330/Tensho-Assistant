using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// g, C-11 (0.361): the whole sightseeing log, a tab of the map lists: every vista grouped by zone, discovered or not (read in game),
// « possible now » from the Eorzea time window and the weather now in its zone against the wiki's weathers (unknown stays unknown),
// filters « not discovered » and « possible now »; a click places the flag, the tooltip and right click are the map's (next window,
// alarm). Rows rebuilt when a source changes, and once a real minute (« possible now » follows the Eorzea clock).
// X-158 (0.1128): « possible now », the colour and the filter come from VistaWindow, the one calculation the tooltip and the next window use.
internal sealed partial class MainWindow
{
    private bool vistaLogMissingOnly,vistaLogNowOnly;
    private string vistaLogZone="";
    private (object? Index,object? Maps,object? Seen,object? Wiki,object? Weather,bool Missing,bool Now,long Minute) vistaLogKey;
    private IReadOnlyList<(string Key,IReadOnlyList<(MapLocalPosition Vista,bool? Done,VistaWindowState Now)> Rows)> vistaLogGroups=[];
    private (int Done,int Read,int Total) vistaLogCount;

    private void DrawVistaLog(MapLiveState? live)
    {
        if(LocalIndexOrStatus() is not { } index)return;
        wikiVistas?.Request();
        ImGui.Checkbox(localization.Get("VistaLog.MissingOnly")+"##VistaMissing",ref vistaLogMissingOnly);
        Tip("VistaLog.MissingOnlyTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("VistaLog.NowOnly")+"##VistaNow",ref vistaLogNowOnly);
        Tip("VistaLog.NowOnlyTip");
        var catalog=maps.State.Catalog;var seen=live?.Sightseen;var weathers=weather.State.Catalog;
        // An Eorzea minute lasts 2 11/12 real seconds: « possible now » is recomputed once per real minute at most.
        var minute=DateTimeOffset.UtcNow.ToUnixTimeSeconds()/60;
        var key=((object?)index,(object?)catalog,(object?)seen,(object?)wikiVistas?.State.Document,(object?)weathers,vistaLogMissingOnly,vistaLogNowOnly,minute);
        if(!Equals(key,vistaLogKey))
        {
            vistaLogKey=key;
            var utc=DateTimeOffset.UtcNow;
            var all=index.All(MapLocalKind.Sightseeing).OrderBy(vista=>vista.Id).Select(vista=>
            {
                bool? done=seen is null ? null:seen.Contains(vista.Id);
                return (Vista:vista,Done:done,Now:VistaStateAt(vista,catalog?.Find(vista.MapId)?.TerritoryId ?? 0,utc,false));
            }).ToArray();
            vistaLogCount=(all.Count(row=>row.Done==true),seen is null ? 0:all.Length,all.Length);
            vistaLogGroups=all.Where(row=>(!vistaLogMissingOnly || row.Done!=true) && (!vistaLogNowOnly || row.Now.State!=VistaOpen.Closed))
                .GroupBy(row=>catalog?.Find(row.Vista.MapId)?.Zone is { Length:>0 } zone ? zone:"—")
                .OrderBy(group=>group.Min(row=>row.Vista.Id))
                .Select(group=>(group.Key,(IReadOnlyList<(MapLocalPosition,bool?,VistaWindowState)>)group.ToArray())).ToArray();
        }
        ImGui.TextDisabled(vistaLogCount.Read==0 ? localization.Format("VistaLog.CountUnknown",vistaLogCount.Total)
            :localization.Format("VistaLog.Count",vistaLogCount.Done,vistaLogCount.Total));
        if(vistaLogGroups.Count==0){ImGui.TextDisabled(localization.Get("VistaLog.None"));return;}
        DrawSplitView("VistaLog",vistaLogGroups,ref vistaLogZone,row=>
        {
            var (vista,done,possible)=row;
            var mark=done switch{true=>"[x]",false=>"[ ]",_=>"[?]"};
            var name=VistaNumber(vista) is { Length:>0 } number ? number+"  "+vista.Name:vista.Name;
            var nowText=possible.State switch
            {
                VistaOpen.Open=>"  · "+(possible.Until is { } until ? localization.Format("VistaLog.NowUntil",until.ToLocalTime().ToString("t")):localization.Get("VistaLog.Now")),
                VistaOpen.TimeOnly=>"  · "+localization.Get("VistaLog.TimeOnly"),
                _=>"",
            };
            ImGui.PushStyleColor(ImGuiCol.Text,done==true ? UI.FfxivTheme.TextMuted:possible.State==VistaOpen.Open ? UI.FfxivTheme.Success:UI.FfxivTheme.Text);
            try
            {
                if(ImGui.Selectable($"{mark}  {name}{nowText}##VistaLog{vista.Id}") && maps.State.Catalog?.Find(vista.MapId) is { } map)
                    FlagAt(map,new Vector3(vista.X,0,vista.Z));
            }
            finally{ImGui.PopStyleColor();}
            var territory=maps.State.Catalog?.Find(vista.MapId)?.TerritoryId ?? 0;
            if(territory!=0 && ImGui.BeginPopupContextItem($"VistaLogAlarm{vista.Id}"))
            {
                try
                {
                    DrawVistaAlarmEntry(vista,territory);
                    if(maps.State.Catalog?.Find(vista.MapId) is { } vistaMap){ImGui.Separator();PlaceEntries(vistaMap,CoordinateOf(vistaMap,new Vector3(vista.X,0,vista.Z)),"EntityMenu.Here");}
                }
                finally{ImGui.EndPopup();}
            }
            if(ImGui.IsItemHovered())DrawSightseeingTooltip(vista,done);
        });
    }

    // X-158: the weathers a vista needs in its zone: null when not known (no wiki row, weather catalogue not ready, a name not proven for
    // the zone), empty when the wiki row asks none.
    private IReadOnlyCollection<uint>? VistaWeathers(MapLocalPosition vista,uint territory)
        => territory!=0 && weather.State.Catalog is { } catalog && WikiVistaFor(vista) is { } wiki ? WeatherIds(catalog,wiki.Weathers,territory):null;

    private VistaWindowState VistaStateAt(MapLocalPosition vista,uint territory,DateTimeOffset now,bool searchNext)
    {
        var catalog=weather.State.Catalog;
        return VistaWindow.At(vista.MinTime,vista.MaxTime,VistaWeathers(vista,territory),time=>Alarms.AlarmService.WeatherAt(catalog,territory,time),now,searchNext);
    }

    // X-158: the next window, searched off the frame (VistaWindow.SearchDays days at most); cached per vista, minute, wiki row and catalogue.
    private (uint Id,long Minute,object? Wiki,object? Weather,Task<VistaWindowState>? Search) vistaNextCache;
    private VistaWindowState? NextVistaWindow(MapLocalPosition vista,uint territory)
    {
        var minute=DateTimeOffset.UtcNow.ToUnixTimeSeconds()/60;
        var wiki=WikiVistaFor(vista);var catalog=weather.State.Catalog;
        if(vistaNextCache.Search is null || vistaNextCache.Id!=vista.Id || vistaNextCache.Minute!=minute || !ReferenceEquals(vistaNextCache.Wiki,wiki) || !ReferenceEquals(vistaNextCache.Weather,catalog))
        {
            var weathers=VistaWeathers(vista,territory);var (min,max)=(vista.MinTime,vista.MaxTime);var now=DateTimeOffset.UtcNow;
            var previous=vistaNextCache.Id==vista.Id ? vistaNextCache.Search:null;
            var search=Task.Run(()=>VistaWindow.At(min,max,weathers,time=>Alarms.AlarmService.WeatherAt(catalog,territory,time),now,true));
            vistaNextCache=(vista.Id,minute,wiki,catalog,search);
            // The minute before's answer is shown while the new one is searched (no blank line).
            if(!search.IsCompletedSuccessfully && previous is { IsCompletedSuccessfully:true })return previous.Result;
        }
        return vistaNextCache.Search is { IsCompletedSuccessfully:true } done ? done.Result:null;
    }

    // X-158: one line, never empty: open until when; or what is missing and when it opens; or no window in the searched days; or searching.
    private (string Text,Vector4 Colour) VistaStateLine(VistaWindowState state,VistaWindowState? next)
    {
        string Opens()=>next is null ? localization.Get("MapView.VistaSearching"):next.Opens is { } opens
            ? localization.Format("MapView.VistaOpens",opens.ToLocalTime().ToString("g"),TimeLeftUp(opens-DateTimeOffset.UtcNow))
            :localization.Format("MapView.VistaNoWindow",next.SearchedDays);
        return state.State switch
        {
            VistaOpen.Open=>(state.Until is { } until ? localization.Format("MapView.VistaOpenUntil",until.ToLocalTime().ToString("t")):localization.Get("MapView.VistaOpenAlways"),UI.FfxivTheme.Success),
            VistaOpen.TimeOnly=>(localization.Get("MapView.VistaTimeOnly")+" "+Opens(),UI.FfxivTheme.Text),
            _=>(localization.Get(!state.TimeOk && state.WeatherOk==false ? "MapView.VistaMissingBoth":!state.TimeOk ? "MapView.VistaMissingTime":"MapView.VistaMissingWeather")+" "+Opens(),UI.FfxivTheme.Warning),
        };
    }
}
