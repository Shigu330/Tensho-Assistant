using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Monsters;

namespace TenshoAssistant;

// « Other sources » of a fish (Gathering page): Garland Tools' item document, fetched on request (one request at a time, cached for the
// sources duration): for each spot linked to the game's fishing spots, the ET window, weather and previous weather, alarm and coming windows
// (MainWindow.FishingForecast.cs), zone weather, and the bait routes with mooches. Shown below GatherBuddy when it knows the fish, alone
// otherwise.
internal sealed partial class MainWindow
{
    private void DrawFishingSources(GatheringCatalog catalog)
    {
        if(catalog.Fishing.ForItem((uint)itemId).Count==0 || !TenshoAssistant.UI.UiComponents.Header(localization.Get("FishingSources.Title")+"###FishingCommunity"))return;
        ImGui.TextWrapped(localization.Get("FishingSources.Help"));
        var state=monsterLoot.State;
        if(state.ItemId!=(uint)itemId)return;
        ImGui.BeginDisabled(state.Loading);
        try
        {
            if(SourceButton(localization.Get(state.Item is null ? "FishingSources.Fetch":"FishingSources.Refresh"),state.Item is null || configuration.CommunityGarland))_ = monsterLoot.RequestItemAsync(state.Item is not null);
        }
        finally{ImGui.EndDisabled();}
        if(state.Loading)ImGui.TextDisabled(localization.Get("FishingSources.Loading"));
        if(state.Error is { } error){ImGui.TextWrapped(localization.Get(SourceOff(error) ? "Advanced.SourceOffTip":"FishingSources.Error"));if(developerMode)ImGui.TextWrapped(RawMessage(error));}
        if(state.Item is not { } document)return;
        ImGui.TextDisabled(localization.Format("FishingSources.Fetched",document.FetchedAt.ToLocalTime().ToString("g")));
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Sources,document.FetchedAt);
        if(document.FishingError is not null){ImGui.TextWrapped(localization.Get("FishingSources.Invalid"));return;}
        if(document.Fishing is not {SpotsKnown:true} fishing){ImGui.TextWrapped(localization.Get("FishingSources.Missing"));return;}
        if(fishing.Spots.Count==0)ImGui.TextWrapped(localization.Get("FishingSources.Empty"));
        for(var index=0;index<fishing.Spots.Count;index++)
        {
            var spot=fishing.Spots[index];ImGui.PushID(index);
            try
            {
                var native=spot.SpotId is { } id ? catalog.ForFishingSpot(id):null;
                var title=native is null ? localization.Get("FishingSources.Unlinked"):$"{native.Zone} · {native.Place}";
                if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("FishingSources.Variant",title,index+1)+"###FishingSource"))continue;
                if(spot.Conditions is { } conditions)
                {
                    ImGui.TextColored(Accent,localization.Get("FishingSources.Times"));
                    if(conditions.Error is not null)ImGui.TextWrapped(localization.Get("FishingSources.InvalidConditions"));
                    else
                    {
                        DrawGatheringTime(conditions.Timing);
                        DrawFishingWeatherNames(localization.Get("FishingWindows.Weather"),conditions.Weather,native?.TerritoryId);
                        DrawFishingWeatherNames(localization.Get("FishingWindows.Previous"),conditions.PreviousWeather,native?.TerritoryId);
                        if(native is not null)DrawFishingAlarmButton($"Fish{index}",conditions,native.TerritoryId);
                        if(native is not null)DrawFishingForecast(conditions,native.TerritoryId);
                    }
                    if(native is not null && ImGui.SmallButton(localization.Get("FishingSources.Forecast")))OpenWeather(native.TerritoryId,native.Zone);
                    ImGui.TextWrapped(localization.Get("FishingSources.TimeHelp"));
                    ImGui.Separator();
                }
                ImGui.TextColored(Accent,localization.Get("FishingSources.Baits"));
                if(spot.BaitChains is null)ImGui.TextDisabled(localization.Get("FishingSources.MissingBait"));
                else if(spot.BaitChains.Count==0)ImGui.TextDisabled(localization.Get("FishingSources.EmptyBait"));
                else for(var route=0;route<spot.BaitChains.Count;route++)
                {
                    ImGui.PushID(route);
                    try
                    {
                        if(spot.BaitChains.Count>1)ImGui.TextDisabled(localization.Format("FishingSources.Alternative",route+1));
                        var chain=spot.BaitChains[route];
                        for(var step=0;step<chain.Count;step++)
                        {
                            var item=itemCatalog.State.Catalog?.Find(chain[step]);ImGui.PushID(step);
                            try
                            {
                                if(step>0)ImGui.TextDisabled(localization.Get("FishingSources.Mooch"));
                                if(item is not null)ItemCell(item,item.Id,28);
                                else ImGui.TextDisabled(localization.Get("FishingSources.UnknownItem"));
                            }
                            finally{ImGui.PopID();}
                        }
                    }
                    finally{ImGui.PopID();}
                }
                ImGui.Separator();
            }
            finally{ImGui.PopID();}
        }
        ImGui.TextWrapped(localization.Get("FishingSources.Disclaimer"));
    }

    private void DrawFishingWeatherNames(string title,IReadOnlyList<string>? names,uint? territory)
    {
        if(names is null)ImGui.TextDisabled(localization.Format("FishingWeather.Missing",title));
        else if(names.Count==0)ImGui.TextDisabled(localization.Format("FishingWeather.Empty",title));
        else
        {
            ImGui.TextDisabled(localization.Format("FishingWeather.Alternatives",title));
            foreach(var name in names)
            {
                var matched=territory is { } id ? weather.State.Catalog?.WeatherBySourceName(name,id):null;
                if(matched is null)ImGui.TextWrapped(localization.Format("FishingWeather.Unmatched",name));
                else
                {
                    DrawItemIcon(new TenshoAssistant.Items.ItemDefinition(0,matched.Name,false,matched.Icon),new Vector2(24));
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FishingWeather.Source",name));
                    ImGui.SameLine();ImGui.TextUnformatted(matched.Name);
                }
            }
        }
    }
}
