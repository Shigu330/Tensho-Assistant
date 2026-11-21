using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    // X-684 (0.1672): the facts of the place under the mouse, worked out once and kept until the place, the minute or the teleport list
    // changes (a tooltip is drawn at each frame while the mouse stays).
    private (uint Map,Vector2? Coordinate,long Minute,object? Costs,PlaceTipFacts Facts)? placeTipSeen;

    private PlaceTipFacts PlaceTipFactsOf(MapDefinition map,Vector2? coordinate)
    {
        var now=DateTimeOffset.UtcNow;var minute=now.ToUnixTimeSeconds()/60;var costs=mapLive?.TeleportCosts;
        if(placeTipSeen is { } seen && seen.Map==map.MapId && seen.Coordinate==coordinate && seen.Minute==minute && ReferenceEquals(seen.Costs,costs))return seen.Facts;
        var aetherytes=(mapMarkers?.For(map.MarkerRange) ?? []).Where(marker=>marker.AetheryteId!=0).Select(marker=>(marker.AetheryteId,marker.X,marker.Y,marker.Shown)).ToArray();
        (string Name,uint Icon)? weatherNow=map.TerritoryId!=0 && weather.State.Catalog?.Forecast(map.TerritoryId,now,1) is [ { Weather:{ } current } ] ? (current.Name,current.Icon):null;
        var facts=PlaceTips.Build(map,coordinate,aetherytes,costs,weatherNow);
        placeTipSeen=(map.MapId,coordinate,minute,costs,facts);
        return facts;
    }

    /// <summary>
    /// X-684 (0.1672, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of a place, wherever it is
    /// named (a map bookmark, a map link of the chat and its list, a favourite of the map, an address of the place directory): its name, the
    /// zone and sub-zone, the region, the coordinates, the map's nearest aetheryte (its cost when the teleport list was read), the zone's
    /// weather now, then what the caller adds (a note, who sent it, a click's effect). Nothing worked out under the mouse at each frame.
    /// </summary>
    private void DrawPlaceTooltip(MapDefinition map,Vector2? coordinate,string? name=null,string? after=null)
    {
        var facts=PlaceTipFactsOf(map,coordinate);
        using(TooltipScope.Begin())
        {
            if(name is { Length:>0 })ImGui.TextColored(Accent,name);
            ImGui.TextUnformatted(facts.Area);
            if(facts.Region.Length>0 && facts.Region!=map.Zone)ImGui.TextDisabled(facts.Region);
            if(facts.Coordinates is { } text)ImGui.TextUnformatted(text);
            if(facts.Aetheryte is { } aetheryte)
                ImGui.TextDisabled(facts.AetheryteCost is { } cost ? localization.Format("MapView.PlaceTipAetheryteCost",aetheryte,cost.ToString("N0",Numbers))
                    :facts.Attuned==false ? localization.Format("MapView.PlaceTipAetheryteNotAttuned",aetheryte):localization.Format("MapView.PlaceTipAetheryte",aetheryte));
            if(facts.Weather is { } weatherName)
            {
                if(facts.WeatherIcon!=0){DrawGameIcon(facts.WeatherIcon,new Vector2(ImGui.GetTextLineHeight()));ImGui.SameLine();}
                ImGui.TextUnformatted(localization.Format("MapView.VistaWeatherNow",weatherName));
            }
            if(after is { Length:>0 }){ImGui.Separator();ImGui.TextDisabled(after);}
        }
    }
}
