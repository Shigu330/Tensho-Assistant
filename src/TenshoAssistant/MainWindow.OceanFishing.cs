using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

// g, R-01 (0.392): Ocean fishing page (Production): the next departures (boarding window of 15 minutes every 2 hours), the voyage from
// Limsa Lominsa (Indigo) and from Kugane (Ruby) with their stops and moments of the day, a filter on one voyage, a countdown and an
// alarm before a departure. Timetable from the game's own loop (OceanSchedule).
internal sealed partial class MainWindow
{
    internal Func<OceanSchedule?>? OceanTimetable { get; set; }
    private uint oceanRouteFilter;

    private void DrawOceanFishing()
    {
        Heading(localization.Get("OceanFishing.Title"),localization.Get("OceanFishing.Description"));
        if(OceanTimetable?.Invoke() is not { } schedule){ImGui.TextDisabled(localization.Get("OceanFishing.Unavailable"));return;}
        var now=DateTimeOffset.UtcNow;
        ImGui.SetNextItemWidth(320);
        var selected=schedule.Routes.FirstOrDefault(route=>route.Id==oceanRouteFilter);
        if(ImGui.BeginCombo("##OceanRoute",selected is null ? localization.Get("OceanFishing.AllRoutes"):RouteLabel(selected)))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("OceanFishing.AllRoutes"),selected is null))oceanRouteFilter=0;
                foreach(var route in schedule.Routes)
                    if(ImGui.Selectable(RouteLabel(route)+"##Route"+route.Id,route.Id==oceanRouteFilter))oceanRouteFilter=route.Id;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("OceanFishing.RouteTip");
        var departures=schedule.Next(now,oceanRouteFilter==0 ? 12:8,oceanRouteFilter==0 ? null:oceanRouteFilter);
        if(departures.Count==0){ImGui.TextDisabled(localization.Get("OceanFishing.None"));return;}
        if(!ImGui.BeginTable("OceanDepartures",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var key in new[]{"Departure","In","Indigo","Ruby",""})
                ImGui.TableSetupColumn(key.Length==0 ? "":localization.Get("OceanFishing.Column"+key),key.Length==0 ? ImGuiTableColumnFlags.WidthFixed:ImGuiTableColumnFlags.None,key.Length==0 ? 28:0);
            ImGui.TableHeadersRow();
            foreach(var departure in departures)
            {
                ImGui.PushID(departure.At.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
                try
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    ImGui.TextUnformatted(departure.At.ToLocalTime().ToString("ddd HH:mm"));
                    ImGui.TableNextColumn();
                    if(departure.At<=now)ImGui.TextColored(Success,localization.Format("OceanFishing.Boarding",(int)Math.Ceiling((departure.At+OceanSchedule.Boarding-now).TotalMinutes)));
                    else{var left=departure.At-now;ImGui.TextUnformatted(localization.Format("OceanFishing.Countdown",(int)left.TotalHours,left.Minutes));}
                    ImGui.TableNextColumn();DrawOceanRoute(departure.Aldenard);
                    ImGui.TableNextColumn();DrawOceanRoute(departure.Othard);
                    ImGui.TableNextColumn();
                    if(departure.At>now)
                        AlarmButton("Ocean",localization.Get("OceanFishing.AlarmTip"),()=>[new TenshoAlarm{
                            Label=localization.Format("OceanFishing.AlarmLabel",departure.Aldenard is { } indigo ? RouteLabel(indigo):"—",departure.Othard is { } ruby ? RouteLabel(ruby):"—"),
                            Group=localization.Get("OceanFishing.Title"),Source=AlarmSource.Other,Repeat=AlarmRepeat.Once,At=departure.At,
                            LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead)}]);
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(localization.Get("OceanFishing.Help"));
    }

    private string RouteLabel(OceanRoute route)=>$"{route.Name} ({localization.Get("OceanFishing.Time"+route.Label)})";

    private void DrawOceanRoute(OceanRoute? route)
    {
        if(route is null){ImGui.TextDisabled("—");return;}
        var highlight=route.Id==oceanRouteFilter;
        if(highlight)ImGui.TextColored(Accent,RouteLabel(route));else ImGui.TextUnformatted(RouteLabel(route));
        if(ImGui.IsItemHovered())
            WrappedTooltip(string.Join("\n",route.Stops.Select((stop,index)=>localization.Format("OceanFishing.Stop",index+1,stop.Place,localization.Get("OceanFishing.Time"+stop.Time)))));
    }
}
