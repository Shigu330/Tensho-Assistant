using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// g, C-11 (0.361): the whole sightseeing log, a tab of the map lists: every vista grouped by zone, discovered or not (read in game),
// « possible now » from the Eorzea time window and the weather now in its zone against the wiki's weathers (unknown stays unknown),
// filters « not discovered » and « possible now »; a click places the flag, the tooltip and right click are the map's (next window,
// alarm). Rows rebuilt when a source changes, and once a real minute (« possible now » follows the Eorzea clock).
internal sealed partial class MainWindow
{
    private enum VistaNow { No, Yes, TimeOnly }
    private bool vistaLogMissingOnly,vistaLogNowOnly;
    private string vistaLogZone="";
    private (object? Index,object? Maps,object? Seen,object? Wiki,object? Weather,bool Missing,bool Now,long Minute) vistaLogKey;
    private IReadOnlyList<(string Key,IReadOnlyList<(MapLocalPosition Vista,bool? Done,VistaNow Now)> Rows)> vistaLogGroups=[];
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
            var now=EorzeaMinutes();var utc=DateTimeOffset.UtcNow;
            var weatherNow=new Dictionary<uint,uint?>();
            var all=index.All(MapLocalKind.Sightseeing).OrderBy(vista=>vista.Id).Select(vista=>
            {
                bool? done=seen is null ? null:seen.Contains(vista.Id);
                return (Vista:vista,Done:done,Now:PossibleNow(vista));
            }).ToArray();
            vistaLogCount=(all.Count(row=>row.Done==true),seen is null ? 0:all.Length,all.Length);
            vistaLogGroups=all.Where(row=>(!vistaLogMissingOnly || row.Done!=true) && (!vistaLogNowOnly || row.Now!=VistaNow.No))
                .GroupBy(row=>catalog?.Find(row.Vista.MapId)?.Zone is { Length:>0 } zone ? zone:"—")
                .OrderBy(group=>group.Min(row=>row.Vista.Id))
                .Select(group=>(group.Key,(IReadOnlyList<(MapLocalPosition,bool?,VistaNow)>)group.ToArray())).ToArray();

            VistaNow PossibleNow(MapLocalPosition vista)
            {
                if((vista.MinTime!=0 || vista.MaxTime!=0) && !vista.InWindow(now))return VistaNow.No;
                if(WikiVistaFor(vista) is not { } wiki)return VistaNow.TimeOnly;
                if(wiki.Weathers.Count==0)return VistaNow.Yes;
                var territory=catalog?.Find(vista.MapId)?.TerritoryId ?? 0;
                if(territory==0 || weathers is null)return VistaNow.TimeOnly;
                if(!weatherNow.TryGetValue(territory,out var current))
                    weatherNow[territory]=current=weathers.Forecast(territory,utc,1) is [ { Weather:{ } found } ] ? found.Id:null;
                if(current is null)return VistaNow.TimeOnly;
                return wiki.Weathers.Any(name=>weathers.WeatherBySourceName(name,territory)?.Id==current) ? VistaNow.Yes:VistaNow.No;
            }
        }
        ImGui.TextDisabled(vistaLogCount.Read==0 ? localization.Format("VistaLog.CountUnknown",vistaLogCount.Total)
            :localization.Format("VistaLog.Count",vistaLogCount.Done,vistaLogCount.Total));
        if(vistaLogGroups.Count==0){ImGui.TextDisabled(localization.Get("VistaLog.None"));return;}
        DrawSplitView("VistaLog",vistaLogGroups,ref vistaLogZone,row=>
        {
            var (vista,done,possible)=row;
            var mark=done switch{true=>"[x]",false=>"[ ]",_=>"[?]"};
            var name=VistaNumber(vista) is { Length:>0 } number ? number+"  "+vista.Name:vista.Name;
            var nowText=possible switch{VistaNow.Yes=>"  · "+localization.Get("VistaLog.Now"),VistaNow.TimeOnly=>"  · "+localization.Get("VistaLog.TimeOnly"),_=>""};
            ImGui.PushStyleColor(ImGuiCol.Text,done==true ? UI.FfxivTheme.TextMuted:possible==VistaNow.Yes ? UI.FfxivTheme.Success:UI.FfxivTheme.Text);
            try
            {
                if(ImGui.Selectable($"{mark}  {name}{nowText}##VistaLog{vista.Id}") && maps.State.Catalog?.Find(vista.MapId) is { } map)
                    FlagAt(map,new Vector3(vista.X,0,vista.Z));
            }
            finally{ImGui.PopStyleColor();}
            var territory=maps.State.Catalog?.Find(vista.MapId)?.TerritoryId ?? 0;
            if(territory!=0 && ImGui.BeginPopupContextItem($"VistaLogAlarm{vista.Id}"))
            {
                try{DrawVistaAlarmEntry(vista,territory);}
                finally{ImGui.EndPopup();}
            }
            if(ImGui.IsItemHovered())DrawSightseeingTooltip(vista,done);
        });
    }
}
