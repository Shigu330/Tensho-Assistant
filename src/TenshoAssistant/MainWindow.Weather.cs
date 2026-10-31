using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private uint weatherTerritory;private string weatherZone="";private long weatherPeriod=-1;private WeatherCatalog? weatherViewCatalog;
    private IReadOnlyList<WeatherPeriod> weatherForecast=[];
    private void OpenWeather(uint territory,string zone){weatherTerritory=territory;weatherZone=zone;weatherPeriod=-1;page=Page.Weather;}
    private void DrawWeather()
    {
        Heading(localization.Get("Weather.Title"),weatherZone);
        DrawGatheringClock();
        if(ImGui.SmallButton(localization.Get("Navigation.Map")))page=Page.Maps;ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Navigation.Gathering")))page=Page.Gathering;
        if(weather.State.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(localization.Get(weather.State.Error is null ? "WeatherDetails.Loading":"WeatherDetails.Error"));
            if(developerMode && weather.State.Error is { } error)ImGui.TextWrapped(error);
            return;
        }
        ImGui.TextWrapped(localization.Get("Weather.Help"));
        var now=DateTimeOffset.UtcNow;var period=now.ToUnixTimeSeconds()/1400;
        if(period!=weatherPeriod || !ReferenceEquals(catalog,weatherViewCatalog))
        {weatherForecast=catalog.Forecast(weatherTerritory,now);weatherPeriod=period;weatherViewCatalog=catalog;}
        ImGui.TextDisabled(localization.Get("Weather.PeriodHelp"));
        if(ImGui.BeginTable("WeatherForecast",4,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                foreach(var title in new[]{"Period","Start","End","Forecast"})ImGui.TableSetupColumn(localization.Get("Weather."+title));
                ImGui.TableHeadersRow();
                for(var i=0;i<weatherForecast.Count;i++)
                {
                    var entry=weatherForecast[i];ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(i==0 ? localization.Get("Weather.Current"):localization.Format("Weather.Next",i));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(entry.StartsAt.ToLocalTime().ToString("g"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted($"{entry.EndsAt.ToLocalTime():g} · {entry.EtHour:00}–{(entry.EtHour+8)%24:00} ET");
                    ImGui.TableNextColumn();
                    if(entry.Weather is { } value){DrawItemIcon(new ItemDefinition(0,value.Name,false,value.Icon),new Vector2(28));ImGui.SameLine();ImGui.TextUnformatted(value.Name);}
                    else
                    {
                        ImGui.TextWrapped(localization.Get("WeatherDetails.Unknown"));
                        if(developerMode && entry.UnknownReason is { } reason)ImGui.TextWrapped(reason);
                    }
                }
            }
            finally{ImGui.EndTable();}
        }
        if(catalog.Zone(weatherTerritory) is { } zone && ImGui.CollapsingHeader(localization.Get("WeatherDetails.Coverage")+"###WeatherCoverage"))
        {
            var total=zone.Shares.Sum(share=>(long)share.Chance);ImGui.TextUnformatted(localization.Format("WeatherDetails.Rates",total));
            foreach(var share in zone.Shares)ImGui.TextUnformatted($"{catalog.Weather(share.WeatherId)?.Name ?? localization.Get("WeatherDetails.Unknown")} : {share.Chance} %");
            if(total!=100)ImGui.TextWrapped(localization.Get("WeatherDetails.Incomplete"));
            if(zone.Individual)ImGui.TextWrapped(localization.Get("WeatherDetails.Individual"));
        }
    }
}
