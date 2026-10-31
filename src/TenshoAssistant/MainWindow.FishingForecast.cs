using Dalamud.Bindings.ImGui;
using TenshoAssistant.Community;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private FishingConditions? fishingForecastConditions;
    private WeatherCatalog? fishingForecastCatalog;
    private FishingForecastResult? fishingForecastResult;
    private uint fishingForecastTerritory,fishingForecastItem;

    private void DrawFishingForecast(FishingConditions conditions,uint territory)
    {
        if(!ImGui.CollapsingHeader(localization.Get("FishingWindows.Title")+"###FishingForecast"))return;
        var catalog=weather.State.Catalog;
        if(catalog is null)
        {
            ImGui.TextWrapped(localization.Get(weather.State.Error is null ? "FishingWindows.Loading":"FishingWindows.Error"));
            if(developerMode && weather.State.Error is { } error)ImGui.TextWrapped(error);
            return;
        }
        DrawGatheringClock();
        ImGui.TextWrapped(localization.Get("FishingForecast.Help"));
        if(ImGui.SmallButton(localization.Get("FishingWindows.Refresh")+"###RefreshFishingWindows"))
        {
            fishingForecastResult=FishingForecast.Calculate(conditions,catalog,territory,DateTimeOffset.UtcNow);
            fishingForecastConditions=conditions;fishingForecastCatalog=catalog;
            fishingForecastTerritory=territory;fishingForecastItem=(uint)itemId;
        }
        if(!ReferenceEquals(conditions,fishingForecastConditions) || !ReferenceEquals(catalog,fishingForecastCatalog)
            || territory!=fishingForecastTerritory || (uint)itemId!=fishingForecastItem || fishingForecastResult is not { } result)return;
        if(result.Error is not null){ImGui.TextWrapped(localization.Get("FishingWindows.Error"));if(developerMode)ImGui.TextWrapped(result.Error);return;}
        ImGui.TextDisabled(localization.Format("FishingForecast.Horizon",result.CalculatedAt.ToLocalTime().ToString("g"),result.HorizonEndsAt.ToLocalTime().ToString("g")));
        var missing=new List<string>();
        if(!result.ReportedFields.HasFlag(FishingConditionFields.Time))missing.Add(localization.Get("FishingWindows.Time"));
        if(!result.ReportedFields.HasFlag(FishingConditionFields.Weather))missing.Add(localization.Get("FishingWindows.Weather"));
        if(!result.ReportedFields.HasFlag(FishingConditionFields.PreviousWeather))missing.Add(localization.Get("FishingWindows.Previous"));
        if(missing.Count>0)ImGui.TextColored(Accent,localization.Format("FishingWindows.Partial",string.Join(", ",missing)));
        var now=DateTimeOffset.UtcNow;
        if(now<result.CalculatedAt){ImGui.TextWrapped(localization.Get("FishingWindows.ClockChanged"));return;}
        if(now>=result.HorizonEndsAt){ImGui.TextWrapped(localization.Get("FishingWindows.Expired"));return;}
        var anyCandidate=false;
        foreach(var span in result.Spans)
        {
            if(span.EndsAt<=now || span.ReportedConditionsMatch==false)continue;
            anyCandidate=true;
            var label=localization.Get(span.ReportedConditionsMatch==true ? "FishingWindows.Compatible":"FishingWindows.Unknown");
            ImGui.TextColored(span.ReportedConditionsMatch==true ? Accent:Muted,$"{span.StartsAt.ToLocalTime():g}–{span.EndsAt.ToLocalTime():g} · {label}");
        }
        if(!anyCandidate)ImGui.TextWrapped(localization.Get("FishingWindows.Empty"));
        ImGui.TextWrapped(localization.Get("FishingWindows.Disclaimer"));
    }
}
