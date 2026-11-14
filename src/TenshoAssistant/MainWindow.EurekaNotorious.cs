using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;
using TenshoAssistant.Fields;

namespace TenshoAssistant;

// X-205 (0.1241, user request of 2026-11-12, K-35): Eureka's notorious monsters that need a weather or the night, their next window from
// the zone's weather forecast and an alarm. The conditions are community knowledge (Eureka Helper, DEC-044), said as such, hidden while
// « game data and what you observed only » is on; the FATE and weather names are the game's. docs/evidence/0.1241-eureka-notorious.md.
internal sealed partial class MainWindow
{
    // The forecast of the four zones, built at most once a real minute (not in every frame): 72 periods of 8 Eorzea hours, about 28 hours.
    private readonly Dictionary<uint,IReadOnlyList<(DateTimeOffset Start,DateTimeOffset End,int EtHour,uint? Weather)>> eurekaForecast=[];
    private DateTimeOffset eurekaForecastAt;

    private IReadOnlyList<(DateTimeOffset Start,DateTimeOffset End,int EtHour,uint? Weather)>? EurekaForecast(Gathering.WeatherCatalog catalog,uint territory,DateTimeOffset now)
    {
        if(now-eurekaForecastAt>TimeSpan.FromMinutes(1)){eurekaForecast.Clear();eurekaForecastAt=now;}
        if(eurekaForecast.TryGetValue(territory,out var known))return known;
        try{return eurekaForecast[territory]=[.. catalog.Forecast(territory,now,72).Select(period=>(period.StartsAt,period.EndsAt,period.EtHour,period.Weather?.Id))];}
        catch(ArgumentOutOfRangeException){return null;}
    }

    private void DrawFieldNotorious(FieldCatalog catalog)
    {
        ImGui.TextWrapped(localization.Get("FieldContent.NotoriousHint"));
        if(configuration.GameDataOnly){ImGui.TextColored(Muted,localization.Get("FieldContent.NotoriousOff"));return;}
        if(weather.State.Catalog is not { } weathers){ImGui.TextDisabled(localization.Get("FieldContent.NotoriousLoading"));return;}
        var now=DateTimeOffset.UtcNow;
        foreach(var zone in catalog.Zones.Where(zone=>EurekaNotoriousBoard.All.Any(monster=>monster.Territory==zone.Territory)))
        {
            var rows=EurekaNotoriousBoard.All.Where(monster=>monster.Territory==zone.Territory)
                .Select(monster=>(Monster:monster,Name:zone.Events.FirstOrDefault(row=>row.Kind==FieldEventKind.Fate && row.Id==monster.Fate)?.Name ?? $"#{monster.Fate}"))
                .Where(row=>FieldBoard.Matches(fieldQuery,row.Name)).ToArray();
            if(rows.Length==0)continue;
            ImGui.Spacing();ImGui.TextColored(Accent,zone.Name);
            if(!ImGui.BeginTable($"###FieldNotorious{zone.Territory}",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))continue;
            ImGui.TableSetupColumn(localization.Get("FieldContent.ColumnName"));
            ImGui.TableSetupColumn(localization.Get("FieldContent.ColumnCondition"));
            ImGui.TableSetupColumn(localization.Get("FieldContent.ColumnWindow"));
            ImGui.TableSetupColumn("",ImGuiTableColumnFlags.WidthFixed,90);
            ImGui.TableHeadersRow();
            var forecast=EurekaForecast(weathers,zone.Territory,now);
            foreach(var (monster,name) in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(name);
                ImGui.TableNextColumn();ImGui.TextWrapped(NotoriousCondition(monster,weathers));
                ImGui.TableNextColumn();
                EurekaWindow? next=null;
                if(forecast is not null)foreach(var open in EurekaNotoriousBoard.Windows(monster,forecast))if(open.End>now){next=open;break;}
                if(next is not { } window)ImGui.TextDisabled(localization.Get("FieldContent.NotoriousNoWindow"));
                else if(window.Start<=now)ImGui.TextColored(Success,localization.Format("FieldContent.NotoriousNow",window.End.ToLocalTime().ToString("t")));
                else ImGui.TextUnformatted(localization.Format("FieldContent.NotoriousNext",window.Start.ToLocalTime().ToString("t"),TimeLeftUp(window.Start-now)));
                ImGui.TableNextColumn();
                if(ImGui.SmallButton(localization.Get("FieldContent.NotoriousAlarm")+"##NmAlarm"+monster.Fate))
                    AddAlarms(AlarmSchedule.ForWindows(name,EurekaNotoriousBoard.AlarmMinutes(monster),DefaultAlarmLead,monster.Territory,0,monster.Weather is { } needed ? [needed]:null));
                Tip("FieldContent.NotoriousAlarmTip");
            }
            ImGui.EndTable();
        }
    }

    // « Gales », « Gales, for the monster that calls it », « night (19:00–06:00 ET), for the monster that calls it », or both.
    private string NotoriousCondition(EurekaNotorious monster,Gathering.WeatherCatalog weathers)
    {
        var parts=new List<string>();
        if(monster.Weather is { } id)
        {
            var name=weathers.Weather(id)?.Name ?? $"#{id}";
            parts.Add(monster.WeatherOn==EurekaConditionOn.Spawner ? localization.Format("FieldContent.NotoriousSpawnerWeather",name):name);
        }
        if(monster.Night)parts.Add(localization.Format("FieldContent.NotoriousNight",Gathering.GatheringTime.Clock(EurekaNotoriousBoard.NightStart),Gathering.GatheringTime.Clock(EurekaNotoriousBoard.NightEnd)));
        return string.Join(" · ",parts);
    }
}
