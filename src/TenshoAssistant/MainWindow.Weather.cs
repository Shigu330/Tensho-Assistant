using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Weather page (World): forecast of a zone computed from the game's weather rate tables (Gathering/WeatherForecast.cs): the current
// and next 11 Eorzea periods (8 ET hours each) with local and ET times, a search for the next occurrences of one weather (about
// three real days ahead), the zone's weather shares, and alarm buttons (Alarms/) for a coming period or weather. Opened from the
// Maps page, the alarms and the gathering / fishing tools through OpenWeather.
internal sealed partial class MainWindow
{
    private uint weatherTerritory;private string weatherZone="";private long weatherPeriod=-1;private WeatherCatalog? weatherViewCatalog;
    private IReadOnlyList<WeatherPeriod> weatherForecast=[];
    private string weatherZoneQuery="";private uint weatherSearchId;private IReadOnlyList<WeatherPeriod> weatherSearchResult=[];private (uint,uint,long) weatherSearchKey;
    private void OpenWeather(uint territory,string zone){weatherTerritory=territory;weatherZone=zone;weatherPeriod=-1;weatherSearchId=0;page=Page.Weather;}
    // 0.563 (C31): several zones in one table, computed once per period (1 400 s), not at each frame.
    private const int WeatherFavoritePeriods=6,WeatherFavoriteZones=12;
    private long weatherFavoritesPeriod=-1;private readonly Dictionary<uint,IReadOnlyList<WeatherPeriod>> weatherFavoritesGrid=[];
    private void DrawWeatherFavorites(WeatherCatalog catalog)
    {
        var favorites=configuration.WeatherFavorites;
        if(weatherTerritory!=0)
        {
            var kept=favorites?.ContainsKey(weatherTerritory)==true;
            if(kept || (favorites?.Count ?? 0)<WeatherFavoriteZones)
            {
                if(ImGui.SmallButton(localization.Get(kept ? "Weather.FavoriteRemove":"Weather.FavoriteAdd")+"##WeatherFavorite"))
                {
                    favorites=configuration.WeatherFavorites??=[];
                    if(kept)favorites.Remove(weatherTerritory);else favorites[weatherTerritory]=weatherZone;
                    weatherFavoritesPeriod=-1;
                    try{save();}catch(Exception error){log.Warning(error,"Could not save the weather zones.");}
                }
                Tip("Weather.FavoriteTip");
            }
        }
        if(favorites is not { Count:>0 })return;
        var now=DateTimeOffset.UtcNow;var period=now.ToUnixTimeSeconds()/1400;
        if(period!=weatherFavoritesPeriod || weatherFavoritesGrid.Count!=favorites.Count)
        {
            weatherFavoritesGrid.Clear();
            foreach(var territory in favorites.Keys)weatherFavoritesGrid[territory]=catalog.Forecast(territory,now,WeatherFavoritePeriods);
            weatherFavoritesPeriod=period;
        }
        if(!ImGui.BeginTable("WeatherFavorites",WeatherFavoritePeriods+1,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Weather.Zone"));
            var first=weatherFavoritesGrid.Values.FirstOrDefault();
            for(var column=0;column<WeatherFavoritePeriods;column++)
                ImGui.TableSetupColumn(first is not null && column<first.Count ? first[column].StartsAt.ToLocalTime().ToString("t"):"");
            ImGui.TableHeadersRow();
            foreach(var (territory,name) in favorites.OrderBy(pair=>pair.Value,StringComparer.CurrentCultureIgnoreCase))
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();
                if(ImGui.Selectable(name+$"##WeatherFavorite{territory}",territory==weatherTerritory))OpenWeather(territory,name);
                if(!weatherFavoritesGrid.TryGetValue(territory,out var row))continue;
                foreach(var entry in row.Take(WeatherFavoritePeriods))
                {
                    ImGui.TableNextColumn();
                    if(entry.Weather is not { } value){ImGui.TextDisabled("?");continue;}
                    DrawItemIcon(new ItemDefinition(0,value.Name,false,value.Icon),new Vector2(24));
                    if(ImGui.IsItemHovered())WrappedTooltip($"{value.Name}\n{entry.StartsAt.ToLocalTime():g} · {entry.EtHours} ET");
                }
            }
        }
        finally{ImGui.EndTable();}
    }
    private void DrawWeather()
    {
        Heading(localization.Get("Weather.Title"),weatherZone);
        DrawGatheringClock();
        DrawAlarmsLink();
        if(ImGui.SmallButton(localization.Get("Navigation.Map")))page=Page.Maps;ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Navigation.Gathering")))page=Page.Gathering;
        if(weather.State.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(localization.Get(weather.State.Error is null ? "WeatherDetails.Loading":"WeatherDetails.Error"));
            if(developerMode && weather.State.Error is { } error)ImGui.TextWrapped(RawMessage(error));
            DrawAlarmsLink();
            return;
        }
        DrawWeatherZonePicker(catalog);
        DrawWeatherFavorites(catalog);
        if(weatherTerritory==0){ImGui.TextWrapped(localization.Get("Weather.ChooseZone"));DrawAlarmsLink();return;}
        ImGui.TextWrapped(localization.Get("Weather.Help"));
        var now=DateTimeOffset.UtcNow;var period=now.ToUnixTimeSeconds()/1400;
        if(period!=weatherPeriod || !ReferenceEquals(catalog,weatherViewCatalog))
        {weatherForecast=catalog.Forecast(weatherTerritory,now,12);weatherPeriod=period;weatherViewCatalog=catalog;weatherSearchKey=default;}
        ImGui.TextDisabled(localization.Get("Weather.PeriodHelp"));
        if(ImGui.BeginTable("WeatherForecast",5,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                foreach(var title in new[]{"Period","Start","End","Forecast"})ImGui.TableSetupColumn(localization.Get("Weather."+title));
                ImGui.TableSetupColumn("",ImGuiTableColumnFlags.WidthFixed,28);
                ImGui.TableHeadersRow();
                for(var i=0;i<weatherForecast.Count;i++)
                {
                    var entry=weatherForecast[i];ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(i==0 ? localization.Get("Weather.Current"):localization.Format("Weather.Next",i));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(entry.StartsAt.ToLocalTime().ToString("g"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted($"{entry.EndsAt.ToLocalTime():g} · {entry.EtHours} ET");
                    ImGui.TableNextColumn();
                    if(entry.Weather is { } value){DrawItemIcon(new ItemDefinition(0,value.Name,false,value.Icon),new Vector2(28));ImGui.SameLine();ImGui.TextUnformatted(value.Name);}
                    else
                    {
                        ImGui.TextWrapped(localization.Get("WeatherDetails.Unknown"));
                        if(developerMode && entry.UnknownReason is { } reason)ImGui.TextWrapped(RawMessage(reason));
                    }
                    ImGui.TableNextColumn();
                    // Single alarm at the start of this period (not for the current one, already started).
                    if(i>0 && entry.Weather is { } named)
                        AlarmButton($"Period{entry.StartsAt.ToUnixTimeSeconds()}",localization.Format("Alarms.PeriodTooltip",named.Name,entry.StartsAt.ToLocalTime().ToString("g")),
                            ()=>[new TenshoAlarm{Label=$"{named.Name} · {weatherZone}",Group=$"{named.Name} · {weatherZone}",IconId=named.Icon,Repeat=AlarmRepeat.Once,At=entry.StartsAt,LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead),TerritoryId=weatherTerritory}]);
                }
            }
            finally{ImGui.EndTable();}
        }
        DrawWeatherSearch(catalog,now);
        if(catalog.Zone(weatherTerritory) is { } zone && TenshoAssistant.UI.UiComponents.Header(localization.Get("WeatherDetails.Coverage")+"###WeatherCoverage"))
        {
            var total=zone.Shares.Sum(share=>(long)share.Chance);ImGui.TextUnformatted(localization.Format("WeatherDetails.Rates",total));
            foreach(var share in zone.Shares)ImGui.TextUnformatted($"{catalog.Weather(share.WeatherId)?.Name ?? localization.Get("WeatherDetails.Unknown")} : {share.Chance} %");
            if(total!=100)ImGui.TextWrapped(localization.Get("WeatherDetails.Incomplete"));
            if(zone.Individual)ImGui.TextWrapped(localization.Get("WeatherDetails.Individual"));
        }
        DrawAlarmsLink();
    }

    // Any zone with a weather table, by name (maps of the game, one entry per territory).
    private void DrawWeatherZonePicker(WeatherCatalog catalog)
    {
        ImGui.SetNextItemWidth(260);
        var open=ImGui.BeginCombo(localization.Get("Weather.Zone")+"##WeatherZone",weatherZone.Length>0 ? weatherZone:localization.Get("Weather.ChooseZone"));Tip("Weather.ZoneTip");if(!open)return;
        try
        {
            ImGui.SetNextItemWidth(-1);ImGui.InputTextWithHint("##WeatherZoneQuery",localization.Get("Weather.ZoneSearch"),ref weatherZoneQuery,64);Tip("Weather.ZoneSearchTip");
            foreach(var map in (maps.State.Catalog?.Entries ?? []).Where(map=>map.TerritoryId!=0 && map.Zone.Length>0 && catalog.Zone(map.TerritoryId) is { RateId:not 0 })
                .DistinctBy(map=>map.TerritoryId).Where(map=>weatherZoneQuery.Length==0 || map.Zone.Contains(weatherZoneQuery,StringComparison.CurrentCultureIgnoreCase)).Take(200))
                if(ImGui.Selectable($"{map.Zone}##Zone{map.TerritoryId}",map.TerritoryId==weatherTerritory))OpenWeather(map.TerritoryId,map.Zone);
        }
        finally{ImGui.EndCombo();}
    }

    // Next occurrences of one weather of the zone (about three real days ahead), with single or daily alarms.
    private void DrawWeatherSearch(WeatherCatalog catalog,DateTimeOffset now)
    {
        if(catalog.Zone(weatherTerritory) is not { } zone || !TenshoAssistant.UI.UiComponents.Header(localization.Get("Weather.SearchTitle")+"###WeatherSearch",ImGuiTreeNodeFlags.DefaultOpen))return;
        var choices=zone.Shares.Where(share=>share.Chance>0).Select(share=>catalog.Weather(share.WeatherId)).OfType<WeatherDefinition>().DistinctBy(row=>row.Id).ToArray();
        if(choices.Length==0){ImGui.TextDisabled(localization.Get("WeatherDetails.Unknown"));return;}
        ImGui.SetNextItemWidth(200);
        if(ImGui.BeginCombo(localization.Get("Weather.SearchWeather")+"##WeatherSearchCombo",catalog.Weather(weatherSearchId)?.Name ?? localization.Get("Weather.ChooseWeather")))
        {
            try{foreach(var choice in choices)if(ImGui.Selectable($"{choice.Name}##W{choice.Id}",choice.Id==weatherSearchId))weatherSearchId=choice.Id;}
            finally{ImGui.EndCombo();}
        }
        Tip("Weather.SearchWeatherTip");
        if(weatherSearchId==0 || catalog.Weather(weatherSearchId) is not { } wanted)return;
        var key=(weatherTerritory,weatherSearchId,now.ToUnixTimeSeconds()/1400);
        if(key!=weatherSearchKey)
        {
            var found=new List<WeatherPeriod>();var from=now;
            for(var batch=0;batch<8 && found.Count<8;batch++)
            {
                var periods=catalog.Forecast(weatherTerritory,from,24);
                found.AddRange(periods.Where(entry=>entry.Weather?.Id==wanted.Id && entry.EndsAt>now).Take(8-found.Count));
                from=periods[^1].EndsAt;
            }
            weatherSearchResult=found;weatherSearchKey=key;
        }
        AlarmButton("WeatherDaily",localization.Format("Alarms.WeatherDailyTooltip",wanted.Name),
            ()=>AlarmSchedule.ForWindows($"{wanted.Name} · {weatherZone}",[0,480,960],DefaultAlarmLead,weatherTerritory,0,[wanted.Id])
                .Select(created=>{created.Group=$"{wanted.Name} · {weatherZone}";created.IconId=wanted.Icon;return created;}).ToArray(),daily:true);
        ImGui.SameLine();ImGui.TextUnformatted(localization.Format("Weather.EveryTime",wanted.Name));
        if(weatherSearchResult.Count==0)ImGui.TextDisabled(localization.Get("Weather.NotSoon"));
        foreach(var entry in weatherSearchResult)
        {
            // 0.330: no bell for a period already started (a single alarm in the past never rings), as in the forecast table; its space keeps the list aligned.
            if(entry.StartsAt>now)
                AlarmButton($"Search{entry.StartsAt.ToUnixTimeSeconds()}",localization.Format("Alarms.PeriodTooltip",wanted.Name,entry.StartsAt.ToLocalTime().ToString("g")),
                    ()=>[new TenshoAlarm{Label=$"{wanted.Name} · {weatherZone}",Group=$"{wanted.Name} · {weatherZone}",IconId=wanted.Icon,Repeat=AlarmRepeat.Once,At=entry.StartsAt,LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead),TerritoryId=weatherTerritory}]);
            else AlarmButtonSpace();
            ImGui.SameLine();
            ImGui.TextUnformatted(localization.Format("Weather.Occurrence",entry.StartsAt.ToLocalTime().ToString("g"),entry.EndsAt.ToLocalTime().ToString("t"),entry.EtHours,
                Math.Max(0,(int)(entry.StartsAt-now).TotalMinutes)));
        }
    }
}
