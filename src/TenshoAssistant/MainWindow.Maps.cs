using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string mapQuery = "";
    private string mapViewQuery = "";
    private MapCatalog? mapViewCatalog;
    private IReadOnlyList<MapDefinition> mapMatches = [];
    private uint selectedMapId;
    private float mapX = 20.5f;
    private float mapY = 20.5f;
    private string? mapOpenError;

    private void DrawMaps()
    {
        Heading(localization.Get("MapsUi.Title"), localization.Get("MapsUi.Description"));
        var state = maps.State;
        if (state.Catalog is not { } catalog)
        {
            ImGui.TextWrapped(state.Error is null ? localization.Format("MapCounts.Loading",state.Copied,state.Total):localization.Get("MapCounts.Error"));
            if(developerMode && state.Error is { } error)ImGui.TextWrapped(error);
            return;
        }
        if (ImGui.Button(localization.Get("MapView.OpenCurrent"))) OpenMapView(0);
        DrawMapBookmarks(catalog);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##MapSearch", localization.Get("MapsUi.Search"), ref mapQuery, 128);
        if (!ReferenceEquals(mapViewCatalog, catalog) || mapViewQuery != mapQuery)
        { mapMatches = catalog.Search(mapQuery); mapViewCatalog = catalog; mapViewQuery = mapQuery; }
        ImGui.TextDisabled(localization.Format("MapCounts.Count",mapMatches.Count));
        var showList = ImGui.BeginChild("MapList", new Vector2(0, Math.Clamp(ImGui.GetContentRegionAvail().Y*.4f,100,210)), true);
        try
        {
            if (showList)
            {
                var clipper = ImGui.ImGuiListClipper();
                try
                {
                    clipper.Begin(mapMatches.Count);
                    while (clipper.Step())
                    for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                    {
                        var map = mapMatches[index];
                        var label = string.IsNullOrWhiteSpace(map.Subzone) ? map.Zone : $"{map.Zone} · {map.Subzone}";
                        if (ImGui.Selectable($"{label}##Map{map.MapId}", selectedMapId == map.MapId))
                        { selectedMapId = map.MapId; mapOpenError = null; selectedBookmark = Guid.Empty; bookmarkName = ""; bookmarkSaved = false; }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"{map.Region}\n{label}"+(developerMode ? $"\nMap {map.MapId} · territory {map.TerritoryId}":""));
                    }
                }
                finally { clipper.End(); clipper.Destroy(); }
            }
        }
        finally { ImGui.EndChild(); }
        if (catalog.Find(selectedMapId) is not { } selected)
        { ImGui.TextDisabled(localization.Get("MapsUi.Select")); return; }
        ImGui.TextColored(Accent, selected.Zone);
        if(ImGui.SmallButton(localization.Get("MapsUi.Weather"))) OpenWeather(selected.TerritoryId,selected.Zone);
        ImGui.TextUnformatted($"{selected.Region} · {selected.Subzone}");
        ImGui.SetNextItemWidth(110); var changedX = ImGui.InputFloat("X##MapX", ref mapX, 0, 0, "%.1f");
        ImGui.SameLine(); ImGui.SetNextItemWidth(110); var changedY = ImGui.InputFloat("Y##MapY", ref mapY, 0, 0, "%.1f");
        if (changedX || changedY) bookmarkSaved = false;
        var valid = MapCatalog.CoordinatesValid(mapX, mapY);
        ImGui.BeginDisabled(!valid);
        try
        {
            if (ImGui.Button(localization.Get("MapsUi.Open")))
                OpenMapPoint(selected, mapX, mapY);
        }
        finally { ImGui.EndDisabled(); }
        if (!valid) ImGui.TextWrapped(localization.Get("MapsUi.InvalidCoordinates"));
        if (mapOpenError is { } errorText) ImGui.TextWrapped(errorText);
        DrawBookmarkEditor(selected);
        if (ImGui.SmallButton(localization.Get("MapView.ShowSelected"))) OpenMapView(selected.MapId);
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("MapsUi.Resources")))
        { sourceZoneQuery = selected.Zone; sourceQuery = ""; sourceTypeFilter = null; page = Page.Gathering; }
        ImGui.TextWrapped(localization.Get("MapsUi.Help"));
    }
    // Flag placed from the Tensho map: the game opens its own map for the link, which the replacer turns into a « show map »
    // request; that one is ignored so the Tensho map neither closes (same zone = M toggle, 0.114 report) nor jumps.
    private DateTimeOffset mapOwnFlagUntil;
    private void PlaceFlagFromMapView(MapDefinition map,float x,float y)
    {
        mapOwnFlagUntil=DateTimeOffset.UtcNow.AddSeconds(2);
        OpenMapPoint(map,x,y);
        if(mapOpenError is not null){mapOwnFlagUntil=default;return;}
        // Centre the Tensho map on the new flag; following the player would move it away at once.
        mapViewMapId=map.MapId;mapViewFollow=false;mapCenterRequest=MapProjection.PixelFromCoordinate(new Vector2(x,y),map);
    }
    private void OpenMapPoint(MapDefinition map, float x, float y)
    {
        if (!MapCatalog.CoordinatesValid(x, y)) { mapOpenError = localization.Get("MapsUi.Invalid"); return; }
        try
        {
            if (!gameGui.OpenMapWithMapLink(new MapLinkPayload(map.TerritoryId, map.MapId, x, y)))
                mapOpenError = localization.Get("MapsUi.NotOpened");
            else mapOpenError = null;
        }
        catch (Exception error) { mapOpenError = localization.Get("MapsUi.OpenError"); log.Warning(error, "Could not open selected map."); }
    }
}
